using System.Text.Json;
using Pedidos.Application;
using Pedidos.Domain;

namespace Pedidos.Api.V2;

/// <summary>Contrato novo (ADR-004): Idempotency-Key obrigatória e snapshot nos itens.</summary>
public static class OrdersV2Endpoints
{
    public sealed record OrderItemV2(string ProductId, string Description, int Quantity, decimal UnitPrice, string Currency, string CatalogVersion);

    public sealed record OrderV2(
        Guid Id,
        OrderStatus Status,
        string CustomerId,
        string Country,
        string Currency,
        decimal Total,
        DateTimeOffset CreatedAt,
        IReadOnlyList<OrderItemV2> Items);

    public static void MapOrdersV2(this IEndpointRouteBuilder app)
    {
        var v2 = app.MapGroup("/v2/orders");

        v2.MapPost("/", async (CreateOrderRequest request, HttpContext ctx, CreateOrderHandler handler, CancellationToken ct) =>
        {
            var clientId = Http.Header(ctx, Http.ClientIdHeader);
            if (string.IsNullOrWhiteSpace(clientId)) return Unauthorized();

            var key = Http.Header(ctx, Http.IdempotencyKeyHeader);
            if (string.IsNullOrEmpty(key))
                return Results.Problem("Header Idempotency-Key é obrigatório no v2.", statusCode: StatusCodes.Status400BadRequest, title: "Requisição inválida");

            var result = await handler.HandleAsync(request.ToCommand("v2", clientId, key, Http.Header(ctx, Http.CountryHeader)), Render, ct);
            return Http.ToHttp(result, ctx, id => $"/v2/orders/{id}");
        });

        v2.MapGet("/{id:guid}", async (Guid id, HttpContext ctx, GetOrderHandler handler, CancellationToken ct) =>
        {
            var clientId = Http.Header(ctx, Http.ClientIdHeader);
            if (string.IsNullOrWhiteSpace(clientId)) return Unauthorized();

            var order = await handler.HandleAsync(id, clientId, ct);
            return order is null ? Http.NotFound() : Results.Json(ToResponse(order), Http.Json);
        });
    }

    private static IResult Unauthorized() =>
        Results.Problem("Cliente de API não identificado.", statusCode: StatusCodes.Status401Unauthorized, title: "Não autenticado");

    private static OrderV2 ToResponse(Order o) => new(
        o.Id, o.Status, o.CustomerId, o.Country, o.Currency, o.Total, o.CreatedAt,
        o.Items.Select(i => new OrderItemV2(i.ProductId, i.Description, i.Quantity, i.UnitPrice, i.Currency, i.CatalogVersion)).ToList());

    private static string Render(Order o) => JsonSerializer.Serialize(ToResponse(o), Http.Json);
}
