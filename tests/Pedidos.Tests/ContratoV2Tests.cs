using System.Net;
using System.Text.Json;
using Pedidos.Tests.Infra;

namespace Pedidos.Tests;

public class ContratoV2Tests
{
    [Fact]
    public async Task Resposta_de_criacao_segue_o_schema_Order_da_OpenAPI_v2()
    {
        using var api = new ApiFactory();
        var resp = await api.CreateClient().Criar("v2", Chamadas.Pedido(("SKU-0001", 2), ("SKU-0002", 1)), Guid.NewGuid().ToString());

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var corpo = JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal($"/v2/orders/{corpo.GetProperty("id").GetGuid()}", resp.Headers.Location?.ToString());

        var schemas = Repo.Yaml("contracts", "openapi", "orders-v2.yaml").Node("components", "schemas");
        AssertCampos(schemas.Node("Order"), corpo);
        foreach (var item in corpo.GetProperty("items").EnumerateArray())
            AssertCampos(schemas.Node("OrderItem"), item);

        Assert.Equal("CREATED", corpo.GetProperty("status").GetString());
        Assert.Equal("BR", corpo.GetProperty("country").GetString());
        Assert.Equal("BRL", corpo.GetProperty("currency").GetString());
        Assert.Equal(259.70m, corpo.GetProperty("total").GetDecimal());
    }

    [Fact]
    public async Task Snapshot_preserva_preco_e_descricao_depois_de_mudanca_no_catalogo()
    {
        using var api = new ApiFactory();
        var http = api.CreateClient();
        var criado = await http.Criar("v2", Chamadas.Pedido(("SKU-0003", 1)), Guid.NewGuid().ToString());
        var id = JsonDocument.Parse(await criado.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetGuid();

        api.Catalog.ChangePrice("BR", "SKU-0003", 399.00m, "2026-11-01.1");
        var consulta = JsonDocument.Parse(await (await http.Consultar("v2", id)).Content.ReadAsStringAsync()).RootElement;

        var item = consulta.GetProperty("items")[0];
        Assert.Equal(329.00m, item.GetProperty("unitPrice").GetDecimal());
        Assert.Equal("Tênis corrida", item.GetProperty("description").GetString());
        Assert.Equal("2026-10-01.1", item.GetProperty("catalogVersion").GetString());
        Assert.Equal(329.00m, consulta.GetProperty("total").GetDecimal());
    }

    [Fact]
    public async Task Cliente_de_api_nao_le_pedido_de_outro_cliente()
    {
        using var api = new ApiFactory();
        var http = api.CreateClient();
        var criado = await http.Criar("v2", Chamadas.Pedido(("SKU-0001", 1)), Guid.NewGuid().ToString(), clientId: "parceiro-a");
        var id = JsonDocument.Parse(await criado.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetGuid();

        Assert.Equal(HttpStatusCode.OK, (await http.Consultar("v2", id, "parceiro-a")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await http.Consultar("v2", id, "parceiro-b")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await http.Consultar("v2", id, clientId: null)).StatusCode);
    }

    [Fact]
    public async Task Produto_inexistente_retorna_422_e_nao_grava_nada()
    {
        using var api = new ApiFactory();
        var resp = await api.CreateClient().Criar("v2", Chamadas.Pedido(("SKU-9999", 1)), Guid.NewGuid().ToString());

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resp.StatusCode);
        Assert.Equal("application/problem+json", resp.Content.Headers.ContentType?.MediaType);
        Assert.Equal(0, await api.CountOrders());
        Assert.Equal(0, await api.CountOutbox());
    }

    [Fact]
    public async Task Catalogo_e_consultado_uma_vez_por_pedido_sem_N_mais_1()
    {
        using var api = new ApiFactory();
        var itens = Enumerable.Range(1, 50).Select(i => ($"SKU-{(i % 10) + 1:0000}", 1)).ToArray();
        var antes = api.Catalog.Calls;

        var resp = await api.CreateClient().Criar("v2", Chamadas.Pedido(itens), Guid.NewGuid().ToString());

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        Assert.Equal(1, api.Catalog.Calls - antes);
    }

    private static void AssertCampos(Dictionary<object, object> schema, JsonElement objeto)
    {
        var props = ((Dictionary<object, object>)schema["properties"]).Keys.Cast<string>().ToHashSet();
        var required = ((List<object>)schema["required"]).Cast<string>();
        var atuais = objeto.EnumerateObject().Select(p => p.Name).ToHashSet();

        foreach (var r in required) Assert.Contains(r, atuais);
        foreach (var a in atuais) Assert.Contains(a, props);
    }
}
