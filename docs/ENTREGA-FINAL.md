# Entrega final — Projeto PROPULSOR

**Data:** 2026-11-27 · **Branch:** `claude/jetpack-joyride-reverse-engineering-317rul`

Engenharia reversa **funcional e observacional** de *Jetpack Joyride* (Halfbrick, 2011)
e implementação de um **jogo original do mesmo gênero** em Unity 2D / C#.

## 1. Escopo legal (resumo)
- Foram estudados **mecânicas, sistemas e fluxos**, a partir de fontes públicas.
- Nenhum código, asset, texto, som ou imagem do original foi extraído, descompilado ou redistribuído.
- A implementação usa **nome, protagonista, cenário, nomes de itens e veículos, arte, sons e músicas próprios**:
  - Jogo PROPULSOR, com a protagonista Nina, na fábrica Engrenópolis.
  - Moeda "porcas", fichas e a Garra da Sorte.
  - Arcos de faísca, feixes de corte, drones-foguete e robôs operários.
- Recursos de terceiros: só as fontes **Lilita One** e **Nunito**, ambas com licença SIL OFL 1.1 e com o arquivo de licença incluído.

## 2. Os 15 entregáveis do Prompt Mestre

| # | Entregável | Onde está | Situação |
|---|---|---|---|
| 1 | Documento completo de engenharia reversa | `docs/fase-1/*` (relatório, versão/fontes, telas, física, obstáculos, veículos, economia, progressão, lacunas) | ✅ com níveis de confiança [C]/[G]/[H]/[E] |
| 2 | Catálogo de todas as telas | `docs/fase-1/02-catalogo-telas.md` (17 telas da referência) · `docs/fase-2/02-telas-detalhadas.md` (especificação própria com wireframes) | ✅ |
| 3 | Catálogo de todos os componentes | `docs/fase-4/01-arquitetura.md` (19 componentes) · `unity/README.md` (estrutura do código) | ✅ |
| 4 | Mapa de navegação | `docs/fase-1/02-catalogo-telas.md` §2 · `docs/fase-3/01-mapeamento-sistemas.md` §1 (máquina de estados implementada em `GameFlow`) | ✅ |
| 5 | Fluxograma da gameplay | `docs/fase-1/03-gameplay-fisica.md` §1 · `docs/fase-3/01-mapeamento-sistemas.md` §8 · `docs/fase-6/00-relatorio-fase-6.md` §1 | ✅ |
| 6 | Documento de física | `docs/fase-1/03-gameplay-fisica.md` · implementação `Core/Physics/PlayerMotor.cs` + testes F-01…F-05 | ✅ (valores [E], calibração pendente) |
| 7 | Documento de obstáculos | `docs/fase-1/04-obstaculos.md` · `docs/fase-3/01` §4–5 · `docs/fase-6` | ✅ |
| 8 | Documento de veículos | `docs/fase-1/05-veiculos.md` · `docs/fase-3/01` §3 · `Core/Gameplay/Vehicles/VehicleMotors.cs` | ✅ |
| 9 | Documento de economia | `docs/fase-1/06-economia.md` · `docs/fase-8/00-relatorio-fase-8.md` | ✅ |
| 10 | Documento de progressão | `docs/fase-1/07-progressao.md` · `docs/fase-8` | ✅ |
| 11 | Arquitetura técnica | `docs/fase-4/01-arquitetura.md` · relatórios das fases 6–9 | ✅ |
| 12 | Modelo de dados | `docs/fase-4/02-modelo-de-dados.md` · `Core/Flow/SessionProfile.cs` · `Core/Save/SaveManager.cs` (JSON salvo) | ✅ |
| 13 | Plano de desenvolvimento | `docs/fase-4/03-plano-desenvolvimento.md` | ✅ (executado nas fases 5–10) |
| 14 | Plano de testes | `docs/fase-4/04-plano-de-testes.md` (matriz com resultados) | ✅ 57 itens aprovados na lógica · pendências na §5 |
| 15 | Protótipo funcional | `web/propulsor.html` (jogável no navegador, verificado sem erros) · `unity/Propulsor` (63 scripts, cerca de 10.400 linhas) · instruções em `unity/README.md` | ⚠️ **código completo e lógica testada; ainda não compilado nem executado no Unity** (ver §4) |

