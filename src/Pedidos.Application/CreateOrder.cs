using System.Security.Cryptography;
using System.Text;
using Pedidos.Domain;

namespace Pedidos.Application;

public sealed record CreateOrderItem(string ProductId, int Quantity);

public sealed record CreateOrderCommand(
    string ApiVersion,
    string ClientId,
    string? IdempotencyKey,
    string CustomerId,
    IReadOnlyList<CreateOrderItem> Items,
    string? Country = null)
{
    public const int MaxKeyLength = 64;

    /// <summary>
    /// SHA-256 do payload normalizado. A versão entra no hash: mesma chave em v1 e v2 conta como payload diferente.
    /// O país não entra: o client_id é vinculado a um país só, e a chave já tem escopo por client_id.
    /// Mudar o hash invalidaria as chaves gravadas antes do deploy.
    /// </summary>
    public string ComputeHash()
    {
        var items = Items
            .OrderBy(i => i.ProductId, StringComparer.Ordinal)
            .ThenBy(i => i.Quantity)
            .Select(i => $"{i.ProductId}:{i.Quantity}");
        var normalized = $"{ApiVersion}|{CustomerId}|{string.Join(",", items)}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
    }
}

public enum CreateOrderOutcome { Created, Replayed, Invalid, KeyReusedWithDifferentPayload, KeyInProgress, UnknownProduct, CountryNotServed }

public sealed record CreateOrderResult(CreateOrderOutcome Outcome, Guid? OrderId = null, int StatusCode = 0, string? Body = null, string? Error = null)
{
    public static CreateOrderResult Fail(CreateOrderOutcome outcome, string error) => new(outcome, Error: error);
    public static CreateOrderResult Replay(IdempotencyRecord r) => new(CreateOrderOutcome.Replayed, r.OrderId, r.StatusCode, r.ResponseBody);
}

public sealed class CreateOrderHandler(IOrderStore store, ICatalog catalog, CellOptions cell, TimeProvider clock)
{
    /// <param name="renderBody">Adaptador da versão (v1/v2) que monta o corpo da resposta. O corpo é gravado para o replay.</param>
    public async Task<CreateOrderResult> HandleAsync(CreateOrderCommand cmd, Func<Order, string> renderBody, CancellationToken ct)
    {
        var invalid = Validate(cmd);
        if (invalid is not null) return CreateOrderResult.Fail(CreateOrderOutcome.Invalid, invalid);

        // País vem do cliente de API (claim propagada pelo gateway); sem claim, vale o padrão da instância (ADR-005).
        var country = cmd.Country ?? cell.Country;
        if (!cell.Serves(country))
            return CreateOrderResult.Fail(CreateOrderOutcome.CountryNotServed, $"País {country} não é atendido por esta instância.");

        var now = clock.GetUtcNow();
        var hash = cmd.ComputeHash();
        IdempotencyRecord? expired = null;

        if (cmd.IdempotencyKey is not null)
        {
            var existing = await store.FindIdempotencyAsync(cmd.ClientId, cmd.IdempotencyKey, ct);
            if (existing is not null && existing.ExpiresAt <= now)
            {
                expired = existing;
                existing = null;
            }
            if (existing is not null) return Compare(existing, hash);
        }

        var ids = cmd.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await catalog.GetProductsAsync(ids, country, ct);
        var missing = ids.Where(id => !products.ContainsKey(id)).ToList();
        if (missing.Count > 0)
            return CreateOrderResult.Fail(CreateOrderOutcome.UnknownProduct, $"Produto inexistente no catálogo de {country}: {string.Join(", ", missing)}.");

        Order order;
        try
        {
            var items = cmd.Items
                .Select(i =>
                {
                    var p = products[i.ProductId];
                    return new OrderItem(p.Id, p.Description, i.Quantity, p.Price, p.Currency, p.CatalogVersion);
                })
                .ToList();
            order = Order.Create(cmd.ClientId, cmd.CustomerId, country, items, now);
        }
        catch (DomainException e)
        {
            return CreateOrderResult.Fail(CreateOrderOutcome.Invalid, e.Message);
        }

        const int created = 201;
        var body = renderBody(order);
        var record = cmd.IdempotencyKey is null
            ? null
            : new IdempotencyRecord(cmd.ClientId, cmd.IdempotencyKey, hash, order.Id, created, body, now, now + cell.IdempotencyRetention);

        try
        {
            await store.AddOrderAsync(order, record, expired, ct);
        }
        catch (IdempotencyConflictException)
        {
            // Corrida: outra chamada com a mesma chave gravou primeiro.
            var winner = await store.FindIdempotencyAsync(cmd.ClientId, cmd.IdempotencyKey!, ct);
            return winner is null
                ? CreateOrderResult.Fail(CreateOrderOutcome.KeyInProgress, "Requisição com esta chave ainda em processamento.")
                : Compare(winner, hash);
        }

        return new CreateOrderResult(CreateOrderOutcome.Created, order.Id, created, body);
    }

    private static CreateOrderResult Compare(IdempotencyRecord existing, string hash) =>
        existing.RequestHash == hash
            ? CreateOrderResult.Replay(existing)
            : CreateOrderResult.Fail(CreateOrderOutcome.KeyReusedWithDifferentPayload, "Idempotency-Key já usada com outro payload.");

    private static string? Validate(CreateOrderCommand cmd)
    {
        if (cmd.Country is { } c && (c.Length != 2 || !c.All(ch => char.IsAsciiLetterUpper(ch) || char.IsAsciiDigit(ch))))
            return "País deve ter 2 caracteres maiúsculos (ISO 3166-1 alfa-2).";
        if (cmd.IdempotencyKey is { } key && (key.Length is 0 or > CreateOrderCommand.MaxKeyLength))
            return $"Idempotency-Key deve ter de 1 a {CreateOrderCommand.MaxKeyLength} caracteres.";
        if (string.IsNullOrWhiteSpace(cmd.CustomerId) || cmd.CustomerId.Length > 64)
            return "customerId é obrigatório (até 64 caracteres).";
        if (cmd.Items is null || cmd.Items.Count is 0 or > Order.MaxItems)
            return $"items deve ter de 1 a {Order.MaxItems} itens.";
        if (cmd.Items.Any(i => i is null || string.IsNullOrWhiteSpace(i.ProductId)))
            return "productId é obrigatório.";
        if (cmd.Items.Any(i => i.Quantity is < 1 or > 999))
            return "quantity deve estar entre 1 e 999.";
        return null;
    }
}
