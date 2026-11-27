# PROPULSOR — protótipo Unity (Fases 5–10)

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

## Builds
Menu **Propulsor** no editor:
- *Preparar projeto*: cria a cena principal e ajusta orientação e nome.
- *Build Android (APK)* e *Build WebGL*: saída em `unity/Propulsor/Builds/`.

Pela linha de comando:
```
Unity -batchmode -quit -projectPath unity/Propulsor -executeMethod Propulsor.EditorTools.BuildScript.BuildAndroid
```
Roteiro completo de validação: `docs/ROTEIRO-VALIDACAO.md`.

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
    Audio/      AudioDirector (o que tocar e quando)
    GameServices.cs  composição dos sistemas de meta-jogo
  Data/       ScriptableObjects (PlayerPhysicsDef)
  Gameplay/   RunDriver, FlowController, AudioManager, ProceduralSynth, PlayerView, WorldView, PlayerInput, CameraFollow, CorridorView, DebugHud
  UI/         UIManager, UIFactory, UIScreen e telas (Splash, Carregamento, Menu, HUD, Pausa,
              Revive, Garra da Sorte, Resultados, Configurações, Oficina, Missões, Conquistas)
  Bootstrap/  PrototypeBootstrap (monta a cena em código)
Assets/Tests/ EditMode + PlayMode
```

## Créditos de terceiros
| Recurso | Licença | Arquivos |
|---|---|---|
| Fonte Lilita One (Juan Montoreano) | SIL Open Font License 1.1 | `Assets/_Project/Resources/Fonts/LilitaOne-*` |
| Fonte Nunito (The Nunito Project Authors) | SIL Open Font License 1.1 | `Assets/_Project/Resources/Fonts/Nunito-*` |

Todo o resto (código, arte geométrica, sons e músicas sintetizados) é original do projeto.
