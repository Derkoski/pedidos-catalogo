# ADR-005 – Residência de dados do segundo país: coluna de país ou célula mínima

Status: Proposto (escolha entre A e B depende do parecer jurídico do País 2 até o dia 30)
Data: 2026-10-22

## Contexto
Em 90 dias a plataforma opera em um segundo país. O PDF exige atender LGPD e os requisitos de residência de dados do novo país, mas não informa o país nem a regra. Há duas leituras possíveis da lei local:
- **permite transferência internacional** com salvaguardas (cláusulas contratuais, adequação), como faz a própria LGPD no art. 33; ou
- **exige localização**: dados pessoais armazenados fisicamente no país.

Fatos que reduzem o problema:
- Dado pessoal (nome, documento, endereço, contato) só existe no banco de Pedidos.
- O evento de status leva só `eventId`, `orderId` (UUID opaco), `status` e `version`. Isso é verificado no CI pelo teste `Eventos_nao_carregam_dado_pessoal`.
- O Catálogo não tem dado pessoal.

## Alternativas
1. **(A) Banco único com coluna `country`.** Atende se a lei permitir transferência com salvaguardas. Custo de infraestrutura extra zero. Não atende se a lei exigir localização.
2. **(B) Célula mínima por país:** API de Pedidos, banco e relay da outbox (com reconciliação) na região do país, mais um cache de leitura do Catálogo. Broker, notificador de webhooks, API Catálogo, gateway e observabilidade continuam compartilhados. Atende localização com o menor acréscimo possível.
3. **Célula completa por país** (B mais broker e notificador por país). Rejeitada: os eventos não têm dado pessoal, então duplicar broker e notificador aumenta custo e operação sem ganho de conformidade. Só volta se o parecer considerar o `orderId` dado pessoal sujeito à residência.
4. **Banco distribuído com placement geográfico.** Rejeitada: exige trocar a tecnologia de banco em 90 dias.
5. **Plataforma inteira separada por país.** Rejeitada: duplica Catálogo, borda e operação.

## Decisão
- Todo pedido grava `country` desde a fase de 30 dias. O país vem do `client_id` (claim propagada pelo gateway), não da configuração da instância: uma instância pode atender vários países (opção A) ou só um (opção B, que recusa os outros com `403`). Provado na fatia por `PaisTests`. Isso permite separar os dados depois sem migração de esquema.
- **Gatilho no dia 30: parecer jurídico do País 2.**
  - Permite transferência → **A**: pedidos do País 2 entram no banco atual com `country = <país>`; cláusulas contratuais ou adequação; transferência registrada no registro de operações de tratamento e no RIPD.
  - Exige localização → **B**: a célula mínima é implantada na região do país no dia 61–90.
- Na opção B:
  - host por país (`api-br`, `api-p2`); `client_id` vinculado a um país; o gateway bloqueia mismatch;
  - mesmo código e mesma versão nas duas células;
  - o relay roda na célula porque lê a outbox local, e publica no broker compartilhado só IDs e status.
- Catálogo: um serviço só, com cache de leitura em cada região, para o País 2 não pagar a ida e volta entre regiões no p95 da criação.
- Em nenhuma opção dado pessoal entra em evento, log ou trace (mascaramento no coletor). Relatórios consolidados só com dados agregados ou pseudonimizados.

## Consequências
**Técnicas**
- Nenhum gasto de infraestrutura no País 2 antes de a regra ser conhecida.
- A: menor custo. Se a lei mudar, a migração é por linhas `country = <país>`, porque o código e o esquema já são os da opção B.
- A: o cliente do País 2 acessa a região BR. A latência de rede até a borda fica fora do p95 medido no gateway, mas aparece na experiência do app.
- B: custo extra = uma instância da API de Pedidos, um banco gerenciado com réplica e um cache na região. Broker e notificador não são duplicados.
- B: o broker compartilhado vira dependência dos dois países. Se ele cair, os eventos atrasam nos dois, mas a criação de pedidos segue, porque a outbox acumula (Q5).
- Pedido não migra entre países. Cliente que compra nos dois países tem dois registros.
- Pipeline com deploy por anel: BR canário → BR → País 2.
- Se o parecer exigir também eventos ou logs locais, a alternativa 3 volta a valer (risco R2).

**Operacionais**
- Opção B: mais uma célula para monitorar, com painéis por país e deploy por anel; plantão cobre as duas regiões.
- Opção A: transferência internacional registrada e revisada anualmente com o jurídico.

**Quando revisitar**
- Mudança na lei do País 2 ou entrada de um terceiro país.
- Parecer considerar o `orderId` dado pessoal sujeito à residência (alternativa 3).
