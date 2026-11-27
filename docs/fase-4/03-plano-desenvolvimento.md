# Fase 4 · 03 — Plano de desenvolvimento

| Fase | Entregas | Critério de pronto |
|---|---|---|
| **5 — Protótipo de movimentação** | `Core.PlayerMotor` + testes; cena com chão/teto, câmera, input (toque/mouse/espaço); arte placeholder | Testes de física verdes; subir/cair/limites conferidos em PlayMode; parâmetros expostos em `PlayerPhysicsDef` |
| **6 — Gameplay** | segmentos + pool; arcos estáticos/rotativos; feixes; drones com aviso; porcas/fichas; morte + ragdoll; 8 veículos com `IVehicle`; curva de dificuldade; validador de caminho livre | Cada perigo e veículo com teste PlayMode próprio; validador passa em 100 % dos segmentos |
| **7 — Menus** | UIManager, S01–S08, S16; HUD; transições | Navegação completa coberta por teste PlayMode de fluxo |
| **8 — Economia/inventário/progressão** | Economy, Store, Inventory, Gadgets, Missions, Ranks, Achievements, Garra da Sorte, SaveManager; S09, S10, S15 | Testes EditMode de todas as regras; salvar/carregar/migrar testados |
| **9 — Testes e refinamento** | executar matriz (doc 4·04); calibrar física por vídeo de referência (se fornecido); áudio; perfilamento | Matriz preenchida; 60 fps estáveis em aparelho médio |
| **10 — Entrega** | build Android (APK) + WebGL; README de execução; relatório final | Build reproduzível |

## Pré-requisitos e riscos
| Item | Situação | Mitigação |
|---|---|---|
| Editor Unity no ambiente | **Indisponível** neste contêiner (sem Unity/.NET) | Código escrito e organizado aqui; lógica `Core` em C# puro; compilação/testes executados por você no Unity (versão sugerida **Unity 6 LTS**) ou em CI com GameCI se houver licença |
| Evidência visual do original | Ausente | Usar estimativas [E]; calibrar quando houver vídeo |
| Push ao GitHub | Bloqueado (403) | Reconectar GitHub / instalar app |
| Arte e áudio | Não há assets | Placeholders geométricos próprios; trocar por arte original depois |
