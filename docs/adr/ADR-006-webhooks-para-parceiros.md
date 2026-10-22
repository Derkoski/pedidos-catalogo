# ADR-006 – Webhooks assinados para notificar parceiros

Status: Aceito
Data: 2026-10-21

## Contexto
Parceiros pedem notificação assíncrona de mudança de status. Eles estão fora da nossa rede, têm disponibilidade própria e nível técnico variado.

## Alternativas
1. **Só polling na API.** Simples, mas gera carga e atraso. Fica como mecanismo de reconciliação, não de notificação.
2. **Acesso direto do parceiro ao broker.** Expõe infraestrutura interna, exige credencial no broker e acopla o parceiro à tecnologia. Rejeitada.
3. **Webhooks HTTPS assinados, alimentados pelo broker interno.** Padrão de mercado, só exige um endpoint HTTPS do parceiro. Escolhida.

## Decisão
- Parceiro cadastra URL HTTPS e eventos de interesse (`POST /v2/webhooks`, escopo `webhooks:manage`; contrato na OpenAPI v2, implementação no plano de 60 dias).
- Corpo: `eventId`, `type`, `schemaVersion`, `occurredAt`, `orderId`, `previousStatus`, `status`, `version`. Sem dado pessoal.
- Assinatura `HMAC-SHA256` do corpo + timestamp no header `X-Signature`; segredo por parceiro, rotacionável.
- Timeout 5 s. Retry exponencial com jitter por até 24 h. Depois, DLQ e alerta.
- Entrega pelo menos uma vez; ordem por pedido não garantida no parceiro, por isso o campo `version`.
- Bulkhead: fila e workers por parceiro, para um parceiro lento não atrasar os outros.
- Contrato descrito em AsyncAPI 3.0.

## Consequências
**Técnicas**
- Parceiro precisa deduplicar por `eventId` e validar assinatura. Documentado com exemplo.
- Validação da URL cadastrada para evitar SSRF (só HTTPS, sem IP privado, resolução checada).
- Mais um componente para operar (notificador). Simples e sem estado além das tentativas.

**Operacionais**
- Painel por parceiro: entregas, taxa de erro, tentativas, tamanho da DLQ. Alerta quando a DLQ de um parceiro passa de zero.
- Runbook: reenvio da DLQ depois que o parceiro corrigir o endpoint.
- Rotação de segredo sem downtime (dois segredos ativos) e sandbox para o parceiro testar assinatura e deduplicação.

**Quando revisitar**
- Parceiro com volume muito alto ou que precise de reprocessamento em massa: avaliar entrega em lote ou acesso gerenciado a um tópico.
