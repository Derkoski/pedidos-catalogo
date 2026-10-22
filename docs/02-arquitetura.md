# 02 – Arquitetura atual e alvo

> **Para o avaliador**  
> **Item do PDF:** 2.3.1 Visões arquiteturais  
> **Critério de avaliação:** Arquitetura de solução  
> **O que conferir aqui:** C4 atual (§1–§2) e alvo (§3–§4); fronteiras de confiança (§5); pontos de falha (§2 e §6); atributo de qualidade → mecanismo (§7); sequências de criação (§8) e de status (§9).

Notação: modelo C4, níveis 1 (Contexto) e 2 (Contêineres), em Mermaid. Usei `flowchart` com o visual oficial do C4 (cores, estereótipo entre colchetes, fronteiras tracejadas). Testei a sintaxe nativa `C4Context`/`C4Container`: ela é marcada como experimental no Mermaid e, com o layout em grade fixa, sobrepôs rótulos e cruzou setas sobre as caixas no diagrama alvo. O `flowchart` roteia as setas e continua versionável e renderizado pelo GitHub.

| Visual | Elemento C4 |
|--------|-------------|
| Azul-escuro, arredondado | Pessoa |
| Azul | Sistema em foco (contexto) ou contêiner |
| Cilindro | Contêiner de dados (banco, cache) |
| Caixa com barras laterais | Broker / fila |
| Cinza | Sistema externo |
| Moldura tracejada | Fronteira de confiança ou célula |
| Rótulo da seta | Interação e, entre colchetes, o protocolo |

## 1. Contexto – atual

```mermaid
flowchart LR
  cli(["<b>Cliente web nacional</b><br/><i>[Pessoa]</i><br/>Compra pelo site"])
  plat["<b>Plataforma Pedidos e Catálogo</b><br/><i>[Sistema]</i><br/>Canal web, API de Pedidos e API de Catálogo"]
  pag["<b>Pagamento</b><br/><i>[Sistema externo]</i>"]
  int["<b>Sistemas internos</b><br/><i>[Sistema externo]</i><br/>Consumidores de eventos"]
  cli -->|"Navega e cria pedidos<br/><i>[HTTPS]</i>"| plat
  plat -->|"Solicita pagamento"| pag
  plat -->|"Publica eventos de pedido"| int
  classDef person fill:#08427b,stroke:#052e56,color:#fff
  classDef system fill:#1168bd,stroke:#0b4884,color:#fff
  classDef container fill:#438dd5,stroke:#2e6295,color:#fff
  classDef ext fill:#999999,stroke:#6b6b6b,color:#fff
  classDef boundary fill:none,stroke:#444,stroke-dasharray:6 4,color:#444
  class cli person
  class plat system
  class pag,int ext
```

## 2. Contêineres – atual

```mermaid
flowchart LR
  cli(["<b>Cliente web nacional</b><br/><i>[Pessoa]</i>"])
  subgraph plat["Plataforma Pedidos e Catálogo [Sistema]"]
    web["<b>Canal web</b><br/><i>[Contêiner: web]</i>"]
    ped["<b>API Pedidos</b><br/><i>[Contêiner: HTTP]</i><br/>Cria e consulta pedidos"]
    cat["<b>API Catálogo</b><br/><i>[Contêiner: HTTP]</i><br/>Produto, descrição, preço"]
    dbp[("<b>Banco Pedidos</b><br/><i>[Contêiner: relacional]</i><br/>sem snapshot")]
    dbc[("<b>Banco Catálogo</b><br/><i>[Contêiner: relacional]</i>")]
    fila[["<b>Fila/broker atual</b><br/><i>[Contêiner: fila]</i>"]]
  end
  int["<b>Consumidores internos</b><br/><i>[Sistema externo]</i>"]
  cli -->|"HTTPS"| web
  web -->|"Cria/consulta pedido<br/><i>[HTTPS]</i>"| ped
  ped -->|"1 chamada por item (N+1)<br/><i>[HTTP síncrono]</i>"| cat
  ped --> dbp
  cat --> dbc
  ped -.->|"Publica fora da transação"| fila
  fila --> int
  classDef person fill:#08427b,stroke:#052e56,color:#fff
  classDef system fill:#1168bd,stroke:#0b4884,color:#fff
  classDef container fill:#438dd5,stroke:#2e6295,color:#fff
  classDef ext fill:#999999,stroke:#6b6b6b,color:#fff
  classDef boundary fill:none,stroke:#444,stroke-dasharray:6 4,color:#444
  class cli person
  class web,ped,cat,dbp,dbc,fila container
  class int ext
  class plat boundary
```

Pontos de falha atuais:

