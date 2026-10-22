using Microsoft.EntityFrameworkCore;
using Pedidos.Domain;

namespace Pedidos.Infrastructure.Persistence;

public sealed class IdempotencyKeyRow
{
    public string ClientId { get; set; } = default!;
    public string Key { get; set; } = default!;
    public string RequestHash { get; set; } = default!;
    public Guid OrderId { get; set; }
    public int StatusCode { get; set; }
    public string ResponseBody { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}

/// <summary>Linha da outbox (ADR-002). Id = eventId. Relay e broker ficam fora da fatia.</summary>
public sealed class OutboxMessage
{
    public Guid Id { get; set; }
    public Guid AggregateId { get; set; }
    public string Type { get; set; } = default!;
    public string Payload { get; set; } = default!;
    public DateTimeOffset OccurredAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
}

public sealed class PedidosDbContext(DbContextOptions<PedidosDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<IdempotencyKeyRow> IdempotencyKeys => Set<IdempotencyKeyRow>();
    public DbSet<OutboxMessage> Outbox => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Order>(o =>
        {
            o.ToTable("orders");
            o.HasKey(x => x.Id);
            o.Property(x => x.ClientId).HasMaxLength(64).IsRequired();
            o.Property(x => x.CustomerId).HasMaxLength(64).IsRequired();
            o.Property(x => x.Country).HasMaxLength(2).IsRequired();
            o.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            o.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
            o.Property(x => x.Version).IsConcurrencyToken();
            o.Ignore(x => x.Events);
            o.HasIndex(x => x.ClientId);
            o.OwnsMany(x => x.Items, i =>
            {
                i.ToTable("order_items");
                i.WithOwner().HasForeignKey("OrderId");
                i.Property<int>("Id");
                i.HasKey("Id");
                i.Property(x => x.ProductId).HasMaxLength(64).IsRequired();
                i.Property(x => x.Description).HasMaxLength(256).IsRequired();
                i.Property(x => x.Currency).HasMaxLength(3).IsRequired();
                i.Property(x => x.CatalogVersion).HasMaxLength(32).IsRequired();
            });
            o.Navigation(x => x.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        b.Entity<IdempotencyKeyRow>(k =>
        {
            k.ToTable("idempotency_keys");
            k.HasKey(x => new { x.ClientId, x.Key });
            k.Property(x => x.ClientId).HasMaxLength(64);
            k.Property(x => x.Key).HasMaxLength(64);
            k.Property(x => x.RequestHash).HasMaxLength(64).IsRequired();
        });

        b.Entity<OutboxMessage>(m =>
        {
            m.ToTable("outbox");
            m.HasKey(x => x.Id);
            m.Property(x => x.Type).HasMaxLength(64).IsRequired();
            m.HasIndex(x => x.PublishedAt);
        });
    }
}
