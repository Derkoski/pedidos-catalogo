namespace Pedidos.Domain;

public sealed class Order
{
    public const int MaxItems = 100;

    private static readonly Dictionary<OrderStatus, OrderStatus[]> Transitions = new()
    {
        [OrderStatus.Created] = [OrderStatus.Confirmed, OrderStatus.Cancelled],
        [OrderStatus.Confirmed] = [OrderStatus.Shipped, OrderStatus.Cancelled],
        [OrderStatus.Shipped] = [OrderStatus.Delivered],
        [OrderStatus.Delivered] = [],
        [OrderStatus.Cancelled] = []
    };

    private readonly List<OrderItem> _items = [];
    private readonly List<OrderStatusChanged> _events = [];

    public Guid Id { get; private set; }
    public string ClientId { get; private set; } = default!;
    public string CustomerId { get; private set; } = default!;
    public string Country { get; private set; } = default!;
    public string Currency { get; private set; } = default!;
    public OrderStatus Status { get; private set; }
    public decimal Total { get; private set; }
    public int Version { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public IReadOnlyList<OrderItem> Items => _items;
    public IReadOnlyList<OrderStatusChanged> Events => _events;

    private Order() { }

    public static Order Create(string clientId, string customerId, string country, IReadOnlyList<OrderItem> items, DateTimeOffset now)
    {
        if (items.Count is 0 or > MaxItems) throw new DomainException($"Pedido deve ter de 1 a {MaxItems} itens.");
        var currencies = items.Select(i => i.Currency).Distinct().ToList();
        if (currencies.Count != 1) throw new DomainException("Itens com moedas diferentes.");

        var order = new Order
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            CustomerId = customerId,
            Country = country,
            Currency = currencies[0],
            Status = OrderStatus.Created,
            Total = Math.Round(items.Sum(i => i.Subtotal), 2),
            Version = 1,
            CreatedAt = now
        };
        order._items.AddRange(items);
        order._events.Add(new OrderStatusChanged(Guid.NewGuid(), order.Id, null, OrderStatus.Created, order.Version, now));
        return order;
    }

    public void ChangeStatus(OrderStatus next, DateTimeOffset now)
    {
        if (!Transitions[Status].Contains(next))
            throw new DomainException($"Transição inválida: {Status} -> {next}.");
        var previous = Status;
        Status = next;
        Version++;
        _events.Add(new OrderStatusChanged(Guid.NewGuid(), Id, previous, next, Version, now));
    }

    public bool BelongsTo(string clientId) => ClientId == clientId;
}