| Ponto | Falha | Efeito |
|-------|-------|--------|
| Pedidos → Catálogo | Lento ou fora | Criação de pedido lenta ou falha (cascata) |
| Retry do cliente | Timeout na resposta | Pedido duplicado |
| Commit + publish | Um ocorre, o outro não | Evento perdido ou evento de pedido inexistente |
| Banco único de Pedidos | Indisponível | Toda a plataforma para |

## 3. Contexto – alvo

```mermaid
flowchart LR
  cw(["<b>Cliente web</b><br/><i>[Pessoa]</i>"])
  ca(["<b>Cliente app móvel</b><br/><i>[Pessoa]</i>"])
  sup(["<b>Atendente</b><br/><i>[Pessoa]</i><br/>Usa o assistente de consulta"])
  pm["<b>Parceiro de marketplace</b><br/><i>[Sistema externo]</i>"]
  plat["<b>Plataforma Pedidos e Catálogo</b><br/><i>[Sistema]</i><br/>BR e País 2 · API v1 preservada · API v2 pública"]
  idp["<b>Provedor de identidade</b><br/><i>[Sistema externo]</i><br/>OIDC / OAuth2"]
  pag["<b>Pagamento</b><br/><i>[Sistema externo]</i>"]
  log["<b>Logística</b><br/><i>[Sistema externo]</i>"]
  cw -->|"Pedidos<br/><i>[HTTPS /v1 e /v2]</i>"| plat
  ca -->|"Pedidos<br/><i>[HTTPS /v2]</i>"| plat
  sup -->|"Consulta pedidos<br/><i>[SSO]</i>"| plat
  pm <-->|"API pública /v2 + OAuth2<br/>webhooks de status + HMAC"| plat
  plat -->|"Valida tokens"| idp
  plat <-->|"Pagamento e status"| pag
  plat <-->|"Entrega e status"| log
  classDef person fill:#08427b,stroke:#052e56,color:#fff
  classDef system fill:#1168bd,stroke:#0b4884,color:#fff
  classDef container fill:#438dd5,stroke:#2e6295,color:#fff
  classDef ext fill:#999999,stroke:#6b6b6b,color:#fff
  classDef boundary fill:none,stroke:#444,stroke-dasharray:6 4,color:#444
  class cw,ca,sup person
  class plat system
  class pm,idp,pag,log ext
```

## 4. Contêineres – alvo

```mermaid
flowchart TB
  subgraph inet["Internet – não confiável"]
    web(["<b>Cliente web</b><br/><i>[Pessoa]</i>"])
    app(["<b>Cliente app</b><br/><i>[Pessoa]</i>"])
    parc["<b>Parceiro</b><br/><i>[Sistema externo]</i>"]
  end
  subgraph borda["Borda"]
    waf["<b>WAF + API Gateway</b><br/><i>[Contêiner: gerenciado]</i><br/>TLS, authN, quotas,<br/>roteia v1/v2 e por país"]
  end
  subgraph br["Célula BR – dados pessoais BR"]
    pedbr["<b>API Pedidos</b><br/><i>[Contêiner: stateless]</i><br/>Idempotência, snapshot, outbox"]
    cachebr[("<b>Cache do Catálogo</b><br/><i>[Contêiner: chave-valor]</i><br/>TTL 60 s")]
    dbbr[("<b>Banco Pedidos BR</b><br/><i>[Contêiner: relacional]</i><br/>pedido com country, snapshot,<br/>chave de idempotência, outbox")]
    relbr["<b>Relay + reconciliação</b><br/><i>[Contêiner: worker]</i>"]
  end
  subgraph p2["Célula País 2 – só se a lei exigir localização (ADR-005)"]
    ped2["<b>API Pedidos</b><br/><i>[Contêiner: mesmo código]</i>"]
    cache2[("<b>Cache do Catálogo</b><br/><i>[Contêiner]</i>")]
    db2[("<b>Banco Pedidos País 2</b><br/><i>[Contêiner: relacional]</i>")]
    rel2["<b>Relay + reconciliação</b><br/><i>[Contêiner: worker]</i>"]
  end
  subgraph glob["Compartilhado – sem dados pessoais"]
    cat["<b>API Catálogo</b><br/><i>[Contêiner: HTTP]</i><br/>leitura em lote, preço por país"]
    dbc[("<b>Banco Catálogo</b><br/><i>[Contêiner: relacional]</i>")]
    brk[["<b>Broker</b><br/><i>[Contêiner: fila]</i><br/>só IDs e status"]]
    wh["<b>Notificador de webhooks</b><br/><i>[Contêiner: worker]</i><br/>retry, DLQ, fila por parceiro"]
    obs["<b>Observabilidade</b><br/><i>[Contêiner: OpenTelemetry]</i><br/>sem dados pessoais"]
  end
  int["<b>Consumidores internos</b><br/><i>[Sistema externo]</i>"]

  web & app -->|"HTTPS"| waf
  parc -->|"HTTPS + OAuth2"| waf
  waf -->|"api-br · mTLS"| pedbr
  waf -->|"api-p2 · mTLS"| ped2
  pedbr -->|"transação única"| dbbr
  ped2 -->|"transação única"| db2
  pedbr -->|"preços"| cachebr
  ped2 -->|"preços"| cache2
  relbr -->|"lê outbox"| dbbr
  rel2 -->|"lê outbox"| db2
  cachebr & cache2 -->|"miss: lote, timeout 150 ms, CB"| cat
  cat --> dbc
  relbr & rel2 -->|"publica"| brk
  brk --> wh
  brk --> int
  wh -->|"webhook HTTPS + HMAC"| parc
  classDef person fill:#08427b,stroke:#052e56,color:#fff
  classDef system fill:#1168bd,stroke:#0b4884,color:#fff
  classDef container fill:#438dd5,stroke:#2e6295,color:#fff
  classDef ext fill:#999999,stroke:#6b6b6b,color:#fff
  classDef boundary fill:none,stroke:#444,stroke-dasharray:6 4,color:#444
  class web,app person
  class parc,int ext
  class waf,pedbr,cachebr,dbbr,relbr,ped2,cache2,db2,rel2,cat,dbc,brk,wh,obs container
  class inet,borda,br,p2,glob boundary
```

