using YamlDotNet.Serialization;

namespace Pedidos.Tests.Infra;

public static class Repo
{
    public static string Root { get; } = FindRoot();

    public static string Path(params string[] parts) => System.IO.Path.Combine([Root, .. parts]);

    /// <summary>Lê YAML como árvore de dicionários e listas.</summary>
    public static Dictionary<object, object> Yaml(params string[] parts) =>
        new DeserializerBuilder().Build().Deserialize<Dictionary<object, object>>(File.ReadAllText(Path(parts)));

    public static Dictionary<object, object> Node(this Dictionary<object, object> node, params string[] path)
    {
        foreach (var p in path) node = (Dictionary<object, object>)node[p];
        return node;
    }

    private static string FindRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(System.IO.Path.Combine(dir.FullName, "PedidosCatalogo.sln")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("Raiz do repositório não encontrada.");
    }
}