## 3. Versão jogável no navegador
`web/propulsor.html` é uma versão autocontida, sem dependências, que dá para jogar agora no computador ou no celular, sem Unity.
- **Mesmas regras do Core C#:** física, os 14 trechos, a curva de dificuldade, arcos, feixes, drones com aviso, os 8 veículos, robôs operários, raspões, arrancada e Garra da Sorte.
- **Oficina:** gadgets, consumíveis, propulsores e roupas.
- **Progressão:** missões e patentes, com prestígio.
- **Salvamento:** fica no navegador (`localStorage`).
- **Música:** as mesmas 3 trilhas sintetizadas do projeto Unity (menu, corrida e Garra), mais baixas durante avisos de drone e na pausa; botão próprio para ligar e desligar.
- **Fora desta versão:** conquistas, que existem só no projeto Unity.

## 4. O que o protótipo Unity contém
| Área | Conteúdo |
|---|---|
| Corrida | Corredor infinito com 14 trechos validados; arcos de faísca fixos e giratórios; feixes de corte; drones-foguete com aviso e rajadas; dificuldade crescente; porcas, fichas e caixas de veículo; 8 veículos; robôs operários; raspões; arrancada |
| Telas | Splash, carregamento, menu, placar da corrida, pausa, revive, Garra da Sorte, resultados, configurações, Oficina, missões, conquistas |
| Meta-jogo | Oficina com 30 itens, 6 gadgets, 3 consumíveis; 38 missões; 15 patentes e prestígio; 20 conquistas; estatísticas |
| Salvamento | JSON local com cópia de segurança, migração de versão, opção de apagar o progresso |
| Áudio | 21 efeitos e 3 músicas, todos sintetizados; música abaixa em avisos e na pausa |
| Qualidade | Lógica 100% em C# puro, testável fora do Unity; 50 Hz em passo fixo; corrida sem alocar memória depois de aquecida |

## 5. Testes — situação final
| Conjunto | Quantidade | Executado | Resultado |
|---|---|---|---|
| Lógica (EditMode, `dotnet test unity/tools/CoreTests`) | **177** | Sim, .NET 8 | **177/177 aprovados** |
| Sintaxe de todos os scripts (Roslyn, C# 9) | 63 arquivos | Sim | 0 erros |
| Integração no Unity (PlayMode) | 13 | **Não**: não há Unity neste ambiente | pendente |
| Compilação contra a API do Unity e builds | — | **Não** | pendente |

> Pela regra do Prompt Mestre ("não declarar reproduzido sem testar"), nada que dependa
> do Unity está marcado como reproduzido. O roteiro em `docs/ROTEIRO-VALIDACAO.md` cobre essa validação.

## 6. Limitações e dúvidas registradas
1. **Sem observação direta do original.** A rede bloqueou wikis e sites, então a pesquisa usou trechos de busca e não houve gravações. Os parâmetros de física, os tempos dos perigos e os preços são estimativas [E].
2. **Sem Unity neste ambiente**, como descrito na §4.
3. **Divergências entre fontes**, por exemplo na troca de tokens (50 ou 100 moedas) e nas estrelas por ciclo (63 ou 105). Adotei valores próprios e registrei cada um.
4. **Itens da referência fora do escopo:** compras com dinheiro real, salvamento em nuvem, eventos sazonais (S17), placares online.
5. **A arte é provisória e geométrica.** É original, mas precisa de um artista para chegar a um visual final.

## 7. Próximos passos recomendados
1. Rodar o roteiro de validação no Unity 6 e me trazer erros e resultados.
2. Gravar partidas da referência e calibrar `PlayerPhysics.asset` (testes F-06 e M-01).
3. Fazer o build Android e medir 60 fps (testes D-01 e O-06 no Profiler).
4. Trocar a arte provisória por arte final própria.
