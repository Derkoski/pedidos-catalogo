# ADR-004 – Versionamento de API por URL e convivência v1/v2

Status: Aceito
Data: 2026-10-21

## Contexto
App e parceiros precisam de um contrato com idempotência obrigatória e snapshot na resposta. O web atual não pode quebrar e o contrato atual tem que ficar compatível por pelo menos 6 meses.

## Alternativas
1. **Mudar o contrato atual no lugar.** Quebra o web. Rejeitada.
2. **Versão por header ou media type** (`Accept: application/vnd...v2+json`). Mais "puro", mas pior para parceiros, cache, logs e roteamento no gateway. Rejeitada.
3. **Versão major na URL (`/v1`, `/v2`), mesmo serviço e mesmo domínio por trás.** Simples de rotear, medir e desligar. Escolhida.
4. Serviço novo para v2 (Strangler com dois backends). Duplica regra de negócio e banco. Rejeitada; o gateway faz o papel de fachada, mas o backend é um só.

## Decisão
- `/v1` mantém o contrato atual, com uma única mudança aditiva: aceita `Idempotency-Key` opcional.
- `/v2` é o contrato novo. Os dois são adaptadores finos sobre o mesmo caso de uso e o mesmo banco.
- Dentro de uma versão, só mudanças aditivas. Mudança que quebra gera nova versão major.
- Desligamento do v1: no mínimo 6 meses após o GA do v2, com headers `Deprecation` e `Sunset`, aviso aos consumidores e uso do v1 próximo de zero nas métricas.
- Teste de contrato do v1 no CI: requisição e resposta do consumidor atual gravadas como fixture.

## Consequências
**Técnicas**
- Web atual segue sem mudança. App e parceiros já nascem no v2.
- Manter dois adaptadores por 6+ meses. Custo baixo porque o domínio é único.
- Precisa de métrica de uso por versão e por cliente para decidir o desligamento.

**Operacionais**
- Painel de uso por versão e por `client_id`; o TL da API é o dono da decisão de desligar o `/v1`.
- Aviso aos consumidores com 6 meses de antecedência (`Deprecation`, `Sunset`, comunicação direta).
- Consumidor que não migrar até o Sunset: prorrogação só por exceção técnica com prazo ([excecoes.md](../excecoes.md)).
- Custo: dois adaptadores e dois testes de contrato mantidos no CI.

**Quando revisitar**
- Uso do `/v1` zerado por 2 semanas: desligar.
- Nova mudança que quebra contrato: `/v3`, mesma regra.
