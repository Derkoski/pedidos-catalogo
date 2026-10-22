using System.Text.RegularExpressions;
using Pedidos.Tests.Infra;

namespace Pedidos.Tests.Governanca;

/// <summary>Formato de ADR checável (docs/adr/README.md).</summary>
public partial class AdrTests
{
    private static readonly string[] Secoes = ["## Contexto", "## Alternativas", "## Decisão", "## Consequências"];

    public static TheoryData<string> Adrs()
    {
        var data = new TheoryData<string>();
        foreach (var f in Directory.GetFiles(Repo.Path("docs", "adr"), "ADR-*.md").Order()) data.Add(Path.GetFileName(f));
        return data;
    }

    [Fact]
    public void Existem_pelo_menos_quatro_adrs() => Assert.True(Adrs().Count >= 4);

    [Theory]
    [MemberData(nameof(Adrs))]
    public void Adr_tem_titulo_status_e_secoes_obrigatorias(string arquivo)
    {
        var texto = File.ReadAllText(Repo.Path("docs", "adr", arquivo)).ReplaceLineEndings("\n");
        var numero = arquivo[4..7];

        Assert.Matches($"^# ADR-{numero} – .+", texto.Split('\n')[0]);
        Assert.Matches(StatusRegex(), texto);
        foreach (var secao in Secoes)
            Assert.True(texto.Split('\n').Contains(secao), $"{arquivo}: falta a seção '{secao}'");
    }

    [Theory]
    [MemberData(nameof(Adrs))]
    public void Adr_esta_listado_no_indice(string arquivo)
    {
        var indice = File.ReadAllText(Repo.Path("docs", "adr", "README.md"));
        Assert.Contains($"({arquivo})", indice);
    }

    [GeneratedRegex(@"(?m)^Status: (Proposto|Aceito|Rejeitado|Substituído por ADR-\d{3})( \(.+\))?$")]
    private static partial Regex StatusRegex();
}
