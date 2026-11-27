# Engenharia reversa funcional — runner de jetpack (referência: Jetpack Joyride)

> **Comece por [ENTREGA-FINAL.md](ENTREGA-FINAL.md).** Para jogar agora sem Unity: abra `web/propulsor.html` no navegador.

Este diretório reúne a documentação do projeto de engenharia reversa **funcional**
do jogo *Jetpack Joyride* (Halfbrick Studios, 2011), conforme o "Prompt Mestre".

## Escopo e limites legais (ler antes de tudo)

- O objetivo é **entender mecânicas, sistemas e fluxos** e documentá-los.
  Mecânicas de jogo não são protegidas por direitos autorais; nome, personagens,
  arte, música, textos e logotipos são.
- A implementação (Fases 5–10) será um **jogo próprio do mesmo gênero**
  (runner horizontal infinito com jetpack), com **nome, personagem, arte, sons e
  textos originais**. Ela **não** usará o nome "Jetpack Joyride", o personagem
  Barry Steakfries, os nomes de veículos/itens da Halfbrick nem imitará o visual
  específico do jogo. Na arquitetura, cada sistema original terá um equivalente
  com nome próprio (ex.: "veículo mecha saltador" em vez de "Lil' Stomper").
- Nenhum código, binário ou asset do jogo original é descompilado, extraído
  ou redistribuído. A "engenharia reversa" aqui é **observacional**: fontes
  públicas (wikis, guias, FAQs oficiais) e, quando possível, observação de jogo.

## Legenda de confiabilidade (usada em todos os documentos)

| Marca | Significado |
|---|---|
| **[C]** | Confirmado por ao menos uma fonte pública consultada (citada no documento). |
| **[C*]** | Confirmado, mas fontes divergem ou é dependente de versão. |
| **[G]** | Conhecimento geral sobre o jogo, **não verificado** nesta sessão. Precisa de validação por observação. |
| **[H]** | Hipótese / inferência de design. |
| **[E]** | Estimativa numérica proposta para a implementação (não é o valor original). |

## Estrutura

| Fase | Documento | Status |
|---|---|---|
| 1 | [fase-1/00-relatorio-fase-1.md](fase-1/00-relatorio-fase-1.md) — relatório da fase | ✅ |
| 1 | [fase-1/01-versao-e-fontes.md](fase-1/01-versao-e-fontes.md) — versão analisada, fontes, método | ✅ |
| 1 | [fase-1/02-catalogo-telas.md](fase-1/02-catalogo-telas.md) — catálogo inicial de telas e mapa de navegação | ✅ (inicial) |
| 1 | [fase-1/03-gameplay-fisica.md](fase-1/03-gameplay-fisica.md) — personagem, controles, física | ✅ (inicial) |
| 1 | [fase-1/04-obstaculos.md](fase-1/04-obstaculos.md) — perigos e dificuldade | ✅ (inicial) |
| 1 | [fase-1/05-veiculos.md](fase-1/05-veiculos.md) — veículos | ✅ (inicial) |
| 1 | [fase-1/06-economia.md](fase-1/06-economia.md) — moedas, tokens, Final Spin, loja | ✅ (inicial) |
| 1 | [fase-1/07-progressao.md](fase-1/07-progressao.md) — missões, patentes, conquistas | ✅ (inicial) |
| 1 | [fase-1/08-lacunas-e-proximos-passos.md](fase-1/08-lacunas-e-proximos-passos.md) — dúvidas, limitações, plano | ✅ |
| 2 | [fase-2/01-identidade-visual-original.md](fase-2/01-identidade-visual-original.md) — identidade própria, tokens, responsividade | ✅ |
| 2 | [fase-2/02-telas-detalhadas.md](fase-2/02-telas-detalhadas.md) — wireframes e especificação de cada tela | ✅ |
| 3 | [fase-3/01-mapeamento-sistemas.md](fase-3/01-mapeamento-sistemas.md) — máquinas de estado, geração, eventos, fluxograma | ✅ |
| 4 | [fase-4/01-arquitetura.md](fase-4/01-arquitetura.md) — 19 componentes | ✅ |
| 4 | [fase-4/02-modelo-de-dados.md](fase-4/02-modelo-de-dados.md) — Defs, SaveData, tabelas | ✅ |
| 4 | [fase-4/03-plano-desenvolvimento.md](fase-4/03-plano-desenvolvimento.md) | ✅ |
| 4 | [fase-4/04-plano-de-testes.md](fase-4/04-plano-de-testes.md) — matriz de testes | ✅ |
| 5 | [fase-5/00-relatorio-fase-5.md](fase-5/00-relatorio-fase-5.md) — protótipo de movimentação (`unity/Propulsor`) | ✅ lógica testada · ⏳ validação no Unity |
| 6 | [fase-6/00-relatorio-fase-6.md](fase-6/00-relatorio-fase-6.md) — gameplay: trechos, perigos, coletáveis, 8 veículos | ✅ lógica testada (84) · ⏳ validação no Unity |
| 7 | [fase-7/00-relatorio-fase-7.md](fase-7/00-relatorio-fase-7.md) — menus e fluxo de telas | ✅ lógica testada (109) · ⏳ validação no Unity |
| 8 | [fase-8/00-relatorio-fase-8.md](fase-8/00-relatorio-fase-8.md) — economia, Oficina, gadgets, missões, patentes, conquistas, salvamento | ✅ lógica testada (161) · ⏳ validação no Unity |
| 9 | [fase-9/00-relatorio-fase-9.md](fase-9/00-relatorio-fase-9.md) — desempenho, robôs operários, raspão, áudio sintetizado, fontes, efeitos | ✅ lógica testada (177) · ⏳ validação no Unity |
| 10 | [ENTREGA-FINAL.md](ENTREGA-FINAL.md) — mapa dos 15 entregáveis · [ROTEIRO-VALIDACAO.md](ROTEIRO-VALIDACAO.md) — validação no Unity | ✅ |

> Nota: o arquivo `cp1leoo.txt` na raiz do repositório é um código C# não
> relacionado a este projeto (Azure Function de verificação de portas) e foi
> deixado intocado.
