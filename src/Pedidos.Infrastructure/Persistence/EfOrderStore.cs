using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Pedidos.Application;
using Pedidos.Domain;

namespace Pedidos.Infrastructure.Persistence;

public sealed class EfOrderStore(PedidosDbContext db) : IOrderStore
{
    // Só a violação da chave (client_id, key) da tabela idempotency_keys é conflito de idempotência.
    // Outras violações (NOT NULL, FK, outra tabela) seguem como erro, para não virarem 409 por engano.
    private const int SqliteConstraintPrimaryKey = 1555;
    private const int SqliteConstraintUnique = 2067;
    private const string IdempotencyTable = "idempotency_keys.";

    private static bool IsIdempotencyKeyViolation(DbUpdateException e) =>
        e.InnerException is SqliteException
        {
            SqliteExtendedErrorCode: SqliteConstraintPrimaryKey or SqliteConstraintUnique
        } sqlite
        && sqlite.Message.Contains(IdempotencyTable, StringComparison.Ordinal);

    public static readonly JsonSerializerOptions EventJson = new(JsonSerializerDefaults.Web)
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper) }
    };

    public Task<Order?> FindOrderAsync(Guid id, CancellationToken ct) =>
        db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<IdempotencyRecord?> FindIdempotencyAsync(string clientId, string key, CancellationToken ct)
    {
        var row = await db.IdempotencyKeys.AsNoTracking()
            .FirstOrDefaultAsync(k => k.ClientId == clientId && k.Key == key, ct);
        return row is null
            ? null
            : new IdempotencyRecord(row.ClientId, row.Key, row.RequestHash, row.OrderId, row.StatusCode, row.ResponseBody, row.CreatedAt, row.ExpiresAt);
    }

    public async Task AddOrderAsync(Order order, IdempotencyRecord? idempotency, IdempotencyRecord? expiredToRemove, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            if (expiredToRemove is not null)
            {
                await db.IdempotencyKeys
                    .Where(k => k.ClientId == expiredToRemove.ClientId && k.Key == expiredToRemove.Key && k.OrderId == expiredToRemove.OrderId)
                    .ExecuteDeleteAsync(ct);
            }

            db.Orders.Add(order);
            db.Outbox.AddRange(order.Events.Select(ToOutbox));
            if (idempotency is not null)
            {
                db.IdempotencyKeys.Add(new IdempotencyKeyRow
                {
                    ClientId = idempotency.ClientId,
                    Key = idempotency.Key,
                    RequestHash = idempotency.RequestHash,
                    OrderId = idempotency.OrderId,
                    StatusCode = idempotency.StatusCode,
                    ResponseBody = idempotency.ResponseBody,
                    CreatedAt = idempotency.CreatedAt,
                    ExpiresAt = idempotency.ExpiresAt
                });
            }

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (DbUpdateException e) when (idempotency is not null && IsIdempotencyKeyViolation(e))
        {
            await tx.RollbackAsync(ct);
            db.ChangeTracker.Clear();
            throw new IdempotencyConflictException(e);
        }
    }

    private static OutboxMessage ToOutbox(OrderStatusChanged e) => new()
    {
        Id = e.EventId,
        AggregateId = e.OrderId,
        Type = OrderStatusChanged.Type,
        OccurredAt = e.OccurredAt,
        Payload = JsonSerializer.Serialize(new
        {
            eventId = e.EventId,
            type = OrderStatusChanged.Type,
            schemaVersion = OrderStatusChanged.SchemaVersion,
            occurredAt = e.OccurredAt,
            orderId = e.OrderId,
            previousStatus = e.PreviousStatus,
            status = e.Status,
            version = e.Version
        }, EventJson)
    };
}
