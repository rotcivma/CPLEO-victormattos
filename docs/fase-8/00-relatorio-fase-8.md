# Relatório da FASE 8 — Economia, inventário e progressão

**Data:** 2026-11-27 · **Código:** `unity/Propulsor` (`Core/Economy`, `Core/Progress`, `Core/Save`, `Core/GameServices.cs`)

## 1. Arquitetura
```
GameServices (Core, C# puro)
 ├─ SessionProfile ── o perfil salvo (carteira, inventário, progressão, estatísticas, configurações)
 ├─ Wallet ───────── créditos/débitos com motivo; nunca negativo; BalanceChanged
 ├─ Inventory ────── posse, consumíveis, equipados por espaço, gadgets (2 ou 3 espaços)
 ├─ Store ────────── compra atômica: valida → debita → concede
 ├─ RunModifiers ─── gadgets + bônus pendentes → RunConfig da próxima corrida
 ├─ RunTracker ───── mede a corrida pelos eventos (porcas, distância, voo, chão, veículos, feixes, mortes...)
 ├─ MissionManager ─ 3 missões, avisos na hora, estrelas → patentes no fim da corrida, pular, prestígio
 ├─ AchievementManager ─ 18 conquistas, idempotentes, com recompensa
 └─ SaveManager ──── JSON próprio (MiniJson), escrita atômica, .bak, migração v0→v1
FlowController (Unity) usa GameServices: monta cada corrida, encerra (EndRun) e salva.
```

## 2. Conteúdo próprio criado
| Sistema | Conteúdo |
|---|---|
| Oficina | 30 itens: 4 propulsores (cosméticos), 4 macacões + 2 acessórios, 6 gadgets, 3 consumíveis, terceiro espaço de gadget, 8 pinturas de veículo |
| Gadgets | Ímã de Porcas (raio 4 m), Cinto de Lastro (queda +30%), Botas de Mola (quica ao pousar), Robô Coletor (pega porcas baixas à frente), Ficha Sortuda (última ficha sempre ganha), Para-choque (2 s de proteção ao perder veículo) |
| Consumíveis | Segunda Chance (1.500), Arrancada 750 m (1.000; botão no HUD nos primeiros 50 m), Pacote de 3 fichas (900) |
| Missões | 34 missões (porcas, distância, voo, chão, sem porcas, fichas, veículos, cada veículo, feixes, mortes, garra, arrancada), por corrida ou no total, 1–3 estrelas, liberadas por patente |
| Patentes | 15 níveis com nomes próprios ("Estagiária" → "Lenda da Fábrica" → "Nina"); recompensa em porcas; no 15, troca por selo de prestígio |
| Conquistas | 18 (corridas, distância, recordes, porcas, veículos, feixes, garra, mortes por tipo, missões, compras, prestígio) |

## 3. Telas
| Tela | O que faz |
|---|---|
| S10 Oficina | 6 abas, grade paginada, detalhe com Comprar/Equipar/Retirar, status (preço, "Porcas insuficientes", "Limite atingido"), fileira dos gadgets equipados |
| S09 Missões | patente e barra de estrelas, 3 missões com progresso, Pular (500 × estrelas), botão de prestígio |
| S15 Conquistas | lista paginada com progresso e recompensa |
| Pausa | agora lista as missões com progresso ao vivo |
| Resultados | linha de recompensas, patente, missões concluídas, subidas de patente e conquistas; botões Oficina/Missões/De novo/Menu |
| HUD | botão **ARRANCADA (xN)** no início da corrida; avisos de missão e conquista |
| Configurações | volumes/vibração salvos; **Apagar todo o progresso** (dupla confirmação); atalhos de teste só no editor |

A aparência equipada (macacão, acessório, cor e tamanho do jato, pintura dourada dos veículos) é aplicada à personagem em tempo real.

## 4. Testes
| Conjunto | Executado | Resultado |
|---|---|---|
| Lógica — **161 testes** (52 novos) | Sim, `dotnet test` | **161/161** |
| Sintaxe C# de todos os scripts | Sim (Roslyn) | 0 erros |
| PlayMode (+1 novo: compra e equipa na Oficina; testes de menu agora usam salvamento em memória) | Não (sem Unity) | pendente |

Matriz atualizada: L-01…L-03, I-01, I-02, K-02, Q-01…Q-05, A-01, S-01…S-03 ✅; R-01 ✅ na lógica.

Destaques dos testes novos:
- ida e volta do JSON;
- arquivo corrompido → cópia de segurança → perfil novo;
- migração v0→v1 e versão futura recusada;
- valores inválidos saneados;
- salvamento em disco real;
- compra sem saldo e limite de consumíveis;
- terceiro espaço de gadget;
- cada gadget alterando a corrida;
- arrancada atravessando parede invulnerável;
- missões por corrida × no total;
- subida de patente com recompensa e prestígio;
- conquistas desbloqueadas uma única vez;
- fim de corrida completo com salvamento.

**Ajuste feito durante os testes:** um teste de missões falhou porque o banco de teste tinha só 3 missões e a missão concluída voltava como nova, que é o comportamento correto quando o banco se esgota. O teste foi corrigido para verificar a conclusão registrada e o progresso zerado. A regra não mudou.

## 5. Desvios e decisões
| Item | Decisão | Motivo |
|---|---|---|
| Terceiro espaço de gadget | comprado com porcas (25.000) | o projeto não terá compras com dinheiro real |
| Estrelas de missão pulada | contam no fim da próxima corrida | mantém uma única regra de conversão de estrelas |
| Fichas | só pela corrida, Garra (+3) ou Oficina (pacote) | — |
| JSON | implementação própria (MiniJson) | sem dependência de pacotes, igual no .NET e no Unity |
| Salvamento em nuvem | fora do escopo | prioridade local (doc 1·06) |

## 6. Pendências → Fase 9
- Validação no Unity (compilação, PlayMode, sensação, desempenho em aparelho).
- Áudio (AudioManager, música por estado, efeitos), fontes OFL do guia e polimento visual (explosões, tremor de câmera).
- Robôs operários derrubáveis e "raspão" (mecânicas observadas na referência ainda não implementadas).
- Eventos especiais (S17).
