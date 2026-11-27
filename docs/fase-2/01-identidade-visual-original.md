# Fase 2 · 01 — Identidade visual própria e guia de interface

A implementação **não copia** a aparência do jogo de referência. Este guia
define uma identidade **original** que cumpre as mesmas funções de UX
observadas (leitura rápida, botão único, menus sobre o cenário).

## 1. Nome e personagem (provisórios)

| Item | Definição |
|---|---|
| Nome de trabalho | **PROPULSOR** (codinome do projeto; nome comercial a definir) |
| Protagonista | **Nina Faísca** — mecânica de oficina, macacão verde-água, óculos de solda na testa, mochila-propulsor improvisada feita de um extintor |
| Cenário | **Fábrica de Brinquedos Automatizada "Engrenópolis"** — esteiras, braços robóticos, caixas, galpões; robôs operários (não letais) no lugar dos cientistas |
| Tom | Cartunesco, cores saturadas, humor visual leve |

Mapeamento funcional → identidade própria:

| Função no original | Equivalente original |
|---|---|
| Laboratório | Fábrica Engrenópolis (seções: Montagem, Pintura, Depósito, Telhado, Esgoto) |
| Cientistas derrubáveis | Robôs operários "Parafusinhos" |
| Zapper | **Arco de faíscas** entre duas bobinas |
| Laser | **Feixe de corte industrial** saindo de trilhos nas laterais |
| Míssil | **Drone-foguete de entrega** desgovernado |
| Moeda | **Porca dourada** (moeda "Porcas") |
| Spin Token | **Ficha de fliperama** ("Ficha") |
| Final Spin | **Garra da Sorte** (máquina de garra no fim da corrida) — mesma função: 1 ficha = 1 tentativa, prêmios aleatórios |
| Stash | **Oficina** |

## 2. Tokens de design

### Paleta
| Token | Hex | Uso |
|---|---|---|
| `--c-bg-deep` | `#1B1F3B` | fundo de menus, sombras |
| `--c-bg-panel` | `#2B3163` | painéis |
| `--c-primary` | `#2EC4B6` | botões principais, protagonista |
| `--c-accent` | `#FF9F1C` | destaque, preços, CTA "Jogar" |
| `--c-gold` | `#FFD23F` | moeda (porcas), estrelas |
| `--c-danger` | `#E71D36` | perigos, avisos de drone |
| `--c-electric` | `#7DF9FF` | arcos de faísca |
| `--c-text` | `#F6F7FB` | texto principal |
| `--c-text-dim` | `#A8AEC9` | texto secundário |
| `--c-ok` | `#6BCB77` | missão concluída |

Regra: todo perigo usa vermelho ou ciano elétrico + brilho; nada não letal usa essas cores (legibilidade em alta velocidade).

### Tipografia (fontes livres, OFL)
| Papel | Fonte | Tamanho base (ref. 1080 px de altura) |
|---|---|---|
| Títulos / números de HUD | **Lilita One** | 64–96 px |
| Botões | **Lilita One** | 44 px |
| Texto corrido | **Nunito Bold** | 32 px |
| Rótulos pequenos | **Nunito SemiBold** | 24 px |

Números de HUD com contorno 4 px `--c-bg-deep` para leitura sobre qualquer fundo.

### Grade e medidas
- Resolução de referência: **1920×1080** (16:9), `CanvasScaler` *Scale With Screen Size*, *match* 0,5.
- Margem segura: 4 % da largura (≈ 77 px) e 5 % da altura (≈ 54 px), respeitando `Screen.safeArea` (notch).
- Unidade de espaçamento: **8 px** (múltiplos 8/16/24/32/48).
- Área mínima de toque: **96×96 px** (≈ 9 mm em celular).
- Raio de borda: 24 px em painéis, 999 px (pílula) em botões.

### Estados de botão
| Estado | Visual |
|---|---|
| Normal | preenchimento sólido, sombra inferior 8 px |
| Pressionado | desloca 6 px para baixo, sombra 2 px, escala 0,96 |
| Desabilitado | 40 % de opacidade, sem sombra |
| Destaque (novidade) | pulso de escala 1,00↔1,06 a 1,2 Hz + selo "!" |
| Sem saldo (loja) | preço em `--c-danger`, toque mostra "Porcas insuficientes" |

### Animação (padrões)
| Uso | Duração | Curva |
|---|---|---|
| Entrada de painel | 250 ms | ease-out-back (overshoot 1,2) |
| Saída de painel | 180 ms | ease-in |
| Contadores (moedas somando) | 600–1200 ms | ease-out |
| Pop-up de missão concluída | 300 ms entra, 2 s visível, 300 ms sai | |
| Fade entre cenas | 300 ms | linear |

## 3. Responsividade
| Proporção | Regra |
|---|---|
| 16:9 (referência) | layout nominal |
| 19,5:9 / 20:9 (celulares longos) | âncoras laterais; HUD gruda nas bordas da safe area; gameplay mostra mais mundo à frente (a câmera mantém a altura fixa) |
| 4:3 / 16:10 (tablets) | altura jogável fixa; a largura visível diminui → personagem a 22 % da largura continua, mas avisos de drone ganham +0,2 s para compensar menos visão à frente |
| Retrato | **não suportado** (orientação travada em paisagem) |

A **altura do mundo visível é constante** (7 u jogáveis + chão/teto) em todas as proporções, para que a física e a dificuldade sejam idênticas.
