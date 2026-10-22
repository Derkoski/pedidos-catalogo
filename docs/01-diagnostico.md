# 01 – Diagnóstico do cenário atual

## Como está hoje
- Um canal (web nacional) consumindo a API de Pedidos.
- Pedidos chama o Catálogo de forma síncrona, uma chamada por item.
- Pedido não guarda preço nem descrição do momento da compra.
- Cliente que dá retry pode gerar pedido duplicado (não existe idempotency key).
- Evento de pedido é publicado fora da transação do banco: pode salvar e não publicar, ou publicar e não salvar.
- Não existe API pública para parceiros nem notificação assíncrona de status.

## Problemas e impacto

| # | Problema | Impacto com 10x de tráfego | Risco |
|---|----------|---------------------------|-------|
| P1 | Retry sem idempotency key | Pedido duplicado, cobrança em dobro, retrabalho no atendimento. Mais tráfego = mais timeout = mais retry | Alto |
| P2 | Evento fora da transação | Status divergente entre sistemas; parceiros recebendo evento de pedido que não existe | Alto |
| P3 | N+1 síncrono no Catálogo | p95 da criação cresce com o número de itens; Catálogo fora do ar derruba Pedidos (efeito cascata) | Alto |
| P4 | Sem snapshot de preço/descrição | Sem auditoria; pedido "muda" quando o Catálogo muda | Médio |
| P5 | Sem versionamento de API | Qualquer mudança para app/parceiros arrisca quebrar o web atual | Alto |
| P6 | Dados pessoais sem segregação por país | Bloqueia a entrada no 2º país (residência de dados) | Alto, mas com prazo de 90 dias |

## Decisão crítica a validar na fatia

**Criação de pedido idempotente com contrato versionado (v1 preservado, v2 novo), gravando snapshot e outbox na mesma transação.**

Por que essa e não outra:
- É o que o critério crítico do teste cobra: mesma chave não duplica pedido, e a evolução de contrato não quebra o consumidor atual.
- Ataca P1, P2, P4 e P5 com mudança pequena, dentro do serviço de Pedidos, sem depender de infraestrutura nova. Cabe nos 30 dias.
- É pré-requisito para o resto: sem idempotência, qualquer retry/circuit breaker que colocarmos depois aumenta duplicidade; sem v2, não dá para abrir para app e parceiros.

Alternativas consideradas para a primeira entrega:

| Alternativa | Por que não agora |
|-------------|-------------------|
| Separar Catálogo de Pedidos em serviços/bancos distintos | Projeto grande, não entrega valor em 30 dias e não resolve duplicidade |
| Strangler Fig com gateway | Entra no plano (é como o v2 convive com o v1), mas sozinho não prova nada sobre consistência |
| Cache/leitura em lote do Catálogo | Resolve P3, é a segunda entrega (60 dias). Melhora latência, mas não é o risco de negócio mais caro |
| Outbox com relay e broker | A gravação na outbox entra na fatia; o relay/broker fica para o plano, porque depende de escolha de infraestrutura |

## Escopo da fatia (Fase 3)
Entra:
- `POST /v1/orders` igual ao contrato atual: aceita sem `Idempotency-Key` (comportamento antigo) e passa a deduplicar quando a chave vem.
- `POST /v2/orders` com `Idempotency-Key` obrigatório; `GET /v1/orders/{id}` e `GET /v2/orders/{id}`.
- Mesma chave + mesmo payload → devolve o pedido original. Mesma chave + payload diferente → 422.
- Snapshot de preço/descrição nos itens; registro na tabela de outbox na mesma transação.
- Testes: idempotência (positivo e negativo), consumidor v1 continua funcionando, outbox gravada junto com o pedido.

Fica de fora: relay da outbox, broker, gateway, autenticação real, Catálogo real (stub com dados sintéticos).

## Lacunas do enunciado (viram premissas)
O PDF não define estes pontos. Vou assumir e registrar como premissa; precisam de validação com o cliente:

1. **Não há código nem contrato atual.** O "contrato v1" será definido por nós como representação do atual.
2. **Volume absoluto não informado.** Só "10x". Sem baseline de pedidos/s, o dimensionamento é por ordem de grandeza.
3. **Stack, banco e broker atuais desconhecidos.** Também não há cloud definida.
4. **Segundo país não identificado.** A regra de residência de dados é tratada de forma genérica (dados pessoais na região do país).
5. **Latência alvo só para criação.** Consulta e notificação de status não têm meta; vamos propor.
6. **Retenção da chave de idempotência não definida.** Proposta: 24 h.
7. **Parceiros:** quantidade, modelo de autenticação atual e quotas não informados.
8. **Orçamento e time disponíveis não informados**, o que afeta a estimativa (diferencial 2.5.3).
9. **Política de preço:** não diz se o pedido usa o preço do momento do pedido ou do pagamento. Assumimos momento do pedido.
