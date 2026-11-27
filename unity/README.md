# PROPULSOR — protótipo Unity (Fases 5–8)

Jogo original do gênero "runner com propulsor". Ver `docs/` para toda a
investigação, arquitetura e planos.

## Requisitos
- **Unity 6 LTS** (6000.0.x) — o `Packages/manifest.json` usa versões de pacote do Unity 6.
- Opcional: .NET SDK 8 para rodar os testes da lógica pura fora do Unity.

## Abrir e jogar
1. Unity Hub ▸ **Add ▸ Add project from disk** ▸ selecione `unity/Propulsor`.
2. Escolha a versão Unity 6 quando o Hub perguntar.
3. Se aparecer o aviso do **Input System** pedindo para habilitar os novos backends, clique **Yes** (o editor reinicia). Clicar *No* também funciona (o código suporta os dois sistemas).
4. Abra qualquer cena (a `SampleScene` vazia ou *File ▸ New Scene*) e aperte **Play**.
   O jogo é montado automaticamente por `PrototypeBootstrap` (splash → carregamento → menu → corrida).
5. **Commit dos arquivos `.meta`** que o Unity gerar na primeira importação.

### Controles
| Entrada | Ação |
|---|---|
| Segurar toque / clique / espaço | subir (propulsor) |
| Soltar | cair |
| Toque livre no menu | começar a corrida |
| **K** | matar o personagem (teste; só no editor/dev build) |
| **V** | montar o próximo veículo (teste) |
| **R** | reiniciar (só no modo sem menus) |

Atalhos de teste nas Configurações (⚙, só no editor/dev build): **+5.000 porcas** e **+3 Fichas**.

### Salvamento
O perfil fica em `Application.persistentDataPath/perfil.json` (com `perfil.json.bak`).
Para começar do zero: ⚙ ▸ **Apagar todo o progresso** (toque duas vezes).

### Ajustar a física ao vivo
*Assets ▸ Create ▸ Propulsor ▸ Player Physics*, salve como
`Assets/Resources/PlayerPhysics.asset`. O bootstrap carrega esse asset; com
*Live Tuning* ligado no `RunDriver`, mudanças no Inspector valem durante o Play.

## Testes
| Conjunto | Como rodar | Onde |
|---|---|---|
| Lógica pura (EditMode) | `dotnet test unity/tools/CoreTests` **ou** Unity Test Runner ▸ EditMode | `Assets/Tests/EditMode` |
| Integração (PlayMode) | Unity Test Runner ▸ PlayMode | `Assets/Tests/PlayMode` |

## Estrutura
```
Assets/_Project/Scripts/
  Core/       C# puro, sem UnityEngine
    Physics/    PlayerMotor, FixedStepper
    Gameplay/   RunSimulation, trechos, validador, perigos, drones, veículos
    Events/     EventBus e eventos
    Flow/       GameFlow, ClawMachine, PopupQueue, SessionProfile (perfil salvo)
    Economy/    ItemCatalog, Wallet, Inventory, Store, RunModifiers (gadgets)
    Progress/   RunTracker, MissionManager, RankTable, AchievementManager
    Save/       MiniJson, SaveManager (JSON + .bak + migração)
    GameServices.cs  composição dos sistemas de meta-jogo
  Data/       ScriptableObjects (PlayerPhysicsDef)
  Gameplay/   RunDriver, FlowController, PlayerView, WorldView, PlayerInput, CameraFollow, CorridorView, DebugHud
  UI/         UIManager, UIFactory, UIScreen e telas (Splash, Carregamento, Menu, HUD, Pausa,
              Revive, Garra da Sorte, Resultados, Configurações, Oficina, Missões, Conquistas)
  Bootstrap/  PrototypeBootstrap (monta a cena em código)
Assets/Tests/ EditMode + PlayMode
```
