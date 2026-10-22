# ADR-002 – Outbox transacional para eventos de pedido

Status: Aceito
Data: 2026-10-21

## Contexto
Hoje o evento é publicado fora da transação do banco. Se o commit passa e o publish falha, o evento some. Se o publish passa e o commit falha, sai evento de pedido que não existe. Parceiros vão receber notificações de status, então a inconsistência fica visível para fora.

## Alternativas
1. **Publicar depois do commit, com retry em memória.** Perde evento se o processo cair. Rejeitada.
2. **Transação distribuída (2PC) banco + broker.** Poucos brokers suportam, acopla disponibilidade e aumenta latência. Rejeitada.
3. **CDC (Debezium ou equivalente) lendo o log do banco.** Robusto, mas exige acesso ao log, operação de conectores e depende do banco atual, que não conhecemos. Fica como evolução.
4. **Tabela outbox na mesma transação + relay por polling.** Simples, independente de banco e broker. Escolhida.

## Decisão
- Toda mudança de pedido grava uma linha na `outbox` na mesma transação.
- `eventId` = ID da linha da outbox (UUID), estável entre republicações.
- Relay separado lê em lote por ordem de criação, publica com chave de partição `orderId` e marca `published_at`.
- Entrega pelo menos uma vez. Consumidores deduplicam por `eventId`.
- Linhas publicadas são apagadas após 7 dias.
- Na fatia executável só a gravação na outbox é provada; relay e broker entram no plano de 60 dias.

## Consequências
**Técnicas**
- Evento existe se, e só se, a mudança foi gravada.
- Atraso de publicação de alguns segundos (intervalo do polling). Aceitável para notificação de status.
- Possível duplicata: todo consumidor precisa ser idempotente.
- Carga de leitura extra no banco de Pedidos. Mitigação: índice em `published_at IS NULL`, lote e intervalo configuráveis. Se virar gargalo, migrar para CDC sem mudar o contrato do evento.

**Operacionais**
- Alerta: linha mais antiga não publicada há mais de 5 min (Q7). Painel com atraso do relay e tamanho da outbox.
- Runbook "relay parado": reiniciar o relay; a outbox drena sozinha, nenhum evento se perde.
- Dupla publicação (caminho antigo + outbox) até o último consumidor migrar, com lista de consumidores e dono de cada um.

**Quando revisitar**
- Polling pesando no banco ou atraso de publicação acima de 30 s no p95: migrar para CDC, sem mudar o contrato do evento.
