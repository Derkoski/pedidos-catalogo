# 05 – Threat model

> **Para o avaliador**  
> **Item do PDF:** 2.4.1 Threat model  
> **Critério de avaliação:** Segurança e privacidade  
> **O que conferir aqui:** Ameaças T1–T25 agrupadas por área (identidade, API pública, dados, eventos, dependências, IA), com risco, controle e prazo, indicando o que já está provado na fatia; LGPD por tema (base legal, papéis, direitos do titular, retenção, transferência internacional, incidente) no fim.

Método: STRIDE simplificado sobre as fronteiras de confiança de [02](02-arquitetura.md#5-fronteiras-de-confiança). Foco nos riscos com maior impacto. Escala: A (alto), M (médio), B (baixo).

## Ativos
- Dados pessoais de clientes (BR e País 2).
- Pedidos e valores (integridade financeira).
- Credenciais de clientes de API e segredos de webhook.
- Disponibilidade da criação de pedido.
- Respostas do assistente de IA do atendimento (piloto).

## Ameaças e controles

| # | Área | Ameaça (STRIDE) | Risco | Controle | Status |
|---|------|-----------------|-------|----------|--------|
| T1 | Identidade | Roubo/vazamento de `client_secret` de parceiro (S) | A | Token curto (15 min), rotação com dois segredos, escopo mínimo, alerta de uso anômalo por `client_id` | 60 dias |
| T2 | Identidade | Token de usuário reutilizado após logout (S) | M | Validade curta, refresh token com rotação | Existente no IdP (premissa) |
| T3 | API pública | Parceiro lê pedido de outro (I, BOLA) | A | Autorização por recurso em Pedidos (`client_id` do pedido); `404` para recurso alheio | **Provado na fatia** (`Cliente_de_api_nao_le_pedido_de_outro_cliente`); produção em 60 dias |
| T4 | API pública | Abuso de volume / DoS (D) | A | WAF, quota por cliente, rate limit de criação, load shedding | 60 dias |
| T5 | API pública | Replay de criação para duplicar pedido (T) | A | `Idempotency-Key` com escopo por cliente | 30 dias |
| T6 | API pública | Manipulação de preço no payload (T) | A | Preço nunca vem do cliente; vem do Catálogo e é gravado em snapshot | 30 dias |
| T7 | API pública | Injeção / payload malformado (T) | M | Validação por schema OpenAPI; limites de tamanho (máx. 100 itens, 64 KB) | 30 dias |
| T8 | Dados | Dado pessoal fora do país (I, residência) | A | Coluna `country` em todo pedido; célula mínima na região se a lei exigir localização (ADR-005); eventos e logs só com IDs; mascaramento no coletor | 90 dias |
| T9 | Dados | Vazamento por log/trace (I) | M | Lista de campos proibidos; teste no CI que falha se log contém CPF/e-mail (a criar na fase de 30 dias); o mesmo princípio já é checado nos eventos (`Eventos_nao_carregam_dado_pessoal`) | 30 dias |
| T10 | Dados | Acesso indevido de operador ao banco (I, E) | M | Acesso via perfil nominal, auditado; criptografia em repouso; mínimo privilégio | Existente (premissa) |
| T11 | Dados | Pedido de exclusão LGPD não atendido (conformidade) | M | Pseudonimização dos dados pessoais do pedido mantendo o registro fiscal | 90 dias |
| T12 | Eventos | Evento forjado no broker (S, T) | M | Broker em rede privada, credencial por serviço, ACL por tópico | 60 dias |
| T13 | Eventos | Webhook falso enviado ao parceiro em nosso nome (S) | M | Assinatura HMAC com timestamp; janela de 5 min contra replay | 60 dias |
| T14 | Eventos | Duplicata ou fora de ordem gera estado errado (T) | M | `eventId` para dedup; `version` para ordem | 60 dias |
| T15 | Dependências | SSRF via URL de webhook cadastrada (E) | A | Só HTTPS, bloqueio de IP privado/link-local, resolução DNS checada no envio, egress controlado | 60 dias |
| T16 | Dependências | Catálogo lento derruba Pedidos (D) | A | Timeout, circuit breaker, cache, bulkhead | 60 dias |
| T17 | Dependências | IdP indisponível (D) | M | Cache de JWKS no gateway; tokens válidos continuam aceitos | 60 dias |
| T18 | Dependências | Biblioteca com vulnerabilidade (E) | M | Scan de dependências e imagem no CI; SBOM | 30 dias |
| T19 | Auditoria | Negação de alteração de status (R) | M | Histórico de status com autor (`client_id`/serviço), data e `traceId` | 30 dias |
| T23 | Identidade | Cliente do app cria ou consulta pedido com o `customerId` de outra pessoa (S, I, BOLA) | A | Com token de usuário, o `customerId` vem da claim `sub`, nunca do corpo; corpo divergente → `403`. O campo do corpo só vale para parceiro (`client_credentials`), que responde pelo próprio cliente | 60 dias (com o gateway) |
| T24 | Identidade / Dados | Cliente forja o país para gravar dado pessoal em outro país ou usar outro preço (T, I) | A | País vem da claim `country` do `client_id`; gateway descarta header de país enviado pelo cliente; célula recusa país que não atende (`403`, `PaisTests`) | **Provado na fatia** (recusa); claim em 60 dias |
| T25 | Dados | Vazamento de segredo HMAC de webhook ou `client_secret` guardado no banco (I) | A | Segredos em cofre gerenciado (KMS/secret manager), nunca em texto puro no banco nem em log; rotação com dois segredos ativos | 60 dias |
| T20 | IA (assistente) | Prompt injection em texto de pedido, parceiro ou runbook leva o assistente a agir fora do papel (T, E) | M | Conteúdo de ferramenta tratado como dado, nunca como instrução; lista fechada de ferramentas só de leitura; autorização feita na API, nunca pelo modelo | Piloto (após 90 dias) |
| T21 | IA (assistente) | Vazamento de dado pessoal ou de pedido de outro cliente na resposta (I) | A | Token do atendente (on-behalf-of); minimização e ids pseudonimizados antes do prompt; casos de avaliação de vazamento com meta zero ([docs/ia](ia/casos-de-avaliacao.jsonl)) | Piloto (após 90 dias) |
| T22 | IA (assistente) | Resposta inventada (status ou preço) repassada ao cliente (T) | M | Resposta exige fonte citada; sem fonte, responde "não sei"; avaliação offline a cada mudança de prompt ou modelo | Piloto (após 90 dias) |

## LGPD – pontos específicos

| Tema | Como é atendido |
|------|-----------------|
| Base legal | Execução de contrato (art. 7º, V) para o pedido; obrigação legal (art. 7º, II) para a guarda fiscal |
| Minimização | Único dado pessoal de Pedidos: contato e endereço de entrega do pedido (`order_contact`). Eventos, webhooks, logs, traces e prompt do assistente só com IDs |
| Papéis | Web e app: a empresa é controladora. Pedido de parceiro: o parceiro é controlador do dado do cliente dele e a plataforma é operadora, com cláusula de operador no contrato ([03](03-dominios-e-integracao.md#1-bounded-contexts)) |
| Direitos do titular (art. 18) | Acesso, correção e exclusão atendidos pelo canal do encarregado, com prazo de resposta de até 15 dias (art. 19). Exclusão = pseudonimizar `order_contact` e manter o registro fiscal do pedido |
| Retenção | Pedido pelo prazo fiscal; `order_contact` pseudonimizado ao fim do prazo ou a pedido do titular. Chave de idempotência: 24 h, e a resposta guardada nela nunca inclui contato ou endereço |
| Segurança (art. 46) | TLS na borda, mTLS interno, criptografia em repouso, segredos em cofre, acesso nominal e auditado, autorização por recurso (T3, T23) |
| Transferência internacional (art. 33) | Opção A do ADR-005 (banco único): transferência dos dados do País 2 com salvaguardas contratuais e registro; opção B: dados ficam no país |
| Incidente | Plano de resposta com comunicação à ANPD e aos titulares em até 3 dias úteis quando houver risco relevante (Resolução CD/ANPD nº 15/2024) |
| Governança | Registro de operações de tratamento (art. 37) e relatório de impacto (RIPD) atualizados para os novos fluxos: app, parceiros, País 2 e assistente de IA. Encarregado (DPO) definido pelo cliente (premissa) |
