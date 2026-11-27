# Relatório da FASE 7 — Menus e fluxo de telas

**Data:** 2026-11-27 · **Código:** `unity/Propulsor` · **Especificação seguida:** `docs/fase-2/02-telas-detalhadas.md`

## 1. Arquitetura
```
GameFlow (Core, C# puro) ── estados e regras de navegação, testados sem Unity
   ▲ comandos                         │ GameStateChanged (EventBus)
FlowController (Gameplay) ── liga o fluxo à corrida: pausa a simulação, credita resultados,
   ▲                                   conduz Garra da Sorte e revive
UIManager + telas (UI) ── só apresentação (uGUI montado em código)
```
- `GameFlow`: Splash → Carregamento → Menu → Saída pela porta → Corrida ⇄ Pausa/Contagem → Morrendo → Revive? → Garra? → Resultados → De novo / Menu.
- `ClawMachine`: 1 ficha = 1 tentativa, prêmio sorteado antes da animação, troca a 100 porcas/ficha, opção de garantir prêmio na última ficha (para o gadget da Fase 8).
- `PopupQueue`: avisos do HUD um por vez (0,3 s + 2 s + 0,3 s).
- `SessionProfile`: saldo, recorde, fichas guardadas, Segundas Chances, bônus pendentes e configurações. **Não é salvo ainda**: a persistência é a Fase 8.
- `RunSimulation.Revive()`: volta no ponto onde o corpo parou, com a velocidade de antes, invulnerável por 2 s.

## 2. Telas implementadas
| Tela | Estado | Destaques |
|---|---|---|
| S01 Splash | ✅ | logo PROPULSOR; toque pula |
| S02 Carregamento | ✅ | ícone girando, barra, dica aleatória; mínimo 0,8 s |
| S03 Menu principal | ✅ | sobreposto à cena parada; toque livre inicia; saldo, recorde, ⚙; Oficina/Missões/Conquistas mostram "chega na Fase 8" |
| S04 HUD | ✅ | distância (fica dourada após o recorde), melhor, porcas com "pop", pausa, contagem 3-2-1, avisos em fila ("Novo recorde!", "+1 ficha", nome do veículo) |
| S05 Pausa | ✅ | continuar com contagem, som, desistir com confirmação; missões chegam na Fase 8 |
| S06 Resultados | ✅ | contadores animados em sequência, "NOVO RECORDE", toque acelera; De novo / Menu / Oficina |
| S07 Garra da Sorte | ✅ | garra desce/pega/sobe em 1 s, prêmio revelado, trocar fichas, "Voltar à corrida!" com Segunda Chance |
| S08 Oferta de revive | ✅ | barra de 4 s, Usar/Não, expira sozinha |
| S16 Configurações | ✅ (sem persistência) | volumes, vibração; **atalhos de teste** +1 Segunda Chance e +3 Fichas até existir a Oficina |
| S09/S10/S15/S17 | ⏳ | Fase 8 |

Outros comportamentos:
- Toques que começam sobre um botão não acionam o propulsor.
- Perder o foco do app pausa a partida.
- As teclas de teste (K, V, R) só funcionam no editor e em builds de desenvolvimento.

## 3. Testes
| Conjunto | Executado | Resultado |
|---|---|---|
| Lógica — 109 testes (25 novos: fluxo, garra, avisos, perfil, revive) | Sim, `dotnet test` | **109/109** |
| Sintaxe C# de todos os scripts | Sim (Roslyn) | 0 erros |
| PlayMode — 4 novos (telas por estado, pausa congela, garra → resultados → de novo, revive) | Não (sem Unity) | pendente |

Matriz: N-01…N-04 ✅ na lógica (telas pendentes no Unity), G-01 e G-02 ✅, R-01 parcial.

## 4. Desvios e decisões
| Item | Decisão |
|---|---|
| Ordem pós-morte | corpo para → revive (se houver consumível) → garra (se houver fichas) → resultados; a Segunda Chance ganha na garra volta direto à corrida [H, conforme Fase 1] |
| Fichas não usadas ao sair da garra | trocadas automaticamente por porcas |
| Botão "Missões" dos resultados | substituído por "Menu" até a Fase 8 |
| Fontes | fonte embutida do Unity; as fontes OFL do guia entram na Fase 9 |
| Porta de saída no início da corrida | animação simplificada (a protagonista só sai correndo) |

## 5. Próximo passo — Fase 8
Economia, Oficina (loja + inventário), gadgets aplicados à corrida, missões, patentes, conquistas e SaveManager (JSON local com backup e migração). O perfil desta fase passa a ser salvo.
