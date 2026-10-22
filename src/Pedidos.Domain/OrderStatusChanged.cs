namespace Pedidos.Domain;

/// <summary>Evento de domínio. Vira linha na outbox na mesma transação (ADR-002). Sem dado pessoal.</summary>
public sealed record OrderStatusChanged(
    Guid EventId,
    Guid OrderId,
    OrderStatus? PreviousStatus,
    OrderStatus Status,
    int Version,
    DateTimeOffset OccurredAt)
{
    public const string Type = "order.status-changed";
    public const int SchemaVersion = 1;
}
