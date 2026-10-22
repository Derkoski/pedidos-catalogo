using Pedidos.Domain;

namespace Pedidos.Application;

public sealed record CatalogProduct(string Id, string Description, decimal Price, string Currency, string CatalogVersion);

/// <summary>Consulta em lote: uma chamada por pedido (ADR-003).</summary>
public interface ICatalog
{
    Task<IReadOnlyDictionary<string, CatalogProduct>> GetProductsAsync(IReadOnlyCollection<string> ids, string country, CancellationToken ct);
}

public sealed record IdempotencyRecord(
    string ClientId,
    string Key,
    string RequestHash,
    Guid OrderId,
    int StatusCode,
    string ResponseBody,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt);

public interface IOrderStore
{
    Task<Order?> FindOrderAsync(Guid id, CancellationToken ct);
    Task<IdempotencyRecord?> FindIdempotencyAsync(string clientId, string key, CancellationToken ct);

    /// <summary>
    /// Grava pedido, eventos (outbox) e chave de idempotência numa única transação.
    /// Lança <see cref="IdempotencyConflictException"/> se a chave já existir.
    /// </summary>
    Task AddOrderAsync(Order order, IdempotencyRecord? idempotency, IdempotencyRecord? expiredToRemove, CancellationToken ct);
}

public sealed class IdempotencyConflictException(Exception inner)
    : Exception("Chave de idempotência já registrada.", inner);

/// <summary>
/// Configuração da instância. <see cref="Country"/> é o país padrão (consumidor atual do v1, sem claim de país).
/// <see cref="Countries"/> lista os países atendidos: opção A do ADR-005 = vários países num banco; opção B = célula com um país só.
/// </summary>
public sealed record CellOptions
{
    public string Country { get; init; } = "BR";
    public string[] Countries { get; init; } = [];
    public TimeSpan IdempotencyRetention { get; init; } = TimeSpan.FromHours(24);

    public bool Serves(string country) =>
        Countries.Length == 0 ? country == Country : Countries.Contains(country, StringComparer.Ordinal);
}
