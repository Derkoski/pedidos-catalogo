# Rastreabilidade: cada item do PDF → onde está

Legenda: ✅ atendido no repositório · ⏳ depende de ação fora do repositório (indicada na linha).

## 2.2.1 Contexto e restrições

| Restrição do PDF | Onde está tratada | Evidência |
|------------------|-------------------|-----------|
| 10x de tráfego | [02 §7](02-arquitetura.md#7-atributos-de-qualidade--mecanismos) · [04 §1–§2](04-qualidade-e-resiliencia.md#1-premissa-de-carga) | Cenário Q1 (200 criações/s) |
| 99,9 % de disponibilidade mensal | [04 §2](04-qualidade-e-resiliencia.md#2-cenários) Q3–Q5 | Métrica e critério de aceite por cenário |
| p95 ≤ 500 ms na criação | [04 §3](04-qualidade-e-resiliencia.md#3-orçamento-de-latência-da-criação-p95) | Orçamento de latência por etapa (300 ms no pior caso) |
| N+1 síncrono no Catálogo | [01 P3](01-diagnostico.md#problemas-e-impacto) · [ADR-003](adr/ADR-003-snapshot-e-catalogo-em-lote.md) | Teste `Catalogo_e_consultado_uma_vez_por_pedido_sem_N_mais_1` |
| Sem snapshot de preço/descrição | [01 P4](01-diagnostico.md#problemas-e-impacto) · [03 §5.1](03-dominios-e-integracao.md#51-snapshot-de-preço) | Teste `Snapshot_preserva_preco_e_descricao_depois_de_mudanca_no_catalogo` |
| Retry sem idempotency key; evento sem garantia transacional | [01 P1–P2](01-diagnostico.md#problemas-e-impacto) · [ADR-001](adr/ADR-001-idempotencia-na-criacao.md) · [ADR-002](adr/ADR-002-outbox-transacional.md) | `IdempotenciaTests`, `OutboxTests` |
| Parceiros: API pública versionada e notificação assíncrona | [03 §6](03-dominios-e-integracao.md#6-api-pública) · [ADR-004](adr/ADR-004-versionamento-de-api.md) · [ADR-006](adr/ADR-006-webhooks-para-parceiros.md) | OpenAPI v2 · AsyncAPI |
| LGPD e residência de dados no novo país | [05 LGPD](05-threat-model.md#lgpd--pontos-específicos) · [ADR-005](adr/ADR-005-residencia-de-dados-por-celula.md) | Coluna `country` vinda do cliente de API + célula mínima condicionada ao parecer; testes `PaisTests` e `Eventos_nao_carregam_dado_pessoal` |
| Contratos atuais compatíveis por 6 meses | [ADR-004](adr/ADR-004-versionamento-de-api.md) · [09 §1](09-governanca-de-contratos.md#1-política-de-evolução-de-contratos) | Teste `Consumidor_web_atual_cria_e_consulta_pedido_no_v1_como_antes` |
| Primeira melhoria em produção em 30 dias, sem indisponibilidade | [06 Dias 1–30](06-plano-30-60-90.md#dias-130-primeira-melhoria-segura-em-produção) | Migração só aditiva, flags e canário |

## 2.3.1 Visões arquiteturais

| Item do PDF | Onde está | Situação |
|-------------|-----------|----------|
| C4 de contexto – estado atual | [02 §1](02-arquitetura.md#1-contexto--atual) | ✅ |
| C4 de contêineres – estado atual | [02 §2](02-arquitetura.md#2-contêineres--atual) | ✅ |
| C4 de contexto – alvo | [02 §3](02-arquitetura.md#3-contexto--alvo) | ✅ |
| C4 de contêineres – alvo | [02 §4](02-arquitetura.md#4-contêineres--alvo) | ✅ |
| Sequência: criação de pedido | [02 §8](02-arquitetura.md#8-sequência--criação-de-pedido-v2) | ✅ |
| Sequência: atualização/notificação de status | [02 §9](02-arquitetura.md#9-sequência--atualização-e-notificação-de-status) | ✅ |
| Componentes e dependências externas | [02 §4](02-arquitetura.md#4-contêineres--alvo) (tabela de contêineres e dependências) | ✅ |
| Fronteiras de confiança | [02 §5](02-arquitetura.md#5-fronteiras-de-confiança) | ✅ |
| Pontos de falha (atual e alvo) | [02 §2](02-arquitetura.md#2-contêineres--atual) e [02 §6](02-arquitetura.md#6-pontos-de-falha-no-alvo) | ✅ |
| Atributo de qualidade → mecanismo | [02 §7](02-arquitetura.md#7-atributos-de-qualidade--mecanismos) | ✅ |

## 2.3.2 Domínios, dados e integração

| Item do PDF | Onde está | Situação |
|-------------|-----------|----------|
| Bounded contexts e responsabilidades | [03 §1](03-dominios-e-integracao.md#1-bounded-contexts) | ✅ |
| Ownership de dados | [03 §3](03-dominios-e-integracao.md#3-ownership-de-dados) | ✅ |
| Linguagem de domínio | [03 §2](03-dominios-e-integracao.md#2-linguagem-de-domínio) | ✅ |
| Síncrono × assíncrono (consistência, latência, acoplamento) | [03 §4](03-dominios-e-integracao.md#4-síncrono-vs-assíncrono) | ✅ |
| Snapshot de preço | [03 §5.1](03-dominios-e-integracao.md#51-snapshot-de-preço) | ✅ |
| Idempotência | [03 §5.2](03-dominios-e-integracao.md#52-idempotência) | ✅ |
| Publicação confiável (outbox) | [03 §5.3](03-dominios-e-integracao.md#53-publicação-confiável) | ✅ |
| Deduplicação | [03 §5.4](03-dominios-e-integracao.md#54-deduplicação) | ✅ |
| Reconciliação | [03 §5.5](03-dominios-e-integracao.md#55-reconciliação) | ✅ |
| API pública: autenticação, autorização, quotas, versionamento | [03 §6](03-dominios-e-integracao.md#6-api-pública) · [ADR-007](adr/ADR-007-autenticacao-e-quotas-da-api-publica.md) · [ADR-004](adr/ADR-004-versionamento-de-api.md) | ✅ |
| No mínimo 4 ADRs (contexto, alternativas, decisão, consequências) | [docs/adr](adr/README.md): 8 ADRs | ✅ formato checado por `AdrTests` |

## 2.4.1 Migração e atributos de qualidade

| Item do PDF | Onde está | Situação |
|-------------|-----------|----------|
| Plano 30/60/90 | [06](06-plano-30-60-90.md) | ✅ |
| Convivência, observabilidade e rollback | [06](06-plano-30-60-90.md): colunas em cada fase · [06 Rollback](06-plano-30-60-90.md#rollback--regras-gerais) | ✅ |
| Descomissionamento | [06 Dias 61–90](06-plano-30-60-90.md#dias-6190-segundo-país-e-parceiros) | ✅ |
| Cenários: disponibilidade, performance, segurança, auditabilidade, recuperação, com métrica e critério de aceite | [04 §2](04-qualidade-e-resiliencia.md#2-cenários) (Q1–Q13) | ✅ |
| Threat model: identidade, APIs públicas, dados, eventos, dependências | [05](05-threat-model.md#ameaças-e-controles) (T1–T25, agrupados por área) | ✅ |
| Timeout, retry com backoff, circuit breaker, bulkhead, degradação controlada | [04 §4](04-qualidade-e-resiliencia.md#4-resiliência--evitar-efeito-cascata) | ✅ |
| Riscos, premissas, débitos aceitos, fora de escopo | [07](07-riscos-premissas-escopo.md) | ✅ |

## 2.4.2 Contratos e fatia executável

| Item do PDF | Onde está | Situação |
|-------------|-----------|----------|
| OpenAPI versionada: criação e consulta de pedidos | [orders-v1.yaml](../contracts/openapi/orders-v1.yaml) · [orders-v2.yaml](../contracts/openapi/orders-v2.yaml) | ✅ |
| AsyncAPI para mudança de status | [order-status.yaml](../contracts/asyncapi/order-status.yaml) | ✅ |
| Prova mínima de decisão crítica | `src/` (idempotência + outbox + compatibilidade v1/v2) | ✅ |
| Testes positivos e negativos reproduzíveis | `tests/Pedidos.Tests` · lista no [README §4](../README.md#4-critério-crítico-do-pdf--teste-que-prova) | ✅ |
| Execução automatizada no pipeline | [ci.yml](../.github/workflows/ci.yml) | ✅ pronto · ⏳ primeira execução quando o repositório for publicado no GitHub |
| Dados sintéticos | [catalogo-sintetico.json](../data/catalogo-sintetico.json) | ✅ |
| Um único comando documentado | `dotnet test` · [README §5](../README.md#5-como-rodar-a-prova) | ✅ |
| **Critério crítico:** mesma chave não duplica | `IdempotenciaTests` | ✅ |
| **Critério crítico:** evolução de contrato não quebra consumidor atual | `ContratoV1ConsumidorTests` + oasdiff no CI | ✅ |

## 2.5 Diferenciais

| Item do PDF | Onde está | Situação |
|-------------|-----------|----------|
| 2.5.1 Capacidade de IA: isolamento, minimização, guardrails, observabilidade, avaliação | [08](08-ia-consulta-de-pedidos.md#controles) · [docs/ia](ia/) (prompt de sistema e casos de avaliação) | ✅ |
| 2.5.1 Uso de IA no desafio: prompts, validações, decisões rejeitadas | [uso-de-ia](uso-de-ia.md) | ✅ |
| 2.5.2 Validação de OpenAPI/AsyncAPI, ADRs e regras arquiteturais como fitness functions no CI | [09 §2](09-governanca-de-contratos.md#2-fitness-functions-no-ci) · `tests/Pedidos.Tests/Governanca` · [ci.yml](../.github/workflows/ci.yml) | ✅ |
| 2.5.2 Política de evolução de contratos e processo leve de exceção | [09 §1](09-governanca-de-contratos.md#1-política-de-evolução-de-contratos) · [09 §3](09-governanca-de-contratos.md#3-processo-leve-de-exceção-técnica) · [excecoes](excecoes.md) | ✅ |
| 2.5.3 Estimativa: esforço, time, custos, premissas, riscos | [10](10-estimativa-fase-30-dias.md) | ✅ |

## 4. Entrega do desafio

| Item do PDF | Onde está | Situação |
|-------------|-----------|----------|
| Repositório no GitHub com README, premissas, instruções e índice | [README](../README.md) | ✅ conteúdo · ⏳ publicação |
| Diagramas C4 e de sequência em formato versionável | Mermaid em [02](02-arquitetura.md) | ✅ |
| Mapa de domínios, ≥ 4 ADRs, threat model, plano 30/60/90 | [03](03-dominios-e-integracao.md) · [ADRs](adr/README.md) · [05](05-threat-model.md) · [06](06-plano-30-60-90.md) | ✅ |
| OpenAPI, AsyncAPI, fatia executável e testes | `contracts/` · `src/` · `tests/` | ✅ |
| Documento da ferramenta de IA: motivo, prompts, validações, cuidados com dados | [uso-de-ia](uso-de-ia.md) | ✅ |
| Resumo executivo de até 2 páginas: recomendação, investimento, riscos, decisões a validar | [00](00-resumo-executivo.md) | ✅ |
| Desejável: vídeo ou slides | [apresentacao.pptx](../entregaveis/apresentacao.pptx) | ✅ 10 slides |

## 3. Critérios de avaliação

O roteiro por critério (onde olhar, o que conferir e qual a evidência) está no [README §3](../README.md#3-roteiro-por-critério-de-avaliação).
