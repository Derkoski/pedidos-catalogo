# 07 – Riscos, premissas, débitos aceitos e fora de escopo

> **Para o avaliador**  
> **Item do PDF:** 2.4.1 Riscos, premissas, débitos aceitos e fora de escopo  
> **Critério de avaliação:** Atuação sênior · Estratégia de evolução  
> **O que conferir aqui:** Riscos com gatilho de acompanhamento; premissas a validar com o cliente; débitos aceitos com data para pagar; o que ficou fora e por quê.

## Riscos

| # | Risco | Prob. | Impacto | Mitigação | Gatilho |
|---|-------|-------|---------|-----------|---------|
| R1 | Baseline real de tráfego muito acima da premissa | M | A | Medir na semana 1; teste de carga antes de cada fase | p95 > 400 ms em homologação |
| R2 | Regra de residência do País 2 mais restrita (ex.: `orderId`, eventos ou logs também locais) | M | A | Validação jurídica até o dia 30; broker e notificador podem ir para a célula (alternativa 3 do ADR-005) sem mudar código | Parecer jurídico |
| R3 | Web não consegue enviar `Idempotency-Key` no prazo | B | M | v1 continua funcionando sem a chave; duplicidade cai conforme adoção | % de criações com chave < 80 % no dia 45 |
| R4 | Consumidores internos atrasam migração para a outbox | M | M | Dual write temporário; lista de consumidores e dono de cada um | Consumidor sem dono no dia 45 |
| R5 | Parceiros não implementam dedup/assinatura | M | M | Guia, SDK de exemplo, ambiente sandbox | Erros de assinatura em beta |
| R6 | Cache de 60 s gera divergência de preço em promoção | B | M | TTL configurável por produto; invalidação por evento se necessário | Reclamação do negócio |
| R7 | Time sem experiência com outbox/broker | M | M | Pareamento; prova na fatia executável | – |
| R8 | Custo da célula do País 2 acima do previsto | B | M | Célula mínima (sem broker e notificador próprios); só existe se a lei exigir; autoescala | Parecer exige localização |

## Premissas (validar com o cliente)

1. Contrato v1 atual é o descrito em [03](03-dominios-e-integracao.md#6-api-pública); não há código nem contrato atual fornecido.
2. Carga: 20 criações/s no pico hoje; 200/s no alvo.
3. Banco atual é relacional com transação ACID.
4. Existe provedor de identidade corporativo (OIDC) que pode emitir tokens `client_credentials`.
5. Preço vale no momento da criação do pedido.
6. Retenção da chave de idempotência: 24 h.
7. Segundo país tem região de nuvem disponível do mesmo provedor.
8. Catálogo não tem dado pessoal e pode ser compartilhado entre países.
9. Evento com `orderId` opaco e status não é dado pessoal sujeito à residência (validar no parecer).
10. Pagamento e Logística conseguem enviar eventos com ID único.
11. Time e orçamento conforme [10](10-estimativa-fase-30-dias.md).

## Débitos aceitos

| Débito | Por que aceitar | Quando pagar |
|--------|-----------------|--------------|
| Relay por polling em vez de CDC | Simples e independente de banco | Se a outbox virar gargalo |
| Dual write temporário (outbox + publicação antiga) | Permite migrar consumidores sem big bang | Até o dia ~75 |
| `/v1` sem chave pode duplicar | Compatibilidade de 6 meses | Sunset do `/v1` |
| Pedidos antigos sem snapshot | Não há dado confiável para preencher | Não pagar; leitura trata `null` |
| Catálogo, broker e notificador compartilhados entre países | Sem dado pessoal; reduz custo | Se a regra do País 2 exigir |
| Fatia executável com SQLite e Catálogo stub | Prova a decisão sem depender de infraestrutura | Não vai para produção |

## Fora do escopo
- Reescrever Catálogo ou separar seu banco.
- Trocar tecnologia de banco ou migrar para banco distribuído.
- Multi-região ativa-ativa dentro do mesmo país.
- Pagamento, logística, antifraude e faturamento.
- Busca de produtos e recomendação.
- Escolha final de provedor de nuvem, broker e gateway (critérios definidos; produto escolhido na semana 1).
- Assistente de IA em produção (piloto proposto em [08](08-ia-consulta-de-pedidos.md), após 90 dias).
