namespace Pedidos.Domain;

/// <summary>Snapshot imutável do Catálogo no momento da criação (ADR-003).</summary>
public sealed class OrderItem
{
    public string ProductId { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public string Currency { get; private set; } = default!;
    public string CatalogVersion { get; private set; } = default!;

    private OrderItem() { }

    public OrderItem(string productId, string description, int quantity, decimal unitPrice, string currency, string catalogVersion)
    {
        if (quantity is < 1 or > 999) throw new DomainException("Quantidade deve estar entre 1 e 999.");
        if (unitPrice < 0) throw new DomainException("Preço não pode ser negativo.");
        ProductId = productId;
        Description = description;
        Quantity = quantity;
        UnitPrice = unitPrice;
        Currency = currency;
        CatalogVersion = catalogVersion;
    }

    public decimal Subtotal => UnitPrice * Quantity;
}
