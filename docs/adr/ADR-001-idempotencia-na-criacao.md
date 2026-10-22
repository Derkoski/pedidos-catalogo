# ADR-001 – Idempotência na criação de pedido

Status: Aceito
Data: 2026-10-21

## Contexto
Clientes fazem retry após timeout. Sem chave de idempotência, cada retry pode criar outro pedido. Com 10x de tráfego, timeouts e retries aumentam. App e parceiros também vão fazer retry automático. O contrato v1 não tem chave e não pode quebrar.

## Alternativas
1. **Deduplicar por conteúdo** (mesmo cliente + mesmos itens em X minutos). Falso positivo: cliente que quer comprar duas vezes a mesma coisa. Rejeitada.
2. **Chave gerada pelo servidor em duas etapas** (reserva um ID, depois confirma). Muda o fluxo do cliente e dobra as chamadas. Rejeitada.
3. **Header `Idempotency-Key` enviado pelo cliente, persistido com hash do payload.** Padrão de mercado (draft IETF `Idempotency-Key`). Escolhida.
4. Guardar a chave só em cache (Redis). Mais rápido, mas perde a garantia se o cache cair ou expirar antes da hora, e não fica na mesma transação do pedido. Rejeitada.

## Decisão
- Header `Idempotency-Key` (UUID recomendado, até 64 caracteres).
- Tabela `idempotency_keys` no banco de Pedidos, UNIQUE em `(client_id, key)`, gravada na mesma transação do pedido, com hash SHA-256 do payload normalizado e a resposta.
- Mesma chave e mesmo hash: devolve a resposta original (mesmo status e corpo) e `Idempotent-Replayed: true`.
- Mesma chave e hash diferente: `422`.
- Corrida com a mesma chave: a UNIQUE barra a segunda gravação, que relê a chave e devolve a resposta original; `409` só se o vencedor ainda não estiver visível (cliente repete).
- v1: opcional. v2: obrigatória (`400` sem ela).
- Retenção: 24 h, depois expurgo em lote.

## Consequências
**Técnicas**
- Retry fica seguro, o que permite usar retry e circuit breaker sem medo de duplicar.
- Uma escrita a mais por pedido e um índice único. Custo baixo.
- Consumidor v1 que não manda a chave continua podendo duplicar. Mitigação: comunicar e migrar o web para mandar a chave no v1 nas primeiras semanas.
- Retry depois de 24 h cria outro pedido. Documentado no contrato.
- A fatia executável usa SQLite: prova a regra (UNIQUE + mesma transação), não a concorrência do banco de produção (isolamento, locks). O teste de corrida será repetido no banco escolhido antes do canário (plano de 30 dias).

**Operacionais**
- Métricas por `client_id`: replays, `422` e `409`. Pico de `422` indica cliente reaproveitando a chave entre compras diferentes; o runbook orienta gerar uma chave por intenção de compra.
- Job diário apaga chaves vencidas; alerta se a tabela crescer acima do previsto para o volume.
- Antes do canário, o teste de corrida roda no banco de produção escolhido.

**Quando revisitar**
- Tabela de chaves pesando no banco: particionar por data de expiração.
- Parceiro pedir retenção maior que 24 h.
