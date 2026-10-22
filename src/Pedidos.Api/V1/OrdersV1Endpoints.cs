using System.Text.Json;
using Pedidos.Application;
using Pedidos.Domain;

namespace Pedidos.Api.V1;

/// <summary>Contrato atual do web, preservado (ADR-004). Idempotency-Key opcional.</summary>
public static class OrdersV1Endpoints
{
    /// <summary>Consumidor atual não manda client_id; assume o canal web nacional.</summary>
    public const string LegacyClientId = "web";

    public sealed record OrderV1(Guid Id, OrderStatus Status, decimal Total);

    public static void MapOrdersV1(this IEndpointRouteBuilder app)
    {
        var v1 = app.MapGroup("/v1/orders");

        v1.MapPost("/", async (CreateOrderRequest request, HttpContext ctx, CreateOrderHandler handler, CancellationToken ct) =>
        {
            var cmd = request.ToCommand("v1", ClientId(ctx), Http.Header(ctx, Http.IdempotencyKeyHeader), Http.Header(ctx, Http.CountryHeader));
            var result = await handler.HandleAsync(cmd, Render, ct);
            return Http.ToHttp(result, ctx);
        });

        v1.MapGet("/{id:guid}", async (Guid id, HttpContext ctx, GetOrderHandler handler, CancellationToken ct) =>
        {
            var order = await handler.HandleAsync(id, ClientId(ctx), ct);
            return order is null ? Http.NotFound() : Results.Json(ToResponse(order), Http.Json);
        });
    }

    private static string ClientId(HttpContext ctx) => Http.Header(ctx, Http.ClientIdHeader) ?? LegacyClientId;

    private static OrderV1 ToResponse(Order o) => new(o.Id, o.Status, o.Total);

    private static string Render(Order o) => JsonSerializer.Serialize(ToResponse(o), Http.Json);
}
