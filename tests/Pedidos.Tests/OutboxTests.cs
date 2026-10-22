using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Pedidos.Tests.Infra;

namespace Pedidos.Tests;

/// <summary>Evento gravado na outbox na mesma transação do pedido (ADR-002) e aderente ao AsyncAPI.</summary>
public class OutboxTests
{
    [Fact]
    public async Task Criacao_grava_evento_na_outbox_junto_com_o_pedido()
    {
        using var api = new ApiFactory();
        var resp = await api.CreateClient().Criar("v2", Chamadas.Pedido(("SKU-0005", 1)), Guid.NewGuid().ToString());
        var orderId = JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetGuid();

        var outbox = await api.QueryDb(db => db.Outbox.SingleAsync());

        Assert.Equal(orderId, outbox.AggregateId);
        Assert.Equal("order.status-changed", outbox.Type);
        Assert.Null(outbox.PublishedAt);

        var payload = JsonDocument.Parse(outbox.Payload).RootElement;
        Assert.Equal(outbox.Id, payload.GetProperty("eventId").GetGuid());
        Assert.Equal(orderId, payload.GetProperty("orderId").GetGuid());
        Assert.Equal(1, payload.GetProperty("schemaVersion").GetInt32());
        Assert.Equal(JsonValueKind.Null, payload.GetProperty("previousStatus").ValueKind);
        Assert.Equal("CREATED", payload.GetProperty("status").GetString());
        Assert.Equal(1, payload.GetProperty("version").GetInt32());
    }

    [Fact]
    public async Task Payload_da_outbox_tem_exatamente_os_campos_do_schema_AsyncAPI()
    {
        using var api = new ApiFactory();
        await api.CreateClient().Criar("v2", Chamadas.Pedido(("SKU-0005", 1)), Guid.NewGuid().ToString());
        var outbox = await api.QueryDb(db => db.Outbox.SingleAsync());

        var schema = Repo.Yaml("contracts", "asyncapi", "order-status.yaml")
            .Node("components", "schemas", "OrderStatusChangedPayload");
        var esperado = ((Dictionary<object, object>)schema["properties"]).Keys.Cast<string>().ToHashSet();
        var atual = JsonDocument.Parse(outbox.Payload).RootElement.EnumerateObject().Select(p => p.Name).ToHashSet();

        Assert.Equal(esperado, atual);
    }

    [Fact]
    public async Task Replay_e_requisicao_rejeitada_nao_geram_evento()
    {
        using var api = new ApiFactory();
        var http = api.CreateClient();
        var key = Guid.NewGuid().ToString();

        await http.Criar("v2", Chamadas.Pedido(("SKU-0001", 1)), key);
        await http.Criar("v2", Chamadas.Pedido(("SKU-0001", 1)), key);
        await http.Criar("v2", Chamadas.Pedido(("SKU-0002", 1)), key);
        await http.Criar("v2", Chamadas.Pedido(("SKU-0001", 1)), key: null);

        Assert.Equal(1, await api.CountOutbox());
    }
}
