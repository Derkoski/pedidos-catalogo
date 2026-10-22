using System.Globalization;
using Pedidos.Tests.Infra;

namespace Pedidos.Tests.Governanca;

/// <summary>Regras de contrato que o lint não cobre (doc 09).</summary>
public class ContratosTests
{
    private static readonly string[] CamposProibidos = ["name", "document", "email", "phone", "address"];

    [Fact]
    public void Eventos_nao_carregam_dado_pessoal()
    {
        var asyncapi = Repo.Yaml("contracts", "asyncapi", "order-status.yaml");
        var campos = new List<string>();
        ColetarPropriedades(asyncapi.Node("components", "schemas"), campos);

        Assert.NotEmpty(campos);
        var violacoes = campos.Where(c => CamposProibidos.Any(p => c.Contains(p, StringComparison.OrdinalIgnoreCase))).ToList();
        Assert.True(violacoes.Count == 0, "Campos com possível dado pessoal: " + string.Join(", ", violacoes));
    }

    [Fact]
    public void Toda_mensagem_exige_eventId_e_schemaVersion()
    {
        var asyncapi = Repo.Yaml("contracts", "asyncapi", "order-status.yaml");
        var schemas = asyncapi.Node("components", "schemas");

        foreach (var (nome, msg) in asyncapi.Node("components", "messages"))
        {
            var payloadRef = (string)((Dictionary<object, object>)msg).Node("payload")["$ref"];
            var schema = (Dictionary<object, object>)schemas[payloadRef.Split('/')[^1]];
            var required = ((List<object>)schema["required"]).Cast<string>().ToList();
            Assert.True(required.Contains("eventId") && required.Contains("schemaVersion"), $"Mensagem {nome} sem eventId/schemaVersion obrigatórios");
        }
    }

    [Fact]
    public void Excecoes_tecnicas_nao_estao_vencidas()
    {
        var linhas = File.ReadAllLines(Repo.Path("docs", "excecoes.md"))
            .Where(l => l.TrimStart().StartsWith('|'))
            .Skip(2); // cabeçalho e separador

        foreach (var linha in linhas)
        {
            var colunas = linha.Trim().Trim('|').Split('|').Select(c => c.Trim()).ToArray();
            var expira = DateOnly.ParseExact(colunas[^1], "yyyy-MM-dd", CultureInfo.InvariantCulture);
            Assert.True(expira >= DateOnly.FromDateTime(DateTime.UtcNow), $"Exceção vencida: {linha.Trim()}");
        }
    }

    private static void ColetarPropriedades(object? node, List<string> campos)
    {
        switch (node)
        {
            case Dictionary<object, object> dict:
                foreach (var (k, v) in dict)
                {
                    if ((string)k == "properties" && v is Dictionary<object, object> props)
                        campos.AddRange(props.Keys.Cast<string>());
                    ColetarPropriedades(v, campos);
                }
                break;
            case List<object> list:
                foreach (var item in list) ColetarPropriedades(item, campos);
                break;
        }
    }
}
