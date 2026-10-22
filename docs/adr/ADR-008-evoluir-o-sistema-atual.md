# ADR-008 – Evoluir o sistema atual em vez de reescrever em microsserviços

Status: Aceito
Data: 2026-10-22

## Contexto
Em 90 dias a plataforma precisa atender app, parceiros e um segundo país, com 10x de volume, sem interromper o web atual e com a primeira melhoria em produção em 30 dias. Os problemas que causam risco (duplicidade, evento fora da transação, N+1, falta de snapshot) estão dentro do serviço de Pedidos e não vêm do tamanho do serviço. O time e a stack atual são conhecidos; a operação de muitos serviços novos, não.

## Alternativas
1. **Reescrever Pedidos e Catálogo como microsserviços novos.** Não entrega nada em 30 dias, exige migração de dados com o sistema rodando e multiplica deploy, observabilidade e plantão. Rejeitada.
2. **Strangler Fig com um serviço novo para o `/v2`** (dois backends atrás do gateway). Isola o código novo, mas duplica regra de negócio e banco e cria sincronização entre os dois durante 6+ meses. Rejeitada.
3. **Evoluir no lugar:** mesmo serviço e mesmo banco de Pedidos, com adaptadores finos `/v1` e `/v2` sobre um único caso de uso; gateway como fachada para roteamento, quotas e desligamento do `/v1`. Escolhida.

## Decisão
- Pedidos e Catálogo continuam como estão hoje (dois serviços, dois bancos). Nenhum serviço novo de negócio.
- Mudanças entram por migração aditiva, feature flag e canário ([06](../06-plano-30-60-90.md)).
- Componentes novos só onde não há alternativa: gateway, relay da outbox, broker e notificador de webhooks.
- As fronteiras internas (Domain, Application, Infrastructure, Api) são checadas no CI (`ArquiteturaTests`), para que uma separação futura seja possível.

## Consequências
**Técnicas**
- A primeira melhoria cabe em 30 dias e o web não percebe a mudança.
- Uma única regra de negócio para `/v1` e `/v2`; o teste de contrato do v1 protege o consumidor atual.
- O serviço de Pedidos continua escalando como um bloco só (autoescala horizontal, stateless).

**Operacionais**
- Poucos componentes novos para operar; o time mantém a stack e o processo de deploy que conhece.
- Exige disciplina de feature flag e de remoção de caminhos antigos (descomissionamento com data em [06](../06-plano-30-60-90.md)).

**Quando revisitar**
- Parte do domínio passar a ter ritmo de mudança, escala ou time próprio (por exemplo, notificações com volume muito maior que pedidos): extrair só essa parte, usando as fronteiras já checadas no CI.
- Teste de carga mostrar que o bloco único não atinge Q1 mesmo com autoescala.