Segundo país ([ADR-005](adr/ADR-005-residencia-de-dados-por-celula.md)): se o parecer jurídico permitir transferência internacional (opção A), a célula País 2 não existe e os pedidos do País 2 ficam no banco BR com `country = <país>`. Se exigir localização (opção B), só sobe a célula mínima acima. Broker e notificador não são duplicados, porque os eventos não têm dado pessoal.

Componentes e responsabilidades:

| Contêiner | Onde roda | Responsabilidade | Dono dos dados |
|-----------|-----------|------------------|----------------|
| WAF + API Gateway | Compartilhado | TLS, WAF, autenticação, quotas, roteamento por versão e por país | – |
| API Pedidos | Por célula | Criar/consultar pedido, idempotência, snapshot, gravar outbox | Pedidos |
| Relay da outbox + reconciliação | Por célula (lê o banco local) | Publica a outbox no broker e marca como publicado; detecta outbox parada e pedidos sem evento | Pedidos |
| Cache do Catálogo | Por célula | Preço e descrição vigentes perto da API, sem atravessar regiões | Catálogo |
| Broker | Compartilhado | Distribui eventos de status (só IDs e status) | – |
| Notificador de webhooks | Compartilhado | Entrega eventos a parceiros com retry, DLQ e fila por parceiro | Notificações |
| API Catálogo | Compartilhado | Produto, descrição, preço vigente por país; leitura em lote | Catálogo |
| Observabilidade | Compartilhado | Traces, métricas, logs (sem dados pessoais) | Plataforma |

Dependências externas: provedor de identidade, pagamento, logística, endpoints dos parceiros (não confiáveis).

## 5. Fronteiras de confiança

| Fronteira | O que cruza | Controle |
|-----------|-------------|----------|
| Internet → Borda | Requisições de web, app, parceiros | TLS 1.2+, WAF, OAuth2/JWT, quotas |
| Borda → Célula | Requisição autenticada | mTLS interno, claims propagadas (cliente, escopos, país) |
| Célula → Compartilhado | IDs de produto e país (Catálogo); eventos com IDs e status (broker) | Nenhum dado pessoal sai da célula; schema do evento checado no CI (`Eventos_nao_carregam_dado_pessoal`) |
| Célula → Parceiro (webhook) | Evento de status | HMAC, só IDs e status, URL cadastrada e validada |
| Célula → Observabilidade | Telemetria | Mascaramento de dados pessoais no coletor |

## 6. Pontos de falha no alvo

| Ponto | Mitigação | Degradação |
|-------|-----------|------------|
| Catálogo lento/fora | Lote, cache na célula, timeout, circuit breaker | Usa o último preço em cache (até 15 min); sem cache → 503 com `Retry-After` (seguro por causa da idempotência) |
| Broker (compartilhado) fora | Outbox de cada célula acumula no banco | Eventos atrasam nos dois países; pedidos continuam sendo criados |
| Endpoint do parceiro fora | Retry com backoff, DLQ | Parceiro reconcilia via `GET /v2/orders/{id}` |
| Banco da célula | Réplica síncrona na mesma região, failover | Célula afetada indisponível; outra célula segue |
| Gateway | Múltiplas zonas | – |

## 7. Atributos de qualidade → mecanismos

