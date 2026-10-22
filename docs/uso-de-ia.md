# Uso de IA no desafio

> **Para o avaliador**  
> **Item do PDF:** Seção 4 do PDF (documento de IA) e 2.5.1 (uso de IA no desafio)  
> **Critério de avaliação:** Uso eficaz de IA  
> **O que conferir aqui:** Ferramenta e motivo; prompts relevantes; como cada saída foi validada; sugestões da IA que foram rejeitadas e por quê; cuidados com dados.

## Ferramenta e motivo
Claude Code (Anthropic), em sessões separadas por fase, com um arquivo de contexto e um controle de fases mantidos fora do repositório.

- **Opus** nas fases de diagnóstico e arquitetura: decisões e trade-offs.
- **Sonnet** nas fases de código e documentação: execução com escopo fechado.
- Por que: lê e edita o repositório, roda `dotnet test` e os linters, e corrige a partir do resultado. Permite exigir prova executável em vez de texto.
- Requisitos extraídos do PDF uma vez, para um arquivo de texto. Evita reenviar o PDF e reduz erro de leitura.

## Método
1. Uma fase por sessão, com saída definida (diagnóstico, arquitetura, fatia, revisão).
2. Cada fase começa com o mesmo prompt curto e termina atualizando o controle de fases com decisões e notas para a próxima.
3. Escopo travado: só o que o PDF pede, incluindo os diferenciais da seção 2.5.
4. Nada gerado é aceito sem checagem (ver abaixo).

## Prompts relevantes

| Fase | Prompt (resumo) | Para quê |
|------|-----------------|----------|
| Todas | "Leia CLAUDE.md e FASES.md. Execute a Fase N. Só gere os arquivos, sem explicar. No fim, atualize FASES.md." | Contexto estável; saída só em arquivos |
| 1 | Escolher a decisão crítica entre idempotência, outbox, contrato e compatibilidade, justificando o que o critério do PDF exige | Evitar prova que não cobre o critério |
| 2 | ADRs com contexto, alternativas, decisão e consequências; alternativas reais, não espantalhos | ADRs úteis para decidir |
| 2 | Estimar a fase de 30 dias com premissas, time, custo e riscos explícitos | Diferencial 2.5.3 |
| 3 | Fatia mínima: idempotência na mesma transação do pedido, outbox, consumidor v1 como fixture, testes positivos e negativos | Prova do critério crítico |
| 3 | Transformar a política de contratos em fitness functions no CI (Spectral, AsyncAPI CLI, oasdiff, ADR, NetArchTest, PII, exceções vencidas) | Diferencial 2.5.2 |
| 4 | README, este documento e slides; cruzar cada item do requisito com um artefato | Fechar lacunas |
| 6 | Avaliação simulada critério a critério (ver seção abaixo), com o candidato conferindo cada ponto antes de aplicar | Achar e corrigir o que um avaliador exigente pegaria |
| 5 | Revisar para leitura humana: guia do avaliador no README, matriz de rastreabilidade item a item do PDF, quadro "Para o avaliador" em cada documento; completar a IA com prompt de sistema, casos de avaliação e ameaças T20–T22 | Facilitar a conferência; fechar lacunas da 2.5.1 |

## Validações feitas

| O que | Como |
|-------|------|
| Comportamento | `dotnet test`: 46 testes na fase 5 e 52 após a fase 6, com 4 execuções seguidas sem falha intermitente |
| Testes realmente pegam o defeito | Contrato quebrado de propósito: Spectral e oasdiff falharam como esperado |
| Contratos | Spectral sem erros; AsyncAPI CLI válido |
| ADRs | Checagem automática pegou desvio real no ADR-005 (status com observação); o formato foi ajustado |
| Arquitetura de código | NetArchTest: domínio sem dependência de infraestrutura nem ASP.NET |
| Evento sem dado pessoal | Teste compara o payload da outbox com os campos do schema AsyncAPI |
| Números da estimativa | A primeira versão usava taxa horária sem fonte (R$ 160/h). Foi rejeitada na revisão humana e refeita com salários CLT informados pela empresa, fator de encargos detalhado e separação entre custo e preço |
| Cobertura do PDF | Revisão item a item contra o enunciado; resultado em [rastreabilidade.md](rastreabilidade.md) |
| Links da documentação | Script conferiu que todo link relativo e toda âncora de seção existem |
| CI no GitHub | Workflow verde no primeiro push (contratos e testes): https://github.com/Derkoski/pedidos-catalogo/actions/runs/39844784059 |

## Decisões de IA rejeitadas

