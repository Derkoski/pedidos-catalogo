using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Pedidos.Application;
using Pedidos.Infrastructure.Catalogo;
using Pedidos.Infrastructure.Persistence;

namespace Pedidos.Tests.Infra;

/// <summary>Sobe a API no servidor de teste com um banco SQLite próprio (arquivo temporário, apagado no Dispose) por instância.</summary>
public sealed class ApiFactory(CellOptions? cell = null) : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"pedidos-test-{Guid.NewGuid():N}.db");

    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 10, 21, 12, 0, 0, TimeSpan.Zero));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<PedidosDbContext>>();
            services.AddDbContext<PedidosDbContext>(o => o.UseSqlite($"Data Source={_dbPath};Default Timeout=30"));
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);
            if (cell is not null)
            {
                services.RemoveAll<CellOptions>();
                services.AddSingleton(cell);
            }
        });
    }

    public InMemoryCatalog Catalog => Services.GetRequiredService<InMemoryCatalog>();

    public async Task<T> QueryDb<T>(Func<PedidosDbContext, Task<T>> query)
    {
        using var scope = Services.CreateScope();
        return await query(scope.ServiceProvider.GetRequiredService<PedidosDbContext>());
    }

    public Task<int> CountOrders() => QueryDb(db => db.Orders.CountAsync());
    public Task<int> CountOutbox() => QueryDb(db => db.Outbox.CountAsync());

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        SqliteConnection.ClearAllPools();
        try { File.Delete(_dbPath); } catch (IOException) { }
    }
}

internal static class ServiceCollectionExtensions
{
    public static void RemoveAll<T>(this IServiceCollection services)
    {
        foreach (var d in services.Where(d => d.ServiceType == typeof(T)).ToList())
            services.Remove(d);
    }
}

public static class Chamadas
{
    public static object Pedido(params (string ProductId, int Quantity)[] items) => new
    {
        customerId = "cli-0001",
        items = items.Select(i => new { productId = i.ProductId, quantity = i.Quantity })
    };

    public static Task<HttpResponseMessage> Criar(this HttpClient http, string version, object body, string? key = null, string? clientId = "parceiro-a", string? country = null)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, $"/{version}/orders") { Content = JsonContent.Create(body) };
        if (key is not null) req.Headers.Add("Idempotency-Key", key);
        if (clientId is not null) req.Headers.Add("X-Client-Id", clientId);
        if (country is not null) req.Headers.Add("X-Client-Country", country);
        return http.SendAsync(req);
    }

    public static Task<HttpResponseMessage> Consultar(this HttpClient http, string version, Guid id, string? clientId = "parceiro-a")
    {
        var req = new HttpRequestMessage(HttpMethod.Get, $"/{version}/orders/{id}");
        if (clientId is not null) req.Headers.Add("X-Client-Id", clientId);
        return http.SendAsync(req);
    }
}
