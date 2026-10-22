using System.Text.Json;
using System.Text.Json.Serialization;
using Pedidos.Application;

namespace Pedidos.Api;

public sealed record CreateOrderRequest(string? CustomerId, List<CreateOrderItemRequest>? Items)
{
    public CreateOrderCommand ToCommand(string apiVersion, string clientId, string? idempotencyKey, string? country) =>
        new(apiVersion, clientId, idempotencyKey, CustomerId ?? "",
            Items?.Select(i => new CreateOrderItem(i?.ProductId ?? "", i?.Quantity ?? 0)).ToList() ?? [],
            country);
}

public sealed record CreateOrderItemRequest(string? ProductId, int Quantity);

internal static class Http
{
    public const string IdempotencyKeyHeader = "Idempotency-Key";
    public const string ReplayedHeader = "Idempotent-Replayed";

    /// <summary>
    /// Na fatia não há gateway: o header simula a claim <c>client_id</c> que o gateway repassa após validar o JWT (ADR-007).
    /// </summary>
    public const string ClientIdHeader = "X-Client-Id";

    /// <summary>
    /// Simula a claim <c>country</c> do cliente de API: cada <c>client_id</c> é vinculado a um país, e o gateway repassa
    /// esse valor (ADR-005, ADR-007). Sem o header, vale o país padrão da instância (consumidor atual do v1).
    /// </summary>
    public const string CountryHeader = "X-Client-Country";

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper) }
    };

    public static string? Header(HttpContext ctx, string name) =>
        ctx.Request.Headers.TryGetValue(name, out var v) ? v.ToString() : null;

    public static IResult ToHttp(CreateOrderResult r, HttpContext ctx, Func<Guid, string>? location = null)
    {
        switch (r.Outcome)
        {
            case CreateOrderOutcome.Created:
            case CreateOrderOutcome.Replayed:
                if (location is not null) ctx.Response.Headers.Location = location(r.OrderId!.Value);
                if (r.Outcome == CreateOrderOutcome.Replayed) ctx.Response.Headers[ReplayedHeader] = "true";
                return Results.Text(r.Body, "application/json", System.Text.Encoding.UTF8, r.StatusCode);
            case CreateOrderOutcome.KeyInProgress:
                return Results.Problem(r.Error, statusCode: StatusCodes.Status409Conflict, title: "Requisição em processamento");
            case CreateOrderOutcome.KeyReusedWithDifferentPayload:
                return Results.Problem(r.Error, statusCode: StatusCodes.Status422UnprocessableEntity, title: "Idempotency-Key reutilizada");
            case CreateOrderOutcome.CountryNotServed:
                return Results.Problem(r.Error, statusCode: StatusCodes.Status403Forbidden, title: "País não atendido");
            case CreateOrderOutcome.UnknownProduct:
                return Results.Problem(r.Error, statusCode: StatusCodes.Status422UnprocessableEntity, title: "Produto inexistente");
            default:
                return Results.Problem(r.Error, statusCode: StatusCodes.Status400BadRequest, title: "Requisição inválida");
        }
    }

    public static IResult NotFound() =>
        Results.Problem("Pedido não encontrado.", statusCode: StatusCodes.Status404NotFound, title: "Não encontrado");
}
