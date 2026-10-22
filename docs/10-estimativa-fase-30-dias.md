# 10 – Estimativa da primeira fase (30 dias)

> **Para o avaliador**  
> **Item do PDF:** 2.5.3 Estimativa técnica (diferencial)  
> **Critério de avaliação:** Atuação sênior  
> **O que conferir aqui:** time, esforço e custo em dois cenários, sem IA e com IA no desenvolvimento; os estudos que explicam o tamanho do ganho com IA; custo separado de preço; premissas e riscos.

Escopo: entregas dos dias 1–30 de [06](06-plano-30-60-90.md). Salários CLT informados pela empresa; o restante está marcado como premissa.

## Time (cenário sem IA)

| Papel | Dedicação | Dias úteis | Pessoa-dia |
|-------|-----------|-----------|-----------|
| Arquiteto de soluções | 50 % | 22 | 11 |
| Tech lead backend | 100 % | 22 | 22 |
| Desenvolvedor backend pleno | 2 × 100 % | 22 | 44 |
| Desenvolvedor web pleno (enviar a chave) | 25 % | 22 | 5,5 |
| QA (automação e carga) | 100 % | 22 | 22 |
| SRE/DevOps | 50 % | 22 | 11 |
| **Total** | | | **~116** |

## Esforço por entrega (cenário sem IA)

| Entrega | Pessoa-dia |
|---------|-----------|
| Migrações aditivas e modelo (chave, outbox, snapshot) | 8 |
| Idempotência no v1 (chave opcional) + concorrência | 15 |
| Snapshot de preço/descrição | 8 |
| Gravação na outbox + dual write | 10 |
| `/v2` fechado (contrato, adaptador) | 10 |
| Testes (contrato v1, idempotência, carga básica) | 20 |
| Observabilidade, dashboards, SLOs, alertas | 12 |
| Pipeline, flags, canário, rollback testado | 10 |
| Web enviando a chave | 5 |
| Arquitetura, ADRs, revisão, validação de premissas | 11 |
| Reserva para imprevistos (~6 %) | 7 |
| **Total** | **116** |

## Custos principais (cenário sem IA)

Regime: **CLT**, o único usado pela empresa. Os salários foram informados pela empresa, exceto o de SRE/DevOps, que é premissa (salário de sênior). A conta tem três camadas, para não misturar salário, custo e preço.

### 1. Custo da equipe na fase (1 mês)

| Papel | Salário mensal (CLT) | Dedicação | Salário no período | Custo para a empresa (× 1,66) |
|-------|---------------------:|----------:|-------------------:|------------------------------:|
| Arquiteto de soluções | R$ 16.000 | 50 % | R$ 8.000 | R$ 13.280 |
| Tech lead backend | R$ 16.000 | 100 % | R$ 16.000 | R$ 26.560 |
| Desenvolvedor backend pleno (2) | R$ 8.500 cada | 2 × 100 % | R$ 17.000 | R$ 28.220 |
| Desenvolvedor web pleno | R$ 8.500 | 25 % | R$ 2.125 | R$ 3.528 |
| QA | R$ 6.000 | 100 % | R$ 6.000 | R$ 9.960 |
| SRE/DevOps (premissa) | R$ 14.000 | 50 % | R$ 7.000 | R$ 11.620 |
| **Total** | | **5,25 FTE** | **R$ 56.125** | **R$ 93.168** |

### 2. De onde vem o fator 1,66 (encargos e provisões CLT)

| Componente | % sobre o salário |
|------------|------------------:|
| INSS patronal | 20,0 % |
| RAT/SAT (ajustado pelo FAP) | ~2,0 % |
| Terceiros (salário-educação, Sistema S, INCRA) | 5,8 % |
| FGTS | 8,0 % |
| 13º salário (1/12) | 8,33 % |
| Férias + 1/3 (1/12 × 4/3) | 11,11 % |
| Encargos sobre 13º e férias (35,8 % × 19,44 %) | 6,96 % |
| Provisão da multa de 40 % do FGTS | ~3,8 % |
| **Total** | **~66 % → fator 1,66** |

Fora do fator: benefícios (vale-refeição, plano de saúde etc.), equipamentos e licenças. Somar conforme a política da empresa. Premissa: a empresa não usa a desoneração da folha (CPRB, em transição pela Lei 14.973/2024); se usar, o INSS patronal muda e o fator cai.

### 3. Do custo ao preço da proposta

Preço = custo ÷ (1 − tributos sobre a receita − margem). Tributos e margem são definidos pela área comercial; a tabela é um exemplo para mostrar a conta.

| Parâmetro | Exemplo |
|-----------|--------:|
| PIS/COFINS (não cumulativo, antes de créditos) | 9,25 % |
| ISS (varia por município, 2 % a 5 %) | 5 % |
| Margem | 20 % |
| Divisor | 0,6575 |
| **Preço da fase no exemplo** | **≈ R$ 141,7 mil** |

IRPJ e CSLL incidem sobre o lucro e saem da margem.

### 4. Infraestrutura e ferramentas