| Sugestão | Por que rejeitada |
|----------|-------------------|
| Idempotência com cache Redis | Cache pode perder a chave e duplicar pedido. A chave fica no banco, na mesma transação do pedido (ADR-001) |
| Publicar o evento direto no broker após o commit | Reabre a janela de evento perdido. Outbox transacional (ADR-002) |
| Relay e broker reais na fatia | Superdimensiona a prova. A fatia prova a gravação atômica; o relay é do plano de 60 dias |
| Fixture do consumidor v1 estrita (falha com campo extra) | Consumidor real tolera campos novos. A fixture tolera extras e falha em campo removido ou com tipo trocado |
| Microsserviços novos para Pedidos e Catálogo desde o início | Sem evidência de necessidade. Evolução do sistema atual, com célula mínima por país só se a lei exigir |
| Célula completa por país (banco, broker e notificador na região) | Revisão humana na avaliação final: os eventos não têm dado pessoal, então duplicar broker e notificador era custo sem ganho de conformidade. Ficou coluna de país + célula mínima condicionada ao parecer (ADR-005) |
| C4 com a sintaxe nativa `C4Context`/`C4Container` do Mermaid | Testada e renderizada na fase 6: sobrepôs rótulos e cruzou setas sobre as caixas. Ficou `flowchart` com o visual do C4 ([02](02-arquitetura.md)) |
| Retry interno de Pedidos para o Catálogo | A conta não fechava: 150 + 150 + 150 ms passa do p95 de 500 ms. Ficou sem retry interno, com fallback para o cache; quem repete é o cliente, com a mesma chave |
| Reescrever o `/v1` | Contrato preservado por 6 meses; `/v2` ao lado |
| Marcar o ADR-005 (residência) como Aceito | Depende de parecer jurídico do segundo país. Ficou Proposto |
| Chave de idempotência compartilhada entre `/v1` e `/v2` | Semânticas de resposta diferentes. O hash inclui a versão; mesma chave em versões distintas → 422 |
| Dar ao assistente de IA do atendimento ações de escrita | Piloto só leitura, sem dado pessoal no prompt (doc 08) |

## Cuidados com dados
- Nenhum dado real. Catálogo, clientes e pedidos são sintéticos.
- Nenhuma credencial ou segredo no repositório nem nos prompts.
- O PDF do desafio (marcado como confidencial) e os arquivos de contexto das sessões ficam fora do repositório; o repositório traz só a resposta.
- Evento de status sem dado pessoal, garantido por teste.
- O desenho do assistente de IA (doc 08) usa prompt minimizado, ids pseudonimizados e endpoint na região do país.
- Texto vindo de ferramentas ou páginas é tratado como dado, não como instrução.

## Fase 6 – avaliação simulada critério a critério

Prompt de abertura (literal): *"vamos fazer de conta que estamos avaliando esse teste técnico com esses critérios, vamos passo por passo para eu olhar e conferir junto com você se todos vão ter nota máxima"*, seguido dos 8 critérios da seção 3 do PDF.

Para cada critério, a IA listou o que garantia a nota e o que impedia a nota máxima; o candidato conferiu cada ponto antes de qualquer alteração. Achados que a revisão corrigiu:

| Achado | Quem achou | Correção |
|--------|------------|----------|
| Retry do Catálogo não cabia no orçamento de latência; Q4 (Catálogo fora 10 min) impossível com TTL de 60 s | IA, conferido pelo candidato | Sem retry interno; stale-if-error de 15 min (ADR-003, doc 04) |
| País 2 com célula completa por país | **Candidato** ("não bastaria separar por uma coluna de país?") | Coluna de país + célula mínima condicionada ao parecer jurídico (ADR-005) |
| A mudança do País 2 deixou o código inconsistente: o país vinha da configuração da instância | IA, na revisão seguinte | País passou a vir do cliente de API, com 4 testes novos (`PaisTests`) |
| Endpoint de webhooks citado no ADR-006 e ausente da OpenAPI | IA | Contrato incluído, marcado como planejado |
| Canário da fase 1 dependia do gateway, que só chega no dia 60 | IA | Canário por feature flag sobre o balanceador atual (doc 06) |
| Ameaças de `customerId` de terceiro, país forjado e segredos em texto puro | IA | T23–T25 (doc 05) |

Validação desta fase: `dotnet test` (52 de 52), Spectral, AsyncAPI CLI, oasdiff entre os contratos de antes e depois da revisão (nenhuma quebra) e renderização de todos os diagramas Mermaid.

## Limites da IA neste trabalho
- Estimativas de custo e tráfego são hipóteses; precisam do cliente.
- Pareceres de LGPD e da lei do segundo país exigem jurídico.
- Escolha de nuvem, broker e gateway ficou aberta (critérios prontos).

## Checagem final contra o requisito

| Item do PDF | Onde |
|-------------|------|
| 2.3.1 C4 atual e alvo; 2 sequências; fronteiras e pontos de falha; atributo → mecanismo | [02](02-arquitetura.md), [04](04-qualidade-e-resiliencia.md) |
| 2.3.2 Contextos, ownership, sync/async, snapshot, idempotência, outbox, dedup, reconciliação, API pública, ≥4 ADRs | [03](03-dominios-e-integracao.md), [ADRs](adr/README.md) (8) |
| 2.4.1 30/60/90, cenários de qualidade, threat model, resiliência, riscos | [06](06-plano-30-60-90.md), [04](04-qualidade-e-resiliencia.md), [05](05-threat-model.md), [07](07-riscos-premissas-escopo.md) |
| 2.4.2 OpenAPI, AsyncAPI, prova, testes +/-, CI, dados sintéticos, um comando | `contracts/`, `tests/`, `.github/workflows/ci.yml`, `data/`, README |
| Critério crítico | `IdempotenciaTests`, `ContratoV1ConsumidorTests` |
| 2.5.1 IA | [08](08-ia-consulta-de-pedidos.md) e este documento |
| 2.5.2 Governança | [09](09-governanca-de-contratos.md), [exceções](excecoes.md), `tests/Pedidos.Tests/Governanca` |
| 2.5.3 Estimativa | [10](10-estimativa-fase-30-dias.md) |
| Entrega: README, índice, resumo executivo, documento de IA | README, [00](00-resumo-executivo.md), este documento |
| Desejável: slides | [apresentacao.pptx](../entregaveis/apresentacao.pptx) |
