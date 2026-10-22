# 09 – Governança: evolução de contratos, fitness functions e exceções

> **Para o avaliador**  
> **Item do PDF:** 2.5.2 Segurança e governança (diferencial)  
> **Critério de avaliação:** Qualidade da prova · Decisões e trade-offs  
> **O que conferir aqui:** Política de evolução de contratos (§1); cada fitness function e onde ela roda no CI (§2); processo de exceção com prazo de expiração checado no CI (§3).

## 1. Política de evolução de contratos

Vale para OpenAPI (`/v1`, `/v2`) e AsyncAPI (eventos e webhooks).

| Mudança | Permitida na mesma versão? |
|---------|----------------------------|
| Novo endpoint, novo campo opcional na requisição, novo campo na resposta | Sim |
| Novo valor em enum de resposta | Sim, se documentado que consumidores devem tolerar valores desconhecidos (vale para `status` no v2 e no evento) |
| Novo tipo de evento | Sim |
| Remover ou renomear campo, mudar tipo, tornar campo obrigatório, mudar semântica, novo valor em enum do v1 | **Não**: nova versão major |

Regras:
- Versão major na URL (`/vN`) e no evento (`order.status-changed` com campo `schemaVersion`).
- Duas versões major ativas no máximo.
- Depreciação: headers `Deprecation` e `Sunset`, aviso por e-mail aos consumidores registrados, prazo mínimo de 6 meses.
- Desligamento só com uso zero (ou consumidores remanescentes cientes) por 2 semanas.
- Cada versão de contrato tem dono (time de Pedidos) e changelog.

## 2. Fitness functions no CI

| Verificação | Ferramenta sugerida | Falha o build quando |
|-------------|---------------------|----------------------|
| Lint OpenAPI | Spectral | Erro de schema, operação sem `operationId`, erro sem `problem+json`, POST de criação no v2 sem `Idempotency-Key` obrigatório |
| Lint AsyncAPI | AsyncAPI CLI (`validate`) | Schema inválido, mensagem sem `eventId` ou `schemaVersion` |
| Quebra de contrato | oasdiff (`breaking`) contra a versão publicada | Mudança que quebra dentro da mesma versão major |
| Teste de contrato do consumidor v1 | Teste xUnit com fixture do consumidor atual | Resposta do v1 diferente da fixture |
| ADRs | Script | ADR sem `Status:` ou sem as seções obrigatórias; ADR não listado no índice |
| Dependências de camadas | Teste de arquitetura (NetArchTest) | Domínio referencia infraestrutura ou ASP.NET; v1/v2 acessam banco direto sem passar pelo caso de uso |
| Dados pessoais em evento | Teste sobre o schema AsyncAPI | Campo com nome da lista proibida (`name`, `document`, `email`, `phone`, `address`) |

## 3. Processo leve de exceção técnica

Quando alguém precisa violar uma regra acima (ex.: mudança que quebra sem nova versão por correção de segurança).

1. Abrir issue com o modelo "Exceção técnica": regra violada, motivo, impacto em consumidores, alternativa considerada, prazo de expiração (máx. 90 dias), plano de correção.
2. Aprovação de 2 pessoas: dono do contrato + arquiteto. Segurança aprova se o motivo for vulnerabilidade.
3. No CI, a regra é suspensa só para o item afetado, com referência à issue (ex.: `# exception: #123` na regra do Spectral, ou lista de exceções em `docs/excecoes.md`).
4. A exceção expira sozinha: o CI falha depois da data de expiração.
5. Revisão mensal das exceções abertas.

Mudança que quebra por segurança: aviso aos consumidores no mesmo dia, mesmo com exceção aprovada.