| Item | Valor |
|------|-------|
| Novas tabelas no banco atual | Sem custo adicional esperado (mesma instância, premissa) |
| Observabilidade gerenciada e ambiente temporário de teste de carga | A cotar na calculadora do provedor depois de medir o tráfego real (semana 1). Provedor ainda não escolhido |
| Ferramentas (Spectral, AsyncAPI CLI, oasdiff, xUnit) | Open source, sem licença |

## Projeção com IA no desenvolvimento

O prazo de 30 dias é fixo (vem do PDF) e é ditado pelo rollout seguro, não pela velocidade de escrever código. A IA reduz **esforço e custo**; não reduz o calendário.

### 1. O que os estudos medem

| Estudo | Contexto | Resultado |
|--------|----------|-----------|
| Peng et al., 2023 (GitHub Copilot) | 95 devs, tarefa isolada e nova (servidor HTTP) | 55,8 % mais rápido. Teto para tarefa pequena e bem delimitada |
| Google, RCT 2024 (ICSE 2025) | 96 engenheiros, tarefa corporativa | ~21 % mais rápido. Intervalo de confiança largo (de ~40 % mais rápido a ~3 % mais lento) |
| Cui et al., 2025 (Management Science; Microsoft, Accenture e uma Fortune 100) | ~4,9 mil devs, trabalho real por semanas | +26 % de tarefas concluídas. Menos experientes: +27 % a 39 %. Mais experientes: +8 % a 13 % |
| METR, 2025 | 16 devs experientes, nos próprios repositórios maduros | **19 % mais lentos** com IA (e achavam que estavam mais rápidos) |
| DORA 2024 (Google) | Pesquisa ampla com times de entrega | Mais adoção de IA associada a −1,5 % de vazão e −7,2 % de estabilidade de entrega |

### 2. Por que o ganho é de ~20 %, e não "1 dev faz o trabalho de 10"

1. **Só parte do trabalho é escrever código.** Nesta fase, 76 dos 116 pessoa-dia são código e teste. O resto é observabilidade, pipeline, canário, arquitetura e validação com o cliente. Mesmo que a IA escrevesse todo o código em tempo zero, sobrariam ~40 pessoa-dia: o ganho máximo teórico seria ~2,9x, não 10x.
2. **O ganho cai em código existente e complexo.** Idempotência com concorrência e outbox no banco atual são o caso estudado pela METR, em que o ganho foi negativo.
3. **Código gerado precisa ser revisado e testado.** Sem essa disciplina, a estabilidade cai (DORA). Por isso a dedicação do tech lead não diminui: ele passa a revisar mais.
4. **O calendário não comprime.** Canário de 1 semana, janela de release do web e mapeamento de consumidores levam o mesmo tempo com ou sem IA.

E por que não usar um ganho menor: o time é majoritariamente pleno, perfil com os maiores ganhos medidos (+27 % a 39 %). As tarefas de código são delimitadas, e os testes da fatia executável servem de especificação para o código gerado.

### 3. Ganho aplicado por tipo de entrega

| Tipo | Entregas | Ganho aplicado | Base nos estudos |
|------|----------|---------------:|------------------|
| A. Código novo e delimitado | migrações, `/v2`, testes, web | 30 % | Entre o Google (~21 %) e o teto de tarefa isolada (55,8 %); time pleno (+27 % a 39 %) |
| B. Código no sistema existente, com concorrência | idempotência, outbox, snapshot | 20 % | Ganho médio em trabalho real (~21 % a 26 %), descontado o risco medido pela METR |
| C. Operação, coordenação e decisão | observabilidade, pipeline, arquitetura | 10 % | Ganho só em configuração como código e rascunho de documentos |
| D. Reserva para imprevistos | – | 0 % | Risco não diminui; revisar código gerado é risco novo |

| Entrega | Tipo | Sem IA (pessoa-dia) | Com IA (pessoa-dia) |
|---------|------|--------------------:|--------------------:|
| Migrações aditivas e modelo | A | 8 | 5,6 |
| Idempotência no v1 + concorrência | B | 15 | 12,0 |
| Snapshot de preço/descrição | B | 8 | 6,4 |
| Gravação na outbox + dual write | B | 10 | 8,0 |
| `/v2` fechado (contrato, adaptador) | A | 10 | 7,0 |
| Testes (contrato v1, idempotência, carga básica) | A | 20 | 14,0 |
| Observabilidade, dashboards, SLOs, alertas | C | 12 | 10,8 |
| Pipeline, flags, canário, rollback testado | C | 10 | 9,0 |
| Web enviando a chave | A | 5 | 3,5 |
| Arquitetura, ADRs, revisão, validação de premissas | C | 11 | 9,9 |
| Reserva para imprevistos | D | 7 | 7,0 |
| **Total** | | **116** | **93,2 (−20 %)** |

### 4. Time com IA (mesmos papéis, menos dedicação)

