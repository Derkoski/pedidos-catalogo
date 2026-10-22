using NetArchTest.Rules;
using Pedidos.Application;
using Pedidos.Domain;

namespace Pedidos.Tests.Governanca;

/// <summary>Fitness functions de dependência entre camadas (doc 09).</summary>
public class ArquiteturaTests
{
    [Fact]
    public void Dominio_nao_depende_de_infraestrutura_aspnet_nem_de_outras_camadas()
    {
        var result = Types.InAssembly(typeof(Order).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny("Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore", "Pedidos.Application", "Pedidos.Infrastructure", "Pedidos.Api")
            .GetResult();

        Assert.True(result.IsSuccessful, Falhas(result));
    }

    [Fact]
    public void Aplicacao_nao_depende_de_infraestrutura_nem_de_aspnet()
    {
        var result = Types.InAssembly(typeof(CreateOrderHandler).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny("Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore", "Pedidos.Infrastructure", "Pedidos.Api")
            .GetResult();

        Assert.True(result.IsSuccessful, Falhas(result));
    }

    [Fact]
    public void Adaptadores_v1_e_v2_nao_acessam_banco_direto()
    {
        var adaptadores = Types.InAssembly(typeof(Program).Assembly).That().ResideInNamespaceStartingWith("Pedidos.Api.V");
        Assert.NotEmpty(adaptadores.GetTypes());

        var result = adaptadores
            .ShouldNot()
            .HaveDependencyOnAny("Microsoft.EntityFrameworkCore", "Pedidos.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful, Falhas(result));
    }

    private static string Falhas(TestResult r) =>
        "Tipos que violam a regra: " + string.Join(", ", r.FailingTypeNames ?? []);
}
