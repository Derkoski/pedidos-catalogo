# ADRs

Formato obrigatório (checado no CI): título `# ADR-NNN – ...`, linha `Status:` e as seções `## Contexto`, `## Alternativas`, `## Decisão`, `## Consequências`.

Status possíveis: `Proposto`, `Aceito`, `Substituído por ADR-NNN`, `Rejeitado`, com observação opcional entre parênteses.

| ADR | Título | Status | O que ganhamos | O que pagamos |
|-----|--------|--------|----------------|---------------|
| [001](ADR-001-idempotencia-na-criacao.md) | Idempotência na criação de pedido | Aceito | Retry seguro, zero duplicata | Uma escrita a mais por pedido; v1 sem chave ainda duplica |
| [002](ADR-002-outbox-transacional.md) | Outbox transacional para eventos de pedido | Aceito | Evento existe se e só se o pedido existe | Atraso de segundos; consumidor precisa deduplicar |
| [003](ADR-003-snapshot-e-catalogo-em-lote.md) | Snapshot de preço e consulta em lote ao Catálogo | Aceito | Latência estável, auditoria de preço | Preço até 60 s defasado (15 min com o Catálogo fora) |
| [004](ADR-004-versionamento-de-api.md) | Versionamento de API por URL e convivência v1/v2 | Aceito | Nenhum consumidor quebra | Dois adaptadores por 6+ meses |
| [005](ADR-005-residencia-de-dados-por-celula.md) | Residência de dados do segundo país: coluna de país ou célula mínima | Proposto | Não gasta antes de a regra ser conhecida | Decisão depende do jurídico até o dia 30 |
| [006](ADR-006-webhooks-para-parceiros.md) | Webhooks assinados para notificar parceiros | Aceito | Parceiro notificado sem acesso à infraestrutura | Mais um componente (notificador) e DLQ para operar |
| [007](ADR-007-autenticacao-e-quotas-da-api-publica.md) | Autenticação, autorização e quotas da API pública | Aceito | Um parceiro não derruba os outros | Dependência do provedor de identidade; onboarding |
| [008](ADR-008-evoluir-o-sistema-atual.md) | Evoluir o sistema atual em vez de reescrever em microsserviços | Aceito | Primeira entrega em 30 dias, sem big bang | Serviço de Pedidos escala como um bloco só |

Toda seção de Consequências separa **Técnicas**, **Operacionais** e **Quando revisitar**.
