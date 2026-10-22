using System.Net;
using System.Text.Json;
using Pedidos.Tests.Infra;

namespace Pedidos.Tests;

/// <summary>Critério crítico: mesma chave não cria pedido duplicado (ADR-001).</summary>
public class IdempotenciaTests
{
    private static readonly object PedidoA = Chamadas.Pedido(("SKU-0001", 2), ("SKU-0003", 1));
    private static readonly object PedidoB = Chamadas.Pedido(("SKU-0001", 3));

    [Fact]
    public async Task V2_mesma_chave_e_mesmo_payload_devolve_o_pedido_original_sem_duplicar()
    {
        using var api = new ApiFactory();
        var http = api.CreateClient();
        var key = Guid.NewGuid().ToString();

        var primeira = await http.Criar("v2", PedidoA, key);
        var segunda = await http.Criar("v2", PedidoA, key);

        Assert.Equal(HttpStatusCode.Created, primeira.StatusCode);
        Assert.Equal(HttpStatusCode.Created, segunda.StatusCode);
        Assert.Equal(await primeira.Content.ReadAsStringAsync(), await segunda.Content.ReadAsStringAsync());
        Assert.Equal(primeira.Headers.Location, segunda.Headers.Location);
        Assert.False(primeira.Headers.Contains("Idempotent-Replayed"));
        Assert.Equal("true", segunda.Headers.GetValues("Idempotent-Replayed").Single());
        Assert.Equal(1, await api.CountOrders());
        Assert.Equal(1, await api.CountOutbox());
    }

    [Fact]
    public async Task V2_mesma_chave_com_payload_diferente_retorna_422_e_nao_cria_pedido()
    {
        using var api = new ApiFactory();
        var http = api.CreateClient();
        var key = Guid.NewGuid().ToString();

        await http.Criar("v2", PedidoA, key);
        var resp = await http.Criar("v2", PedidoB, key);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resp.StatusCode);
        Assert.Equal("application/problem+json", resp.Content.Headers.ContentType?.MediaType);
        Assert.Equal(1, await api.CountOrders());
    }

    [Fact]
    public async Task V2_sem_chave_retorna_400_e_nao_cria_pedido()
    {
        using var api = new ApiFactory();
        var resp = await api.CreateClient().Criar("v2", PedidoA, key: null);

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        Assert.Equal("application/problem+json", resp.Content.Headers.ContentType?.MediaType);
        Assert.Equal(0, await api.CountOrders());
    }

    [Fact]
    public async Task Chave_com_mais_de_64_caracteres_retorna_400()
    {
        using var api = new ApiFactory();
        var resp = await api.CreateClient().Criar("v2", PedidoA, new string('k', 65));

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        Assert.Equal(0, await api.CountOrders());
    }

    [Fact]
    public async Task Chave_tem_escopo_por_cliente_de_api()
    {
        using var api = new ApiFactory();
        var http = api.CreateClient();
        const string key = "mesma-string";

        var a = await http.Criar("v2", PedidoA, key, clientId: "parceiro-a");
        var b = await http.Criar("v2", PedidoA, key, clientId: "parceiro-b");

        Assert.Equal(HttpStatusCode.Created, a.StatusCode);
        Assert.Equal(HttpStatusCode.Created, b.StatusCode);
        Assert.NotEqual(await Id(a), await Id(b));
        Assert.Equal(2, await api.CountOrders());
    }

    [Fact]
    public async Task V1_com_chave_passa_a_deduplicar()
    {
        using var api = new ApiFactory();
        var http = api.CreateClient();
        var key = Guid.NewGuid().ToString();

        var primeira = await http.Criar("v1", PedidoA, key, clientId: null);
        var segunda = await http.Criar("v1", PedidoA, key, clientId: null);

        Assert.Equal(await Id(primeira), await Id(segunda));
        Assert.Equal("true", segunda.Headers.GetValues("Idempotent-Replayed").Single());
        Assert.Equal(1, await api.CountOrders());
    }

    [Fact]
    public async Task V1_sem_chave_mantem_o_comportamento_antigo()
    {
        using var api = new ApiFactory();
        var http = api.CreateClient();

        var primeira = await http.Criar("v1", PedidoA, key: null, clientId: null);
        var segunda = await http.Criar("v1", PedidoA, key: null, clientId: null);

        Assert.Equal(HttpStatusCode.Created, primeira.StatusCode);
        Assert.Equal(HttpStatusCode.Created, segunda.StatusCode);
        Assert.Equal(2, await api.CountOrders());
    }

    [Fact]
    public async Task Mesma_chave_no_v1_e_no_v2_conta_como_payload_diferente()
    {
        using var api = new ApiFactory();
        var http = api.CreateClient();
        var key = Guid.NewGuid().ToString();

        await http.Criar("v1", PedidoA, key, clientId: "parceiro-a");
        var resp = await http.Criar("v2", PedidoA, key, clientId: "parceiro-a");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resp.StatusCode);
        Assert.Equal(1, await api.CountOrders());
    }

    [Fact]
    public async Task Chamadas_concorrentes_com_a_mesma_chave_criam_um_unico_pedido()
    {
        using var api = new ApiFactory();
        var http = api.CreateClient();
        var key = Guid.NewGuid().ToString();

        var respostas = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => http.Criar("v2", PedidoA, key)));

        Assert.All(respostas, r => Assert.Contains(r.StatusCode, new[] { HttpStatusCode.Created, HttpStatusCode.Conflict }));
        var ids = new HashSet<Guid>();
        foreach (var r in respostas.Where(r => r.StatusCode == HttpStatusCode.Created)) ids.Add(await Id(r));
        Assert.Single(ids);
        Assert.Equal(1, await api.CountOrders());
        Assert.Equal(1, await api.CountOutbox());
    }

    [Fact]
    public async Task Chave_expirada_apos_24h_cria_novo_pedido()
    {
        using var api = new ApiFactory();
        var http = api.CreateClient();
        var key = Guid.NewGuid().ToString();

        var primeira = await http.Criar("v2", PedidoA, key);
        api.Clock.Advance(TimeSpan.FromHours(24) + TimeSpan.FromSeconds(1));
        var depois = await http.Criar("v2", PedidoA, key);

        Assert.Equal(HttpStatusCode.Created, depois.StatusCode);
        Assert.False(depois.Headers.Contains("Idempotent-Replayed"));
        Assert.NotEqual(await Id(primeira), await Id(depois));
        Assert.Equal(2, await api.CountOrders());
    }

    private static async Task<Guid> Id(HttpResponseMessage r)
    {
        using var doc = JsonDocument.Parse(await r.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("id").GetGuid();
    }
}
