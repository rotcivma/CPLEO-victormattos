# Relatório da FASE 5 — Protótipo da movimentação

**Data:** 2026-11-27 · **Código:** `unity/Propulsor` · **Instruções:** `unity/README.md`

## 1. Entregue
| Item | Arquivo | Descrição |
|---|---|---|
| Lógica de movimento | `Core/Physics/PlayerMotor.cs` | Estados Grounded/Thrusting/Falling/Dead/DeadStopped; gravidade + empuxo com limites; velocidade horizontal crescente com teto; chão/teto; morte com quiques, deslizamento e parada |
| Passo fixo | `Core/Physics/FixedStepper.cs` | 50 Hz independente do framerate, com limite anti-travamento e fração para interpolação |
| Parâmetros | `Core/Physics/PlayerPhysicsParams.cs`, `Data/PlayerPhysicsDef.cs` | valores [E] da Fase 1; asset editável com ajuste ao vivo |
| Eventos | `Core/Events/EventBus.cs`, `GameEvents.cs` | barramento tipado + eventos RunStarted, DistanceChanged, PlayerStateChanged, PlayerLanded, PlayerDied, PlayerStopped |
| Input | `Gameplay/PlayerInput.cs` | botão único: toque, mouse, espaço; Input System novo e antigo |
| Jogador em cena | `Gameplay/PlayerView.cs` | ponte motor→Unity, interpolação, partículas do jato, poses por estado |
| Câmera | `Gameplay/CameraFollow.cs` | segue X, jogador a 22 % da largura, altura visível fixa, tremor |
| Cenário | `Gameplay/CorridorView.cs` | corredor infinito reciclado, pilares com parallax, marcos a cada 10 m |
| HUD de teste | `Gameplay/DebugHud.cs` | distância, recorde da sessão, estado, velocidades, tempo no ar, FPS |
| Montagem | `Bootstrap/PrototypeBootstrap.cs` | monta tudo em código ao apertar Play |
| Testes | `Tests/EditMode` (20), `Tests/PlayMode` (5) | ver §2 |

Arte 100 % provisória, geométrica e original (protagonista Nina: corpo verde-água, mochila laranja, óculos).

## 2. Resultados de testes
| Conjunto | Executado? | Resultado |
|---|---|---|
| EditMode / lógica pura — 20 testes (F-01…F-05, eventos, passo fixo, morte) | **Sim**, via `dotnet test` (.NET 8, NUnit 3.14) neste ambiente | **20/20 passaram** |
| PlayMode — 5 testes (teto, chão, câmera 22 %, morte, reinício) | **Não** — não há Unity neste ambiente | pendente |
| Scripts Unity (Gameplay/Bootstrap/Data) | **Não compilados** — sem Unity | pendente |

Valores derivados dos parâmetros atuais (cálculo, não medição de referência):
- chão → teto segurando: ≈ 0,72 s;
- teto → chão soltando: ≈ 0,61 s;
- velocidade horizontal: 8 → 20 u/s em 240 s (≈ 3.360 m).

## 3. Não verificado / limitações
- Compilação e execução no Unity (preciso que você rode — passos abaixo).
- Sensação de controle comparada ao jogo de referência (teste F-06, M-01): exige vídeo de referência e teste manual.
- A curva de aceleração horizontal provavelmente está lenta demais; calibrar com a Fase 6 (dificuldade).

## 4. Como validar (você, no Unity 6)
1. Abrir `unity/Propulsor` no Unity Hub (Unity 6 LTS).
2. Play numa cena vazia → testar controles (segurar/soltar, K, R).
3. *Window ▸ General ▸ Test Runner*: rodar EditMode e PlayMode.
4. Me enviar: erros do Console (se houver), resultado dos testes e impressão da sensação (pesado/leve, rápido/lento).

## 5. Próximos passos (Fase 6 — gameplay)
Segmentos e pooling → arcos de faísca → feixes de corte → drones-foguete → porcas/fichas → veículos. Começo pela geração por segmentos e pelos perigos, com a lógica novamente em C# puro e testada aqui.