| Papel | Sem IA | Com IA | Custo com IA (salário × dedicação × 1,66) |
|-------|-------:|-------:|-----------------------------------------:|
| Arquiteto de soluções | 50 % | 45 % | R$ 11.952 |
| Tech lead backend | 100 % | 100 % (revisa o código gerado) | R$ 26.560 |
| Desenvolvedor backend pleno (2) | 2 × 100 % | 2 × 70 % | R$ 19.754 |
| Desenvolvedor web pleno | 25 % | 15 % | R$ 2.116 |
| QA | 100 % | 85 % | R$ 8.466 |
| SRE/DevOps (premissa) | 50 % | 40 % | R$ 9.296 |
| **Total** | **5,25 FTE** | **4,25 FTE (~93,5 pessoa-dia)** | **R$ 78.145** |

### 5. Custo da ferramenta de IA

| Item | Conta | Valor no mês |
|------|-------|-------------:|
| Licenças (GitHub Copilot Business, US$ 19/usuário/mês) | 7 pessoas × US$ 19 × R$ 5,04 | R$ 670 |
| Teto de consumo acima da franquia (política, não estimativa) | 7 × US$ 10 × R$ 5,04 | R$ 353 |
| **Total** | | **≈ R$ 1.023** |

Câmbio de 21/10/2026 (R$ 5,04). Outra ferramenta corporativa (por exemplo, Claude Code) troca a linha, não a conta.

### 6. Como provar o ganho (e não só prometer)
- Semanas 1 e 2: comparar o esforço planejado com o realizado nas entregas de tipo A e B.
- Indicadores de qualidade não podem piorar: falhas no CI, defeitos achados no canário, retrabalho na revisão.
- Se o ganho medido for menor, a reserva cobre a diferença na fase de 30 dias, e as fases de 60 e 90 são reestimadas com o número real.
- Guardrails: todo código gerado passa pelos mesmos testes, lint de contrato e revisão. Nenhum dado real, credencial ou código do cliente sai para a ferramenta sem aprovação (ver [uso de IA](uso-de-ia.md)).

Alternativa com o mesmo custo: manter o time sem IA e usar o ganho para antecipar entregas dos 60 dias (por exemplo, o Catálogo em lote). A escolha é do cliente.

## Resumo: sem IA × com IA

| | Sem IA | Com IA | Diferença |
|-|-------:|-------:|----------:|
| **Fase de 30 dias** | | | |
| Esforço | ~116 pessoa-dia (5,25 FTE) | ~93 pessoa-dia (4,25 FTE) | −20 % |
| Custo da equipe (CLT, empresa) | R$ 93,2 mil | R$ 78,1 mil | |
| Ferramenta de IA | – | R$ 1,0 mil | |
| **Custo total** | **R$ 93,2 mil** | **R$ 79,2 mil** | **−15 %** |
| Preço no exemplo da seção "Do custo ao preço" | ≈ R$ 141,7 mil | ≈ R$ 120,4 mil | −15 % |
| **Fases de 60 e 90 dias (projeção)** | | | |
| Equipe | 6,25 FTE × 2 meses (~275 pessoa-dia) | 4,95 FTE × 2 meses (~218 pessoa-dia) | −21 % |
| Custo total (equipe + ferramenta) | ≈ R$ 214,6 mil | ≈ R$ 178,4 mil | −17 % |
| Preço no mesmo exemplo | ≈ R$ 326,3 mil | ≈ R$ 271,3 mil | |
| Infraestrutura | A cotar | A cotar | – |

As fases de 60 e 90 dias com IA aplicam os mesmos ganhos (o backend extra também a 70 %) e serão recalibradas com o ganho medido nas semanas 1 e 2.

Fontes dos estudos: Peng et al., *The Impact of AI on Developer Productivity: Evidence from GitHub Copilot* (arXiv 2302.06590); Paradis et al., *How much does AI impact development speed? An enterprise-based randomized controlled trial* (arXiv 2410.12944); Cui et al., *The Effects of Generative AI on High-Skilled Work: Evidence from Three Field Experiments with Software Developers* (Management Science, 2025; SSRN 4945566); METR, *Measuring the Impact of Early-2025 AI on Experienced Open-Source Developer Productivity* (2025); Google Cloud, *Accelerate State of DevOps Report 2024* (DORA).

## Premissas
- Time já conhece o código atual e tem acesso a produção com pipeline existente.
- Banco relacional atual suporta as novas tabelas sem trocar instância.
- Feature flags disponíveis (ou biblioteca simples adotada na semana 1).
- Salários CLT informados pela empresa; SRE/DevOps a R$ 14.000 é premissa. Benefícios fora do custo.
- Tributos e margem da seção 3 são exemplo; os valores reais vêm da área comercial.
- Cenário com IA: ganhos por tipo de entrega baseados nos estudos citados; time com acesso a uma ferramenta corporativa aprovada; ganho medido nas semanas 1 e 2.

## Riscos da fase
- Descoberta tardia de consumidores do evento atual: aumenta o esforço do dual write.
- Concorrência na idempotência mais difícil no banco atual (isolamento, locks): reserva cobre ~1 semana.
- Web sem janela de release no período: entrega de valor parcial (só app/v2 preparados).
- Ganho real com IA abaixo do projetado: a reserva cobre a fase de 30 dias; as fases seguintes são reestimadas.
