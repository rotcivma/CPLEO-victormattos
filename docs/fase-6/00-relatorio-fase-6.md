# Relatório da FASE 6 — Implementação da gameplay

**Data:** 2026-11-27 · **Código:** `unity/Propulsor` · **Execução:** `unity/README.md`

## 1. Arquitetura adotada
Toda a regra do jogo fica em **`Propulsor.Core` (C# puro)**, testável fora do Unity.
O Unity só **conduz** (`RunDriver`: input + passo fixo de 50 Hz) e **desenha**
(`PlayerView`, `WorldView`, `CorridorView`, `DebugHud`).

```
Input ─► RunDriver ─► RunSimulation.Step(dt, segurando, tocou)
                        ├─ PlayerMotor / IVehicleMotor (vertical) + horizontal
                        ├─ StreamWorld: SegmentSelector → PlacedSegment (+ porcas, fichas, tokens)
                        ├─ LaserSequence (presa à tela)
                        ├─ MissileDirector (aviso → trava → voo; bloqueios)
                        ├─ CollectPickups (ímã opcional)
                        └─ CheckHazards → morte | veículo destruído | blindagem
                     ─► EventBus ─► HUD / (Fase 8: missões, economia, conquistas)
Views leem o estado da simulação e usam pools (sem instanciar durante a corrida).
```

## 2. Entregue
| Sistema | Arquivos (Core) | O que faz |
|---|---|---|
| Aleatoriedade determinística | `Gameplay/Random.cs` | Xorshift32 com semente: mesma corrida em .NET e no Mono do Unity |
| Trechos | `SegmentDef.cs`, `SegmentLibrary.cs` | 14 trechos próprios (aquecimento, arcos estáticos, rotativos, túnel, feixes, denso) |
| Seleção | `SegmentSelector.cs`, `Difficulty.cs` | peso × afinidade com a dificuldade; distância mínima; sem repetir os últimos 3 |
| Validador | `PathValidator.cs` | prova que cada trecho tem trajetória livre na velocidade máxima, margens livres, faixas livres nos feixes, slots fora de perigo |
| Arcos de faísca | `Hazards.cs` | estáticos e rotativos; colisão cápsula × caixa |
| Feixes de corte | `LaserSequence.cs` | entrada → aviso → disparo por onda → saída; dano só no disparo |
| Drones-foguete | `MissileDirector.cs` | aviso que segue a altura → trava 0,4 s → voo; tempo de rastreio 1,4→0,6 s; rajadas após 1.500 m; bloqueio durante feixes e na Moto-Trilho |
| Coletáveis | `RunSimulation.cs` | porcas (linhas, arcos, blocos), fichas, tokens de veículo, ímã |
| Veículos | `Vehicles/VehicleMotors.cs` | 8 veículos próprios com física e controle distintos |
| Corrida | `RunSimulation.cs` | integra tudo; invulnerabilidade de 1 s ao perder veículo; Guindaste Blindado com 3 PV contra drones |
| Eventos | `Events/GameEvents.cs` | +10 eventos (moedas, fichas, veículos, feixes, drones, causa da morte) |

Unity: `RunDriver`, `PlayerView` (a pé + 8 veículos, ponteiro da Cápsula Blink, inversão do Traje Inverso, blindagem escurecendo), `WorldView` (arcos com bobinas e faísca tremulante, feixes com emissores deslizando, avisos piscando 4/10 Hz, drones, porcas, fichas, caixas de veículo, água do Jet-Esqui), `DebugHud` e bootstrap atualizado. Tecla **V** monta veículos em sequência para teste.

## 3. Testes
| Conjunto | Executado | Resultado |
|---|---|---|
| Lógica (EditMode) — **84 testes** | Sim, `dotnet test` | **84/84 passaram** |
| Sintaxe C# de todos os scripts Unity | Sim (Roslyn, C# 9) | 0 erros |
| Compilação contra a API Unity | Não (sem Unity aqui) | pendente |
| PlayMode — 7 testes | Não | pendente |

Matriz atualizada (`docs/fase-4/04-plano-de-testes.md`): C-01…C-06, O-01…O-05, K-01, K-03 ✅; V-01…08 ✅ na lógica; K-02 parcial; O-06 pendente.

**Bug encontrado pelos testes e corrigido:** o validador discretizava o eixo X de forma que o arredondamento zerava o movimento vertical, reprovando 5 trechos válidos. O passo agora é derivado da velocidade de subida. Os testes negativos (parede completa, zigue-zague impossível, feixes sem faixa livre, arco na margem) continuam sendo rejeitados.

## 4. Decisões e hipóteses registradas
| Item | Decisão | Tipo |
|---|---|---|
| Token de veículo coletado já montado | vira 10 porcas | [H] |
| Feixes/arcos contra o Guindaste Blindado | destroem em 1 acerto (só drones gastam PV) | [H] |
| Frequência de tokens | veículo a cada 250–450 m; ficha a cada 300–600 m | [E] |
| Primeiro drone | após 150 m; intervalo 8–12 s → 2,5–5 s | [E] |
| Velocidade relativa do drone | 30 u/s | [E] |

## 5. Pendências
- Validar no Unity: compilação, PlayMode, sensação de cada veículo, desempenho (O-06, D-01).
- "Raspão" (HazardNearMiss) e derrubar robôs operários: ficam para a Fase 8 (só servem às missões).
- Tremor de câmera e efeitos de explosão: polimento na Fase 9.

## 6. Próximo passo — Fase 7 (menus)
UIManager com pilha de telas; telas S01–S08 e S16 da especificação (splash, carregamento, menu que vira jogo ao tocar, HUD definitivo, pausa com contagem, resultados, Garra da Sorte, revive, configurações).
