# Pedidos e Catálogo – evolução arquitetural

Resposta ao **Desafio Técnico COE Sênior – Arquitetura de Soluções (Stefanini)**.

> **Por onde começar:** leia a seção "Em 1 minuto" abaixo, depois o [resumo executivo](docs/00-resumo-executivo.md) (2 páginas). Para avaliar, use o [roteiro por critério](#3-roteiro-por-critério-de-avaliação). Para conferir item por item do PDF, use a [matriz de rastreabilidade](docs/rastreabilidade.md).

**Versão em PDF para leitura:** [Resumo executivo (2 páginas)](entregaveis/Resumo-executivo.pdf) · [Documentação completa](entregaveis/Documentacao-completa.pdf). Os PDFs são gerados a partir destes `.md`, que são a fonte (ver [scripts/pdf](scripts/pdf/build.mjs)).

## 1. Em 1 minuto

| | |
|-|-|
| **Problema** | A plataforma atende só o web nacional. Em 90 dias precisa atender app, parceiros e um 2º país, com 10x de volume, 99,9 % de disponibilidade e p95 ≤ 500 ms na criação. Tem quatro falhas: pedido duplicado em retry, evento fora da transação, N+1 no Catálogo e falta de snapshot de preço. |
| **Recomendação** | Evoluir o sistema atual em três ondas (30/60/90 dias), sem reescrita e sem janela de indisponibilidade. |
| **Decisão crítica provada** | Criação de pedido idempotente com contrato versionado: a mesma `Idempotency-Key` não cria pedido duplicado, e o `/v2` convive com o `/v1` sem quebrar o consumidor atual. Snapshot e outbox na mesma transação. |
| **Como conferir** | `dotnet test`: 52 testes (prova + fitness functions), sem dependência externa. |

## 2. Ordem de leitura sugerida (≈ 30 min)

| # | Ler | Tempo | O que você vai encontrar |
|---|-----|-------|--------------------------|
| 1 | [00 – Resumo executivo](docs/00-resumo-executivo.md) | 3 min | Recomendação, investimento, riscos, decisões a validar |
| 2 | [01 – Diagnóstico](docs/01-diagnostico.md) | 3 min | Problemas P1–P6, por que essa decisão crítica, lacunas do enunciado que viraram premissas |
| 3 | [02 – Arquitetura](docs/02-arquitetura.md) | 7 min | C4 atual e alvo, fronteiras de confiança, pontos de falha, atributo → mecanismo, 2 sequências |
| 4 | [03 – Domínios e integração](docs/03-dominios-e-integracao.md) | 5 min | Bounded contexts, ownership, sync × async, snapshot, idempotência, outbox, API pública |
| 5 | [ADRs](docs/adr/README.md) | 4 min | 8 decisões com alternativas, consequências técnicas e operacionais e gatilho para revisitar |
| 6 | [04 – Qualidade](docs/04-qualidade-e-resiliencia.md) · [05 – Threat model](docs/05-threat-model.md) · [06 – Plano 30/60/90](docs/06-plano-30-60-90.md) · [07 – Riscos](docs/07-riscos-premissas-escopo.md) | 6 min | Cenários com métrica, ameaças e LGPD, plano com rollback, débitos e fora de escopo |
| 7 | [Seção 4 deste README](#4-critério-crítico-do-pdf--teste-que-prova) e `tests/` | 2 min | Qual teste prova cada garantia |
| 8 | [Uso de IA](docs/uso-de-ia.md) | 2 min | Ferramenta, prompts, validações, sugestões rejeitadas |

Diferenciais (seção 2.5 do PDF): [08 – IA](docs/08-ia-consulta-de-pedidos.md) · [09 – Governança](docs/09-governanca-de-contratos.md) · [10 – Estimativa](docs/10-estimativa-fase-30-dias.md).

Todo documento começa com um quadro **"Para o avaliador"**: qual item do PDF ele atende, qual critério e o que conferir.

## 3. Roteiro por critério de avaliação

Mesma ordem da seção 3 do PDF.

| # | Critério | Onde olhar | O que conferir | Evidência objetiva |
|---|----------|-----------|----------------|--------------------|
| 1 | **Arquitetura de solução** | [02](docs/02-arquitetura.md) §1–§7 · [04](docs/04-qualidade-e-resiliencia.md) §3 | Cada restrição do PDF (10x, 99,9 %, p95, app, parceiros, 2º país) ligada a um mecanismo | Tabela [atributo → mecanismo](docs/02-arquitetura.md#7-atributos-de-qualidade--mecanismos); [orçamento de latência](docs/04-qualidade-e-resiliencia.md#3-orçamento-de-latência-da-criação-p95) somando 300 ms no pior caso |
| 2 | **Modelagem de domínio e dados** | [03](docs/03-dominios-e-integracao.md) §1–§5 | Context map, quem é dono de cada dado (e do que não é), linguagem, sync × async com motivo | Tabela [Bounded contexts](docs/03-dominios-e-integracao.md#1-bounded-contexts) com colunas "possui" e "não possui" |
| 3 | **Decisões e trade-offs** | [ADRs](docs/adr/README.md) | Alternativas reais com motivo da rejeição; consequências técnicas e operacionais; ADR-005 em "Proposto" por depender do jurídico | Formato verificado no CI por `AdrTests` |
| 4 | **Estratégia de evolução** | [06](docs/06-plano-30-60-90.md) · [04](docs/04-qualidade-e-resiliencia.md) §5 · [07](docs/07-riscos-premissas-escopo.md) | Para cada entrega: convivência, observabilidade e rollback. Migração só aditiva, critério de parada do canário, descomissionamento | Tabelas por fase em [06](docs/06-plano-30-60-90.md) |
| 5 | **Segurança e privacidade** | [05](docs/05-threat-model.md) · [02](docs/02-arquitetura.md) §5 · [ADR-005](docs/adr/ADR-005-residencia-de-dados-por-celula.md) · [ADR-007](docs/adr/ADR-007-autenticacao-e-quotas-da-api-publica.md) | STRIDE por fronteira (T1–T25), identidade, autorização por recurso, residência por país (coluna ou célula mínima), LGPD | Testes `Cliente_de_api_nao_le_pedido_de_outro_cliente` e `Eventos_nao_carregam_dado_pessoal` |
| 6 | **Qualidade da prova** | `contracts/` · `tests/` · [CI](.github/workflows/ci.yml) | OpenAPI v1 e v2, AsyncAPI, testes positivos e negativos, pipeline | [Seção 4](#4-critério-crítico-do-pdf--teste-que-prova) deste README |
| 7 | **Uso eficaz de IA** | [Uso de IA](docs/uso-de-ia.md) · [08](docs/08-ia-consulta-de-pedidos.md) · [docs/ia/](docs/ia/) | Prompts, validações, sugestões rejeitadas, cuidados com dados; guardrails e avaliação do assistente | Tabela [Decisões de IA rejeitadas](docs/uso-de-ia.md#decisões-de-ia-rejeitadas); [casos de avaliação](docs/ia/casos-de-avaliacao.jsonl) |
| 8 | **Atuação sênior** | [00](docs/00-resumo-executivo.md) · [01](docs/01-diagnostico.md) · [07](docs/07-riscos-premissas-escopo.md) | Escolha justificada do que entra nos 30 dias; lacunas viram premissas; débitos aceitos; o que fica fora; sem microsserviços por moda | [Alternativas para a primeira entrega](docs/01-diagnostico.md#decisão-crítica-a-validar-na-fatia); [Fora do escopo](docs/07-riscos-premissas-escopo.md#fora-do-escopo) |

## 4. Critério crítico do PDF → teste que prova

> *"Chamadas repetidas com a mesma chave não podem criar pedidos duplicados, e a evolução de contrato não pode quebrar um consumidor atual demonstrado no teste."*

| Garantia | Teste | Arquivo |
|----------|-------|---------|
| Mesma chave + mesmo payload → mesmo pedido, 1 pedido e 1 evento | `V2_mesma_chave_e_mesmo_payload_devolve_o_pedido_original_sem_duplicar` | `IdempotenciaTests` |
| Chamadas concorrentes com a mesma chave → um único pedido | `Chamadas_concorrentes_com_a_mesma_chave_criam_um_unico_pedido` | `IdempotenciaTests` |
| Mesma chave + payload diferente → `422`, nada gravado | `V2_mesma_chave_com_payload_diferente_retorna_422_e_nao_cria_pedido` | `IdempotenciaTests` |
| `/v2` sem chave → `400`; chave > 64 caracteres → `400` | `V2_sem_chave_retorna_400_e_nao_cria_pedido`, `Chave_com_mais_de_64_caracteres_retorna_400` | `IdempotenciaTests` |
| Chave tem escopo por cliente; expira em 24 h | `Chave_tem_escopo_por_cliente_de_api`, `Chave_expirada_apos_24h_cria_novo_pedido` | `IdempotenciaTests` |
| `/v1` sem chave segue igual; com chave passa a deduplicar | `V1_sem_chave_mantem_o_comportamento_antigo`, `V1_com_chave_passa_a_deduplicar` | `IdempotenciaTests` |
| **Consumidor web atual do `/v1` continua funcionando** (fixture do consumidor) | `Consumidor_web_atual_cria_e_consulta_pedido_no_v1_como_antes` | `ContratoV1ConsumidorTests` |
| Resposta do `/v2` segue o schema da OpenAPI | `Resposta_de_criacao_segue_o_schema_Order_da_OpenAPI_v2` | `ContratoV2Tests` |
| Snapshot preserva preço e descrição após mudança no Catálogo | `Snapshot_preserva_preco_e_descricao_depois_de_mudanca_no_catalogo` | `ContratoV2Tests` |
| Catálogo consultado uma vez por pedido (sem N+1) | `Catalogo_e_consultado_uma_vez_por_pedido_sem_N_mais_1` | `ContratoV2Tests` |
| Evento gravado na outbox na mesma transação; replay não gera evento | `Criacao_grava_evento_na_outbox_junto_com_o_pedido`, `Replay_e_requisicao_rejeitada_nao_geram_evento` | `OutboxTests` |
| País vem do cliente de API: uma instância atende dois países (opção A); célula recusa outro país com `403` (opção B) | `Opcao_A_uma_instancia_grava_o_pais_e_o_preco_do_cliente_de_api`, `Opcao_B_celula_de_um_pais_recusa_cliente_de_outro_pais_sem_gravar_nada` | `PaisTests` |
| Fitness functions: ADRs, camadas, PII em evento, exceções vencidas | pasta `Governanca/` | `AdrTests`, `ArquiteturaTests`, `ContratosTests` |

No CI ([ci.yml](.github/workflows/ci.yml)) também rodam: Spectral (lint OpenAPI), AsyncAPI CLI e oasdiff, que falha se houver mudança que quebra dentro da mesma versão major. Uma prova negativa ([contrato-breaking-autoteste.sh](scripts/contrato-breaking-autoteste.sh)) remove de propósito o campo `total` do v1 e exige que o oasdiff acuse a quebra: se a checagem parar de funcionar, o CI falha.

## 5. Como rodar a prova

Pré-requisito: .NET SDK 8 (versão fixada em `global.json`).

```bash
dotnet test
```

Um comando, sem dependência externa. Os testes usam `WebApplicationFactory` e SQLite em memória, com [dados sintéticos](data/catalogo-sintetico.json).

API local (opcional, grava em `pedidos.db`):

```bash
dotnet run --project src/Pedidos.Api
curl -X POST http://localhost:5000/v2/orders \
  -H "Content-Type: application/json" \
  -H "X-Client-Id: app" \
  -H "Idempotency-Key: 7f3c1c1e-0001" \
  -d '{"customerId":"c-1","items":[{"productId":"SKU-0001","quantity":2}]}'
```

Ao repetir o mesmo comando, a API devolve o mesmo pedido com `Idempotent-Replayed: true`. Ao trocar a quantidade mantendo a chave, devolve `422`.

Lint de contratos (opcional, o CI já faz):

```bash
npx @stoplight/spectral-cli@6.15.0 lint contracts/openapi/*.yaml --ruleset .spectral.yaml
npx @asyncapi/cli@3.4.2 validate contracts/asyncapi/order-status.yaml
```

## 6. Premissas da fatia executável

- Sem gateway: o header `X-Client-Id` simula a claim `client_id` do token. `/v1` sem o header assume `web`; `/v2` sem o header devolve `401`.
- O Catálogo é um stub em memória com dados sintéticos. Em produção seria o serviço real, consultado em lote.
- A outbox é gravada na transação; não há relay nem broker na fatia (entram no plano de 60 dias).
- O header `X-Client-Country` simula a claim `country` do `client_id` (cada cliente de API é vinculado a um país). Sem o header, vale o país padrão (`Cell:Country`, `BR`), como no consumidor atual do `/v1`. `Cell:Countries` lista os países que a instância atende: `["BR"]` simula a célula (opção B do ADR-005); `["BR","P2"]` simula a opção A. Retenção da chave: 24 h (`Cell:IdempotencyRetention`).
- O hash de idempotência inclui a versão da API: a mesma chave em `/v1` e `/v2` → `422`.
- O SQLite substitui o banco real. Prova a regra (UNIQUE + mesma transação), não o desempenho (p95, 10x) nem a concorrência do banco de produção. Esses pontos estão em [04](docs/04-qualidade-e-resiliencia.md) e no [ADR-001](docs/adr/ADR-001-idempotencia-na-criacao.md).

As premissas da arquitetura (tráfego, país, nuvem) estão em [07 – Premissas](docs/07-riscos-premissas-escopo.md#premissas-validar-com-o-cliente).

## 7. Índice dos artefatos

| Artefato pedido no PDF (seção 4) | Arquivo |
|----------------------------------|---------|
| README, premissas, instruções, índice | este arquivo |
| Diagramas C4 e de sequência (Mermaid) | [02 – Arquitetura](docs/02-arquitetura.md) |
| Mapa de domínios | [03 – Domínios e integração](docs/03-dominios-e-integracao.md) |
| ADRs (8) | [docs/adr](docs/adr/README.md) |
| Threat model | [05](docs/05-threat-model.md) |
| Plano 30/60/90 | [06](docs/06-plano-30-60-90.md) |
| OpenAPI v1 e v2 | [v1](contracts/openapi/orders-v1.yaml) · [v2](contracts/openapi/orders-v2.yaml) |
| AsyncAPI 3.0 (`order.status-changed`) | [order-status](contracts/asyncapi/order-status.yaml) |
| Fatia executável e testes | `src/` · `tests/Pedidos.Tests` |
| Documento de uso de IA | [uso-de-ia](docs/uso-de-ia.md) |
| Resumo executivo (2 páginas) | [00](docs/00-resumo-executivo.md) |
| Apresentação (desejável) | [apresentacao.pptx](entregaveis/apresentacao.pptx) · [PDF](entregaveis/apresentacao.pdf) |
| Conferência item a item | [rastreabilidade](docs/rastreabilidade.md) |
| Versões em PDF (geradas dos `.md`) | [Resumo executivo](entregaveis/Resumo-executivo.pdf) · [Documentação completa](entregaveis/Documentacao-completa.pdf) |

Outros: [01 – Diagnóstico](docs/01-diagnostico.md) · [04 – Qualidade e resiliência](docs/04-qualidade-e-resiliencia.md) · [07 – Riscos e escopo](docs/07-riscos-premissas-escopo.md) · [08 – IA](docs/08-ia-consulta-de-pedidos.md) · [09 – Governança](docs/09-governanca-de-contratos.md) · [10 – Estimativa](docs/10-estimativa-fase-30-dias.md) · [Exceções](docs/excecoes.md) · [docs/ia](docs/ia/)

## 8. Estrutura

```
contracts/   OpenAPI (v1, v2) e AsyncAPI
data/        catálogo sintético
src/         Domain, Application, Infrastructure, Api
tests/       prova executável e fitness functions (Governanca/)
docs/        arquitetura, ADRs, governança, IA
entregaveis/ resumo executivo e documentação completa em PDF (gerados dos .md) e slides (pptx e pdf)
scripts/     checagem de quebra de contrato (oasdiff); geração dos PDFs (pdf/)
```