| Atributo | Mecanismos |
|----------|-----------|
| Escala 10x | Leitura em lote + cache do Catálogo; Pedidos stateless com autoescala; cache do Catálogo por célula; publicação assíncrona |
| Disponibilidade 99,9% | Multi-zona; outbox desacopla broker; circuit breaker e bulkhead; deploy canário com rollback por feature flag |
| p95 500 ms na criação | 1 chamada em lote ao Catálogo; cache; orçamento de latência (ver [04](04-qualidade-e-resiliencia.md)) |
| Consistência | Idempotency-Key; snapshot + outbox na mesma transação; deduplicação por `eventId` no consumidor |
| Auditabilidade | Snapshot de preço/descrição; histórico de status; trilha de acesso |
| Segurança/LGPD | OAuth2 com escopos; coluna `country` e célula mínima por país se a lei exigir ([ADR-005](adr/ADR-005-residencia-de-dados-por-celula.md)); eventos sem dados pessoais; mascaramento em logs |
| Evolução sem quebra | `/v1` e `/v2` em paralelo; política de contratos; testes de contrato no CI |

## 8. Sequência – criação de pedido (v2)

```mermaid
sequenceDiagram
  autonumber
  participant C as Cliente (app/parceiro)
  participant G as API Gateway
  participant P as API Pedidos
  participant K as API Catálogo
  participant D as Banco Pedidos

  C->>G: POST /v2/orders + Idempotency-Key + token
  G->>G: valida token, escopo orders:write, quota
  G->>P: encaminha para a célula do país
  P->>D: busca chave (cliente + Idempotency-Key)
  alt chave existe, mesmo hash do payload
    D-->>P: pedido original
    P-->>C: 201 + mesmo corpo + Idempotent-Replayed: true
  else chave existe, payload diferente
    P-->>C: 422 chave reutilizada com outro payload
  else chave nova
    P->>P: preços no cache da célula (TTL 60 s)
    P->>K: GET /products?ids=... só os itens fora do cache (1 chamada, timeout 150 ms, sem retry)
    K-->>P: preço e descrição vigentes
    P->>D: BEGIN
    P->>D: INSERT pedido + itens (snapshot com catalogVersion)
    P->>D: INSERT chave de idempotência (UNIQUE) + resposta
    P->>D: INSERT outbox (order.created)
    P->>D: COMMIT
    alt COMMIT ok
      P-->>C: 201 Created + Location
    else UNIQUE violada (corrida com a mesma chave)
      P->>D: relê a chave
      P-->>C: resposta do vencedor (201 replay ou 422), ou 409 se ainda não visível
    end
  end
```

Duas requisições simultâneas com a mesma chave: a constraint `UNIQUE` faz uma delas falhar no commit, e nada dela é gravado. Essa requisição relê a chave e devolve o pedido original (ou `422`, se o payload for outro). Não existe um estado "em processamento" gravado: o `409` só aparece no caso raro em que a releitura ainda não enxerga o vencedor, e o cliente repete com a mesma chave. Se o Catálogo falhar ou estourar o timeout, a API usa o último preço em cache (até 15 min); item sem cache responde `503` + `Retry-After`, e o cliente repete com a mesma chave sem risco de duplicar.

## 9. Sequência – atualização e notificação de status

```mermaid
sequenceDiagram
  autonumber
  participant O as Origem (pagamento/logística)
  participant P as API Pedidos
  participant D as Banco Pedidos
  participant R as Relay da outbox
  participant B as Broker
  participant N as Notificador de webhooks
  participant X as Parceiro

  O->>P: evento de mudança de status (ID do evento de origem)
  P->>D: BEGIN, INSERT evento recebido (UNIQUE origem + ID)
  alt ID já recebido
    P->>D: ROLLBACK
    P-->>O: 200 (duplicado, ignorado)
  else transição inválida na máquina de status
    P->>D: ROLLBACK
    P-->>O: 409 + registro para análise
  else novo e válido
    P->>D: UPDATE status (versão otimista), INSERT histórico, INSERT outbox, COMMIT
    P-->>O: 200
  end
  loop a cada poucos segundos
    R->>D: SELECT outbox não publicada (lote, ordem)
    R->>B: publica order.status-changed (eventId)
    R->>D: marca publicada
  end
  B->>N: entrega (pelo menos uma vez)
  N->>N: dedup por eventId, filtra assinaturas
  N->>X: POST webhook (HMAC, timeout 5 s)
  alt 2xx
    X-->>N: ok
  else erro/timeout
    N->>N: retry exponencial até 24 h, depois DLQ
  end
  X->>P: GET /v2/orders/{id} (reconciliação, quando quiser)
```

Garantias: evento de entrada processado uma vez (deduplicação pelo ID da origem, na mesma transação da mudança); entrega pelo menos uma vez; consumidores deduplicam por `eventId`; ordem garantida por pedido (chave de partição = `orderId`); campo `version` no evento permite descartar evento antigo.
