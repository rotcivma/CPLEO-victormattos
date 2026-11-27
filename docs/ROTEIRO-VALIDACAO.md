# Roteiro de validação no Unity

Tempo estimado: cerca de 45 minutos. Para cada passo, marque ✅ ou ❌ e anote erros do Console.
Os resultados voltam para `docs/fase-4/04-plano-de-testes.md`.

## 0. Preparação
| # | Passo | Esperado | ✅/❌ |
|---|---|---|---|
| 0.1 | Unity Hub ▸ Add ▸ `unity/Propulsor`, versão **Unity 6 LTS** | projeto importa | |
| 0.2 | Aviso do Input System → **Yes** | editor reinicia | |
| 0.3 | Console sem erros de compilação | 0 erros | |
| 0.4 | Menu **Propulsor ▸ Preparar projeto** | cria `Assets/_Project/Scenes/Principal.unity` | |
| 0.5 | Commit dos `.meta` gerados | — | |

## 1. Testes automáticos (Window ▸ General ▸ Test Runner)
| # | Passo | Esperado | ✅/❌ |
|---|---|---|---|
| 1.1 | EditMode ▸ Run All | 177 aprovados | |
| 1.2 | PlayMode ▸ Run All | 13 aprovados | |

## 2. Fluxo de telas (Play na cena Principal)
| # | Passo | Esperado | Caso | ✅/❌ |
|---|---|---|---|---|
| 2.1 | Apertar Play | Splash → Carregamento → Menu, com música do menu | N-01, AU-02 | |
| 2.2 | Tocar na área livre | Nina sai correndo; placar aparece; música da corrida | N-02 | |
| 2.3 | Botão II | pausa, mostra missões; música mais baixa | N-03, AU-03 | |
| 2.4 | Continuar | contagem 3-2-1 e volta | N-03 | |
| 2.5 | Tirar o foco da janela | pausa sozinho | N-03 | |
| 2.6 | Morrer (K) sem fichas | Resultados com contadores animados | N-04 | |
| 2.7 | ⚙ ▸ +3 fichas, jogar e morrer | Garra da Sorte; PEGAR anima e mostra prêmio | N-04, G-01 | |
| 2.8 | Oficina ▸ Consumíveis ▸ comprar Segunda Chance; morrer | oferta de revive de 4 s; USAR volta à corrida | N-04 | |
| 2.9 | Resultados ▸ De novo | corrida nova do zero, progresso mantido | R-01 | |

## 3. Corrida
| # | Passo | Esperado | Caso | ✅/❌ |
|---|---|---|---|---|
| 3.1 | Segurar/soltar | sobe/cai com resposta imediata; chiado do propulsor | M-01, AU-05 | |
| 3.2 | Toques curtos | consegue pairar | M-01 | |
| 3.3 | Arcos, feixes, drones | aviso de drone pisca na borda direita; feixe avisa antes de disparar | C-01…C-03 | |
| 3.4 | Tecla V repetidas vezes | cada veículo com controle diferente (anotar sensação) | V-01…08 | |
| 3.5 | Bater com veículo | explosão, tremor, Nina pisca 1 s | C-04 | |
| 3.6 | Robôs no chão | caem com esbarrão e com o jato; nunca matam | W-01…03 | |
| 3.7 | Passar perto de arco | texto "raspão!" e som | NM-01 | |

## 4. Meta-jogo e salvamento
| # | Passo | Esperado | Caso | ✅/❌ |
|---|---|---|---|---|
| 4.1 | ⚙ ▸ +5.000 porcas; Oficina ▸ comprar e equipar propulsor/macacão | aparência muda na hora | L-01 | |
| 4.2 | Comprar sem saldo | botão desabilitado / "Porcas insuficientes" | L-02 | |
| 4.3 | Equipar 3º gadget sem o espaço extra | aviso "espaços cheios" | I-01 | |
| 4.4 | Missões ▸ Pular | debita 500 × estrelas | Q-03 | |
| 4.5 | Fechar e reabrir o jogo | saldo, itens, missões e volumes preservados | S-01 | |
| 4.6 | ⚙ ▸ Apagar progresso (2 toques) | volta ao zero | — | |

## 5. Desempenho e builds
| # | Passo | Esperado | Caso | ✅/❌ |
|---|---|---|---|---|
| 5.1 | Profiler durante 2 min de corrida | GC Alloc ≈ 0 B/quadro (exceto UI de texto) | O-06 | |
| 5.2 | Propulsor ▸ Build Android | APK em `unity/Propulsor/Builds/Android` | — | |
| 5.3 | Aparelho Android médio, 10 min | ≥ 58 fps p95 | D-01 | |
| 5.4 | Propulsor ▸ Build WebGL | build em `Builds/WebGL` abre no navegador | — | |
