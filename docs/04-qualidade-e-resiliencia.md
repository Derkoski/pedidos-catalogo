# 04 – Cenários de qualidade e resiliência

## 1. Premissa de carga
Sem baseline informado (premissa 2 do [diagnóstico](01-diagnostico.md)). Ordem de grandeza usada:

| | Hoje (premissa) | Alvo 10x |
|-|-----------------|----------|
| Criação de pedido no pico | 20/s | 200/s |
| Consulta de pedido no pico | 100/s | 1.000/s |
| Itens por pedido | média 4, máx. 100 | igual |

Os números são para dimensionar teste de carga. Trocar pelo baseline real na primeira semana.

## 2. Cenários

| # | Atributo | Estímulo | Resposta esperada | Métrica / critério de aceite |
|---|----------|----------|-------------------|------------------------------|
| Q1 | Performance | 200 criações/s, pedidos de 1 a 100 itens | Criação concluída | p95 ≤ 500 ms, p99 ≤ 1 s, medido no gateway, por 30 min |
| Q2 | Performance | 1.000 consultas/s | Pedido retornado | p95 ≤ 200 ms (meta proposta) |
| Q3 | Disponibilidade | Mês de operação | API de criação responde | ≥ 99,9 % de requisições não-5xx por mês (≤ 43 min de indisponibilidade) |
| Q4 | Disponibilidade | Catálogo fora por 10 min | Criação continua com o último preço em cache (stale-if-error até 15 min) | ≥ 95 % das criações com sucesso; nenhuma falha em Pedidos fora da criação |
| Q5 | Disponibilidade | Broker fora por 30 min | Pedidos continua criando | 0 eventos perdidos; atraso drenado em ≤ 15 min após volta |
| Q6 | Consistência | Mesmo pedido enviado 5x com a mesma chave, inclusive em paralelo | 1 pedido criado | 0 duplicatas; 4 respostas com `Idempotent-Replayed: true` ou `409` |
| Q7 | Consistência | Processo cai entre commit e publish | Evento publicado depois | 100 % dos pedidos com evento correspondente em ≤ 5 min |
| Q8 | Auditabilidade | Preço muda no Catálogo após o pedido | Pedido mantém preço e descrição originais | 100 % dos itens com snapshot; histórico de status com autor e data |
| Q9 | Segurança | Parceiro A tenta ler pedido do parceiro B | Acesso negado | `404` (não revela existência); evento de auditoria registrado |
| Q10 | Segurança | Parceiro excede quota | Requisições excedentes rejeitadas | `429` em < 50 ms; outros parceiros sem impacto no p95 |
| Q11 | Recuperação | Perda do banco primário da célula | Failover para réplica | RTO ≤ 15 min, RPO ≤ 1 min (meta proposta) |
| Q12 | Recuperação | Deploy com erro | Volta à versão anterior | Rollback por feature flag em ≤ 5 min; por versão em ≤ 15 min |
| Q13 | Evolução | Nova versão do contrato | Consumidor v1 continua funcionando | Teste de contrato v1 verde no CI; 0 erros 4xx novos no v1 após deploy |

## 3. Orçamento de latência da criação (p95)

| Etapa | Orçamento |
|-------|-----------|
| Gateway (TLS, token, quota) | 30 ms |
| Leitura da chave de idempotência | 10 ms |
| Catálogo em lote (cache miss) | 150 ms (timeout, sem retry) |
| Transação (pedido + itens + chave + outbox) | 60 ms |
| Serialização e rede | 50 ms |
| **Total (pior caso, cache miss)** | **300 ms** (folga de 200 ms para o p95 de 500 ms) |

Com cache quente, o Catálogo cai para < 5 ms. O cache fica na mesma região da API de Pedidos (inclusive na célula do País 2, se existir), então nenhum pedido paga a ida e volta entre regiões no caminho quente.

## 4. Resiliência – evitar efeito cascata

| Mecanismo | Onde | Configuração inicial |
|-----------|------|----------------------|
| Timeout | Pedidos → Catálogo | 150 ms |
| Timeout | Gateway → Pedidos | 2 s |
| Timeout | Notificador → parceiro | 5 s |
| Retry | Pedidos → Catálogo | Nenhum no caminho síncrono: erro ou timeout vai direto para o cache (stale-if-error). Um retry interno não caberia no orçamento (150 + 150 + 150 ms > 500 ms); quem repete é o cliente, com a mesma `Idempotency-Key` |
| Retry com backoff + jitter | Notificador → parceiro | Exponencial (1 s … 1 h), até 24 h |
| Retry | Cliente → Pedidos | Seguro por causa da `Idempotency-Key`; documentado no contrato |
| Circuit breaker | Pedidos → Catálogo | Abre com 50 % de falha em 20 chamadas / 10 s; meio-aberto após 30 s |
| Bulkhead | Pedidos | Pool de conexões separado para criação e consulta |
| Bulkhead | Notificador | Fila e workers por parceiro |
| Rate limit / quota | Gateway | Por `client_id` (ver [ADR-007](adr/ADR-007-autenticacao-e-quotas-da-api-publica.md)) |
| Load shedding | Pedidos | Rejeita com `503` quando a fila interna passa do limite, antes de saturar o banco |
| Degradação controlada | Criação | Catálogo fora → usa o último preço em cache por até 15 min (stale-if-error; TTL normal 60 s); item sem cache → `503` + `Retry-After` |
| Degradação controlada | Notificação | Broker fora → outbox acumula; criação segue |

Regra: retry só em operação idempotente e sempre com teto. Nunca retry em cascata (só uma camada repete).

## 5. Observabilidade

- OpenTelemetry em gateway, Pedidos, relay e notificador. `traceparent` propagado até o evento (campo no header da mensagem).
- SLOs com alerta por *burn rate*: disponibilidade da criação (99,9 %), latência p95 da criação (500 ms).
- Métricas de negócio: pedidos criados, replays de idempotência, `422`/`409` por cliente, idade da linha mais antiga na outbox, tamanho da DLQ, uso por versão de API.
- Logs estruturados sem dados pessoais (mascaramento no coletor).
