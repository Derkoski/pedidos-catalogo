using System.Net;
using System.Text.Json;
using Pedidos.Application;
using Pedidos.Tests.Infra;

namespace Pedidos.Tests;

/// <summary>
/// ADR-005: o país do pedido vem do cliente de API, não da instância.
/// Opção A = uma instância atende vários países; opção B = célula com um país só, que recusa os outros.
/// </summary>
public class PaisTests
{
    private static readonly CellOptions OpcaoA = new() { Country = "BR", Countries = ["BR", "P2"] };

    [Fact]
    public async Task Sem_claim_de_pais_o_pedido_usa_o_pais_padrao_da_instancia()
    {
        using var api = new ApiFactory();
        var resp = await api.CreateClient().Criar("v2", Chamadas.Pedido(("SKU-0001", 1)), Guid.NewGuid().ToString());

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var corpo = JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("BR", corpo.GetProperty("country").GetString());
        Assert.Equal("BRL", corpo.GetProperty("currency").GetString());
    }

    [Fact]
    public async Task Opcao_A_uma_instancia_grava_o_pais_e_o_preco_do_cliente_de_api()
    {
        using var api = new ApiFactory(OpcaoA);
        var http = api.CreateClient();

        var br = await http.Criar("v2", Chamadas.Pedido(("SKU-0001", 2)), Guid.NewGuid().ToString(), clientId: "app-br", country: "BR");
        var p2 = await http.Criar("v2", Chamadas.Pedido(("SKU-0001", 2)), Guid.NewGuid().ToString(), clientId: "app-p2", country: "P2");

        Assert.Equal(HttpStatusCode.Created, br.StatusCode);
        Assert.Equal(HttpStatusCode.Created, p2.StatusCode);
        var pedidoBr = JsonDocument.Parse(await br.Content.ReadAsStringAsync()).RootElement;
        var pedidoP2 = JsonDocument.Parse(await p2.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(("BR", "BRL", 99.80m), (pedidoBr.GetProperty("country").GetString(), pedidoBr.GetProperty("currency").GetString(), pedidoBr.GetProperty("total").GetDecimal()));
        Assert.Equal(("P2", "USD", 24.00m), (pedidoP2.GetProperty("country").GetString(), pedidoP2.GetProperty("currency").GetString(), pedidoP2.GetProperty("total").GetDecimal()));
        Assert.Equal(2, await api.CountOrders());
    }

    [Fact]
    public async Task Opcao_B_celula_de_um_pais_recusa_cliente_de_outro_pais_sem_gravar_nada()
    {
        using var api = new ApiFactory();
        var resp = await api.CreateClient().Criar("v2", Chamadas.Pedido(("SKU-0001", 1)), Guid.NewGuid().ToString(), clientId: "app-p2", country: "P2");

        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
        Assert.Equal("application/problem+json", resp.Content.Headers.ContentType?.MediaType);
        Assert.Equal(0, await api.CountOrders());
        Assert.Equal(0, await api.CountOutbox());
    }

    [Fact]
    public async Task Pais_em_formato_invalido_retorna_400()
    {
        using var api = new ApiFactory(OpcaoA);
        var resp = await api.CreateClient().Criar("v2", Chamadas.Pedido(("SKU-0001", 1)), Guid.NewGuid().ToString(), country: "brasil");

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        Assert.Equal(0, await api.CountOrders());
    }
}
