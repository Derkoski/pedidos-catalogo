# ADR-007 – Autenticação, autorização e quotas da API pública

Status: Aceito
Data: 2026-10-21

## Contexto
A API passa a ter três tipos de consumidor: web, app e parceiros. Parceiros são sistemas (máquina a máquina). Com 10x de tráfego, um parceiro mal comportado pode derrubar os outros.

## Alternativas
1. **API key estática no header.** Simples, mas sem expiração, difícil de rotacionar e sem escopo. Rejeitada para parceiros.
2. **mTLS por parceiro.** Forte, mas operação de certificados pesada para parceiros pequenos. Opcional para parceiros grandes.
3. **OAuth2 `client_credentials` com JWT curto e escopos, validado no gateway.** Escolhida para parceiros. Web e app usam OIDC (token de usuário).

## Decisão
- Provedor de identidade emite JWT (validade 15 min) com `client_id`, escopos e país.
- Gateway valida assinatura, expiração, audiência e escopo; repassa claims para Pedidos.
- Pedidos aplica autorização por recurso: parceiro só lê pedidos com o seu `client_id`; usuário só lê os seus.
- Escopos: `orders:read`, `orders:write`, `webhooks:manage`.
- Quotas por `client_id` no gateway: taxa por segundo (token bucket) e cota diária, por plano. Excedeu: `429` + `Retry-After`.
- Rate limit separado e mais baixo para criação de pedido do que para leitura.
- Segredo do cliente rotacionável sem downtime (dois segredos ativos).
- O `/v1` não passa a exigir token: o consumidor atual chama sem `Authorization`, e exigir token quebraria o contrato. O gateway identifica essa rota como cliente `web`, com quota própria, até o Sunset do `/v1` (ADR-004).

## Consequências
**Técnicas**
- Padrão conhecido pelos parceiros, com bibliotecas prontas.
- Dependência do provedor de identidade. Mitigação: cache das chaves públicas (JWKS) no gateway; tokens curtos já emitidos continuam válidos se o IdP cair por pouco tempo.
- Precisa de processo de onboarding de parceiro (cadastro, plano, país).

**Operacionais**
- Onboarding de parceiro com dono definido: cadastro, plano de quota, país, segredo.
- Alertas de `401`, `403` e `429` por `client_id`; rotação periódica de segredos.
- Cache das chaves públicas (JWKS) no gateway, monitorado.

**Quando revisitar**
- Parceiro grande exigir mTLS (alternativa 2 como opção).
- Indisponibilidade do provedor de identidade maior que a validade do token (15 min).
