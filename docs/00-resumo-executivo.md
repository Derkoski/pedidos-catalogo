# Resumo executivo – Evolução da plataforma de Pedidos e Catálogo

> **Para o avaliador**  
> **Item do PDF:** Seção 4 do PDF: resumo executivo de até 2 páginas  
> **Critério de avaliação:** Atuação sênior  
> **O que conferir aqui:** Recomendação, investimento, riscos e decisões que o cliente precisa validar, nessa ordem.

## Situação
A plataforma atende só o web nacional. Em 90 dias precisa atender app, parceiros de marketplace e um segundo país, com 10x de volume, 99,9 % de disponibilidade e p95 de 500 ms na criação de pedido. Hoje ela tem quatro falhas que pioram com escala: pedido duplicado em retry, evento publicado fora da transação, N+1 síncrono no Catálogo e ausência de snapshot de preço. Não há API versionada para parceiros nem separação de dados por país.

## Recomendação
Evoluir o sistema atual em três ondas, sem reescrita e sem janela de indisponibilidade:

1. **30 dias – parar de errar.** Criação de pedido idempotente (`Idempotency-Key`), snapshot de preço e descrição, evento gravado na mesma transação (outbox). Contrato atual preservado; `/v2` preparado. Observabilidade e SLOs ativos.
2. **60 dias – escalar e abrir.** Catálogo consultado em lote com cache e circuit breaker; broker alimentado pela outbox; gateway com OAuth2 e quotas; app em produção no `/v2`; webhooks com parceiros piloto.
3. **90 dias – expandir.** Segundo país: todo pedido já grava o país; se a lei exigir que dados pessoais fiquem no país, sobe só uma célula mínima (API de Pedidos + banco na região); parceiros em produção; anúncio de depreciação do `/v1` com prazo mínimo de 6 meses.

A decisão mais crítica (idempotência + contrato versionado + outbox) está provada na fatia executável do repositório: chamadas repetidas com a mesma chave não duplicam pedido e o consumidor atual do `/v1` continua funcionando, com testes automáticos no pipeline.

## Principais decisões (ADRs)

| Decisão | Por quê |
|---------|---------|
| Idempotency-Key persistida com o pedido | Retry seguro; base para todos os mecanismos de resiliência |
| Outbox transacional | Evento existe se e só se o pedido foi gravado |
| Consulta em lote + snapshot | Latência estável e auditoria de preço |
| Versão na URL, v1 e v2 lado a lado | Nenhum consumidor atual quebra |
| Residência: coluna de país; célula mínima só se a lei exigir | Não gasta antes de a regra ser conhecida; broker e notificador não são duplicados |
| Webhooks assinados | Notificação a parceiros sem expor infraestrutura |
| OAuth2 + quotas no gateway | Um parceiro não derruba os outros |

## Investimento

| Fase | Sem IA | Com IA no desenvolvimento | Infraestrutura |
|------|-------:|--------------------------:|----------------|
| 30 dias | ~116 pessoa-dia · ≈ R$ 93,2 mil | ~93 pessoa-dia · ≈ R$ 79,2 mil (−15 %) | A cotar após medir o tráfego |
| 60 e 90 dias | ~275 pessoa-dia · ≈ R$ 214,6 mil | ~218 pessoa-dia · ≈ R$ 178,4 mil (−17 %) | A cotar após definir país e provedor |

Custo = salário CLT × 1,66 (encargos e provisões), sem benefícios; o cenário com IA inclui as licenças. O ganho com IA (~20 % de esforço) vem de estudos publicados e será medido nas semanas 1 e 2; o prazo não muda, porque é ditado pelo rollout seguro. O preço da proposta acrescenta tributos e margem, definidos pela área comercial.

Detalhe em [10-estimativa-fase-30-dias.md](10-estimativa-fase-30-dias.md).

## Riscos principais
- **Tráfego real desconhecido.** Dimensionamento por premissa (20 → 200 criações/s). Medir na semana 1.
- **Regra de residência do segundo país não definida.** Decide entre coluna de país (custo zero) e célula mínima na região. Parecer até o dia 30.
- **Consumidores atuais de eventos não mapeados.** Pode alongar o período de dupla publicação.
- **Adoção da chave pelo web.** Sem ela, o `/v1` ainda pode duplicar.

## Decisões que precisam de validação do cliente
1. Baseline de tráfego e contrato v1 real.
2. País, regra de residência e parecer jurídico (LGPD e lei local).
3. Preço vale no momento do pedido; defasagem de até 60 s no cache é aceitável; com o Catálogo fora, vender pelo último preço conhecido por até 15 min (ou recusar o pedido).
4. Retenção da chave de idempotência em 24 h.
5. Provedor de nuvem, broker e gateway (critérios prontos; escolha na semana 1).
6. Salário de SRE/DevOps (premissa), benefícios, tributos e margem da proposta.
7. Piloto do assistente de IA para o atendimento após os 90 dias.

## Índice
Diagnóstico [01](01-diagnostico.md) · Arquitetura [02](02-arquitetura.md) · Domínios e integração [03](03-dominios-e-integracao.md) · Qualidade e resiliência [04](04-qualidade-e-resiliencia.md) · Threat model [05](05-threat-model.md) · Plano 30/60/90 [06](06-plano-30-60-90.md) · Riscos e premissas [07](07-riscos-premissas-escopo.md) · IA [08](08-ia-consulta-de-pedidos.md) · Governança [09](09-governanca-de-contratos.md) · Estimativa [10](10-estimativa-fase-30-dias.md) · [ADRs](adr/README.md) · Conferência item a item do PDF: [rastreabilidade](rastreabilidade.md)
