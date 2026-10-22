using System.Net.Http.Json;
using System.Text.Json;
using Pedidos.Tests.Infra;

namespace Pedidos.Tests;

/// <summary>
/// Teste de contrato do consumidor atual (web) do v1 (ADR-004). A fixture representa o que o web envia e lê.
/// Se a evolução para o v2 quebrar o v1, este teste falha.
/// </summary>
public class ContratoV1ConsumidorTests
{
    private static JsonElement Fixture(string nome) =>
        JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "v1-consumidor-web", nome))).RootElement;

    [Fact]
    public async Task Consumidor_web_atual_cria_e_consulta_pedido_no_v1_como_antes()
    {
        using var api = new ApiFactory();
        var http = api.CreateClient();

        var criar = Fixture("criar-pedido.json");
        var resp = await http.PostAsJsonAsync(criar.GetProperty("request").GetProperty("path").GetString(),
            criar.GetProperty("request").GetProperty("body"));
        var corpo = await AssertResposta(resp, criar.GetProperty("response"));

        var consultar = Fixture("consultar-pedido.json");
        var path = consultar.GetProperty("request").GetProperty("path").GetString()!
            .Replace("{id}", corpo.GetProperty("id").GetString());
        await AssertResposta(await http.GetAsync(path), consultar.GetProperty("response"));
    }

    private static async Task<JsonElement> AssertResposta(HttpResponseMessage resp, JsonElement esperado)
    {
        Assert.Equal(esperado.GetProperty("status").GetInt32(), (int)resp.StatusCode);
        Assert.Equal(esperado.GetProperty("contentType").GetString(), resp.Content.Headers.ContentType?.MediaType);
        var corpo = JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement;
        AssertCorpo(esperado.GetProperty("body"), corpo, "$");
        return corpo;
    }

    /// <summary>Todo campo que o consumidor lê precisa existir, com o mesmo tipo e valor. Campos extras são tolerados.</summary>
    private static void AssertCorpo(JsonElement esperado, JsonElement atual, string caminho)
    {
        switch (esperado.ValueKind)
        {
            case JsonValueKind.Object:
                Assert.Equal(JsonValueKind.Object, atual.ValueKind);
                foreach (var campo in esperado.EnumerateObject())
                {
                    Assert.True(atual.TryGetProperty(campo.Name, out var valor), $"Campo ausente: {caminho}.{campo.Name}");
                    AssertCorpo(campo.Value, valor, $"{caminho}.{campo.Name}");
                }
                break;
            case JsonValueKind.String when esperado.GetString() == "{{uuid}}":
                Assert.True(Guid.TryParse(atual.GetString(), out _), $"{caminho} não é UUID");
                break;
            case JsonValueKind.Number:
                Assert.Equal(JsonValueKind.Number, atual.ValueKind);
                Assert.Equal(esperado.GetDecimal(), atual.GetDecimal());
                break;
            default:
                Assert.Equal(esperado.ValueKind, atual.ValueKind);
                Assert.Equal(esperado.ToString(), atual.ToString());
                break;
        }
    }
}
