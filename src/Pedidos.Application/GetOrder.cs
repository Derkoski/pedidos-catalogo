using Pedidos.Domain;

namespace Pedidos.Application;

public sealed class GetOrderHandler(IOrderStore store)
{
    /// <summary>Cliente de API só lê os próprios pedidos (ADR-007). Pedido de outro cliente = não encontrado.</summary>
    public async Task<Order?> HandleAsync(Guid id, string clientId, CancellationToken ct)
    {
        var order = await store.FindOrderAsync(id, ct);
        return order is not null && order.BelongsTo(clientId) ? order : null;
    }
}
