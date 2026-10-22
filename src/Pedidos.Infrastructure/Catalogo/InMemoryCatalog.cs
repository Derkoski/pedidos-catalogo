using System.Collections.Concurrent;
using System.Text.Json;
using Pedidos.Application;

namespace Pedidos.Infrastructure.Catalogo;

/// <summary>Stub do Catálogo com dados sintéticos (data/catalogo-sintetico.json). Simula a consulta em lote do ADR-003.</summary>
public sealed class InMemoryCatalog : ICatalog
{
    private readonly ConcurrentDictionary<(string Country, string Id), CatalogProduct> _products = new();
    private int _calls;

    public InMemoryCatalog()
    {
        using var stream = typeof(InMemoryCatalog).Assembly.GetManifestResourceStream("catalogo-sintetico.json")
            ?? throw new InvalidOperationException("Catálogo sintético não encontrado.");
        var file = JsonSerializer.Deserialize<CatalogFile>(stream, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        foreach (var p in file.Products)
            _products[(p.Country, p.Id)] = new CatalogProduct(p.Id, p.Description, p.Price, p.Currency, file.CatalogVersion);
    }

    /// <summary>Número de chamadas recebidas. Usado para provar que não há N+1.</summary>
    public int Calls => _calls;

    public Task<IReadOnlyDictionary<string, CatalogProduct>> GetProductsAsync(IReadOnlyCollection<string> ids, string country, CancellationToken ct)
    {
        Interlocked.Increment(ref _calls);
        IReadOnlyDictionary<string, CatalogProduct> found = ids
            .Where(id => _products.ContainsKey((country, id)))
            .ToDictionary(id => id, id => _products[(country, id)]);
        return Task.FromResult(found);
    }

    /// <summary>Simula mudança de preço no Catálogo.</summary>
    public void ChangePrice(string country, string id, decimal price, string catalogVersion) =>
        _products.AddOrUpdate((country, id),
            _ => throw new KeyNotFoundException(id),
            (_, p) => p with { Price = price, CatalogVersion = catalogVersion });

    private sealed record CatalogFile(string CatalogVersion, List<ProductRow> Products);
    private sealed record ProductRow(string Id, string Country, string Description, decimal Price, string Currency);
}
