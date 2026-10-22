# ADR-003 – Snapshot de preço e consulta em lote ao Catálogo

Status: Aceito
Data: 2026-10-21

## Contexto
Pedidos chama o Catálogo uma vez por item (N+1). Pedido grande estoura o p95 de 500 ms e Catálogo fora derruba Pedidos. Preço e descrição não são gravados no pedido, então não dá para auditar o que o cliente pagou depois que o Catálogo muda.

## Alternativas
1. **Manter referência ao Catálogo e versionar preços lá.** Auditoria depende do histórico do Catálogo e de outra chamada a cada leitura. Rejeitada.
2. **Réplica local do Catálogo em Pedidos via eventos.** Remove a chamada síncrona, mas traz preço possivelmente desatualizado no momento da venda e mais um modelo para manter. Fica como evolução se o Catálogo virar gargalo.
3. **Consulta em lote + cache curto + snapshot no item.** Uma chamada por pedido, preço atual, auditoria resolvida. Escolhida.

## Decisão
- Catálogo expõe `GET /products?ids=...&country=` (até 100 IDs).
- Pedidos faz no máximo uma chamada por pedido, só para os itens fora do cache, com timeout de 150 ms, sem retry interno e com circuit breaker. Um retry não caberia no p95 de 500 ms; o cliente repete com a mesma `Idempotency-Key`.
- Cache em Pedidos, na mesma região da API, por produto e país, com TTL de 60 s.
- Cada item grava `unitPrice`, `currency`, `description`, `catalogVersion`. Itens são imutáveis.
- Vale o preço do momento da criação (premissa a validar).
- Se o Catálogo falhar ou o circuit breaker abrir, usa o último preço em cache por até 15 min (stale-if-error); sem cache para algum item, responde `503` com `Retry-After`.
- O snapshot grava o `catalogVersion` usado, venha do Catálogo ou do cache.

## Consequências
**Técnicas**
- Latência da criação deixa de crescer com o número de itens.
- Auditoria simples: o pedido é autocontido.
- Preço pode estar até 60 s defasado em relação ao Catálogo em operação normal, e até 15 min com o Catálogo fora. Precisa de aceite do negócio; a alternativa é recusar o pedido (`503`) durante a queda. Promoção relâmpago pode exigir TTL menor ou invalidação por evento.
- Auditoria continua exata: o `catalogVersion` no item mostra qual versão de preço foi cobrada.
- Mais armazenamento por item (texto da descrição). Custo baixo.

**Operacionais**
- Painel: taxa de acerto do cache, estado do circuit breaker, % de pedidos criados com preço vindo do cache vencido (stale-if-error).
- Alerta: circuit breaker aberto por mais de 1 min.
- Runbook de promoção: invalidar o cache dos produtos alterados.

**Quando revisitar**
- Catálogo virar gargalo, ou o negócio não aceitar a defasagem de preço: réplica do Catálogo por eventos (alternativa 2).
