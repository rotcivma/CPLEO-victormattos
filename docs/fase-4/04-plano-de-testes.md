# Fase 4 · 04 — Plano e matriz de testes

Tipos: **U** = unitário EditMode (C# puro) · **P** = PlayMode (Unity) · **M** = manual · **C** = comparação com a referência (vídeo).
Status: ⏳ não executado · ✅ passou (meio de execução, data) · ❌ falhou. Execução fora do Unity = `dotnet test unity/tools/CoreTests`.

| ID | Área | Caso | Tipo | Esperado | Status |
|---|---|---|---|---|---|
| F-01 | Física | Sem input a partir de y=3, cai e para no chão | U | y = chão, vy = 0, estado Grounded | ✅ U (dotnet, 2026-11-27) |
| F-02 | Física | Input contínuo sobe até o teto e para | U | y = teto, vy = 0 | ✅ U (dotnet, 2026-11-27) |
| F-03 | Física | vy nunca excede limites | U | vy ∈ [maxDown, maxUp] | ✅ U (dotnet, 2026-11-27) |
| F-04 | Física | Velocidade horizontal cresce e satura | U | vx(t) monotônica, ≤ maxSpeed | ✅ U (dotnet, 2026-11-27) |
| F-05 | Física | Integração independente do framerate (30/60/120 Hz) | U | diferença de posição < 1 % | ✅ U (dotnet, 2026-11-27) |
| F-06 | Física | Tempo chão→teto comparado à referência | C | ±15 % | ⏳ |
| M-01 | Movimento | Toques curtos permitem pairar | M | oscilação estável ±0,5 u | ⏳ |
| C-01 | Colisão | Arco estático mata a pé | P | `PlayerDied(Arc)` | ⏳ |
| C-02 | Colisão | Arco rotativo: colisor acompanha rotação | P | dano só na área do arco | ⏳ |
| C-03 | Colisão | Feixe em Warning não causa dano; em Firing causa | P | | ⏳ |
| C-04 | Colisão | Perigo com veículo destrói veículo, não mata | P | `VehicleDestroyed`, jogador vivo, invulnerável 1 s | ⏳ |
| C-05 | Colisão | Guindaste Blindado aguenta 3 drones | P | destruído no 3º | ⏳ |
| C-06 | Colisão | Coleta e dano no mesmo quadro | P | coleta contabilizada | ⏳ |
| O-01 | Geração | Mesma seed ⇒ mesma sequência | U | idêntica | ⏳ |
| O-02 | Geração | Sem repetição de segmento nas últimas 3 | U | | ⏳ |
| O-03 | Geração | Validador: todo segmento tem caminho livre | U | 100 % | ⏳ |
| O-04 | Drones | Não lançados durante feixe ativo nem na Moto-Trilho | U/P | 0 lançamentos | ⏳ |
| O-05 | Drones | `t_track` decresce com distância | U | 1,4 s @0 m → 0,6 s @3000 m | ⏳ |
| O-06 | Desempenho | Sem alocação GC por quadro na corrida | P | 0 B/frame após aquecimento | ⏳ |
| K-01 | Moedas | Porca coletada soma 1 à run wallet | U | | ⏳ |
| K-02 | Moedas | Dobro ×2 aplica só nas porcas da corrida | U | | ⏳ |
| K-03 | Moedas | Ímã atrai dentro do raio | P | | ⏳ |
| V-01..08 | Veículos | Controle de cada veículo conforme doc 3·01 §3 | P/M | | ⏳ |
| N-01 | Navegação | Splash→Carregamento→Menu | P | | ⏳ |
| N-02 | Navegação | Toque livre no menu inicia corrida | P | | ⏳ |
| N-03 | Navegação | Pausa/continuar com contagem; perda de foco pausa | P | | ⏳ |
| N-04 | Navegação | Morte: revive → garra → resultados conforme inventário | P | caminhos do diagrama 3·01 §1 | ⏳ |
| L-01 | Loja | Compra com saldo suficiente | U | debita, concede, salva | ⏳ |
| L-02 | Loja | Saldo insuficiente | U | `SaldoInsuficiente`, nada muda | ⏳ |
| L-03 | Loja | Comprar item já possuído | U | `JaPossui` | ⏳ |
| I-01 | Inventário | Equipar 3º gadget sem slot extra | U | recusado | ⏳ |
| I-02 | Inventário | Consumir Segunda Chance decrementa | U | | ⏳ |
| G-01 | Garra | 1 ficha = 1 tentativa; tabela por pesos (10⁵ sorteios) | U | frequências ±1 % | ⏳ |
| G-02 | Garra | Troca de fichas = 100 porcas cada | U | | ⏳ |
| Q-01 | Missões | Sempre 3 ativas após RunEnded | U | | ⏳ |
| Q-02 | Missões | Escopo Run zera a cada corrida; Lifetime acumula | U | | ⏳ |
| Q-03 | Missões | Pular custa 500 × estrelas | U | | ⏳ |
| Q-04 | Patente | Estrelas suficientes ⇒ RankUp + recompensa | U | | ⏳ |
| Q-05 | Patente | Nível 15 ⇒ prestígio reinicia ciclo | U | | ⏳ |
| A-01 | Conquistas | Desbloqueio idempotente | U | 1 evento só | ⏳ |
| S-01 | Save | Salvar/carregar ida e volta | U | igual | ⏳ |
| S-02 | Save | Arquivo corrompido ⇒ usa .bak | U | | ⏳ |
| S-03 | Save | Migração v0→v1 | U | | ⏳ |
| R-01 | Reinício | "De novo" reseta estado de corrida e mantém progresso | P | | ⏳ |
| D-01 | Desempenho | 60 fps em aparelho Android médio por 10 min | M | ≥ 58 fps p95 | ⏳ |

Regra do projeto: **nenhuma funcionalidade é declarada reproduzida sem o teste correspondente executado e registrado aqui.**
