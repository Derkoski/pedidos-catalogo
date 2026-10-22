using Pedidos.Domain;

namespace Pedidos.Tests;

public class DominioTests
{
    private static readonly DateTimeOffset Agora = new(2026, 10, 21, 12, 0, 0, TimeSpan.Zero);

    private static Order NovoPedido() => Order.Create("parceiro-a", "cli-0001", "BR",
        [new OrderItem("SKU-0001", "Camiseta", 2, 49.90m, "BRL", "v1"), new OrderItem("SKU-0004", "Meia", 1, 19.90m, "BRL", "v1")], Agora);

    [Fact]
    public void Criacao_calcula_total_e_gera_evento_inicial()
    {
        var order = NovoPedido();

        Assert.Equal(119.70m, order.Total);
        Assert.Equal(OrderStatus.Created, order.Status);
        var evento = Assert.Single(order.Events);
        Assert.Null(evento.PreviousStatus);
        Assert.Equal(1, evento.Version);
    }

    [Fact]
    public void Mudanca_de_status_valida_incrementa_versao_e_gera_evento()
    {
        var order = NovoPedido();
        order.ChangeStatus(OrderStatus.Confirmed, Agora);
        order.ChangeStatus(OrderStatus.Shipped, Agora);

        var ultimo = order.Events[^1];
        Assert.Equal(3, order.Version);
        Assert.Equal(OrderStatus.Confirmed, ultimo.PreviousStatus);
        Assert.Equal(OrderStatus.Shipped, ultimo.Status);
        Assert.Equal(3, ultimo.Version);
    }

    [Theory]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Delivered)]
    [InlineData(OrderStatus.Created)]
    public void Transicao_invalida_e_rejeitada(OrderStatus destino)
    {
        var order = NovoPedido();
        Assert.Throws<DomainException>(() => order.ChangeStatus(destino, Agora));
    }

    [Fact]
    public void Pedido_sem_itens_ou_com_moedas_diferentes_e_rejeitado()
    {
        Assert.Throws<DomainException>(() => Order.Create("c", "cli", "BR", [], Agora));
        Assert.Throws<DomainException>(() => Order.Create("c", "cli", "BR",
            [new OrderItem("A", "a", 1, 1m, "BRL", "v1"), new OrderItem("B", "b", 1, 1m, "USD", "v1")], Agora));
    }
}
