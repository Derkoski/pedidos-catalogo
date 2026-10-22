using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pedidos.Application;
using Pedidos.Infrastructure.Catalogo;
using Pedidos.Infrastructure.Persistence;

namespace Pedidos.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPedidosInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<PedidosDbContext>(o => o.UseSqlite(connectionString));
        services.AddScoped<IOrderStore, EfOrderStore>();
        services.AddSingleton<InMemoryCatalog>();
        services.AddSingleton<ICatalog>(sp => sp.GetRequiredService<InMemoryCatalog>());
        return services;
    }

    /// <summary>Cria o schema na subida. Suficiente para a prova; produção usaria migrations.</summary>
    public static void EnsurePedidosDatabase(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        scope.ServiceProvider.GetRequiredService<PedidosDbContext>().Database.EnsureCreated();
    }
}
