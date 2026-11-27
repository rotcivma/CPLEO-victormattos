# Fase 2 · 02 — Especificação detalhada das telas (implementação própria)

Cada tela abaixo cumpre a **função** observada na referência (doc 1·02) com
layout e visual **próprios**. Coordenadas em % da tela (x a partir da
esquerda, y a partir do topo), referência 1920×1080. "SA" = safe area.

Coluna **Ref.** indica a tela funcional equivalente do inventário da Fase 1.

---

## S01 · Splash (Ref. T01)
- **Objetivo:** exibir logo do estúdio/projeto.
- **Hierarquia:** `Canvas > BG (sólido --c-bg-deep) > Logo (centro, 30 % largura)`.
- **Comportamento:** fade-in 300 ms, permanece 1,2 s, fade-out 300 ms → S02. Toque pula.
- **Áudio:** "clink" de chave inglesa no início.

## S02 · Carregamento (Ref. T02)
```
┌──────────────────────────────────────────────┐
│                                              │
│            [ Nina girando chave ]            │  ← 50 %,45 %  ícone 12 % alt.
│                                              │
│        ▓▓▓▓▓▓▓▓▓▓▓░░░░░░░░░░░░  62 %         │  ← barra 40 % larg., y 65 %
│     "Dica: segure para subir, solte p/ cair" │  ← y 75 %, Nunito 28 px
└──────────────────────────────────────────────┘
```
- Carrega `SaveData`, catálogos (ScriptableObjects) e cena principal (assíncrono).
- Duração mínima 0,8 s (evita piscar). Sai com fade → S03.

## S03 · Menu principal (Ref. T03)
Conceito funcional preservado: **o menu é a própria cena de jogo pausada**;
tocar na área livre inicia a corrida sem tela intermediária.
```
┌──────────────────────────────────────────────────────────┐
│ ⚙                                      🔩 12.340   🎟 3   │  y 6 %  (SA)
│                                                          │
│   P R O P U L S O R                        [OFICINA]     │  logo 8 %,20 %; botão 88 %,30 %
│   Recorde: 2.415 m                         [MISSÕES]     │                   88 %,45 %
│                                            [CONQUIST.]   │                   88 %,60 %
│   (Nina parada ao lado da porta da fábrica)              │
│                                                          │
│           ▶ TOQUE PARA COMEÇAR  (pulsando)               │  50 %,82 %
└──────────────────────────────────────────────────────────┘
```
| Elemento | Âncora | Tamanho | Ação |
|---|---|---|---|
| Config. ⚙ | sup.-esq. | 96×96 | abre S16 (modal) |
| Saldo porcas/fichas | sup.-dir. | altura 64 | toque → Oficina/aba Porcas |
| Logo | esq. | 36 % larg. | — |
| Recorde | sob logo | Nunito 36 | — |
| Botões laterais | dir., coluna, gap 24 | 320×110 cada | abrem S10, S09, S15 |
| Prompt iniciar | centro inferior | Lilita 56 | — |
| Área de toque livre | resto da tela | — | **inicia corrida**: Nina corre e atravessa a porta (0,8 s), UI do menu desliza para fora (180 ms) e HUD entra |

## S04 · Gameplay / HUD (Ref. T04)
```
┌──────────────────────────────────────────────────────────┐
│ 1.284 m   (melhor 2.415)                         [ II ]  │ y 6 %
│ 🔩 187                                                    │ y 13 %
│                                                     ⚠ ◀  │ aviso de drone na borda dir., y = altura alvo
│      ✈Nina                                               │ x 22 %
│                    ⚡━━━━━━⚡                              │
│▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀│ chão
│   [✓ Missão: Colete 500 porcas ★★]   (pop-up, y 88 %)     │
└──────────────────────────────────────────────────────────┘
```
| Elemento | Regras |
|---|---|
| Distância | atualiza a cada quadro, inteiro, separador de milhar; pisca dourado ao passar o recorde |
| Porcas | incrementa com "pop" escala 1,15 por 80 ms |
| Pausa | 96×96, sup.-dir.; também ativada ao perder foco do app |
| Aviso de drone | ícone 80 px na borda direita; fase *rastreando* (segue y do jogador, pisca 4 Hz) → fase *travado* (fixo, pisca 10 Hz, 0,4 s) → drone entra |
| Marcador de recorde | placa no cenário na distância do recorde |
| Pop-up missão | máximo 1 por vez, fila |

## S05 · Pausa (Ref. T05)
Painel central 60 %×70 %, fundo escurecido 60 %.
```
        ┌──────────── PAUSADO ────────────┐
        │  Missões                         │
        │  ☐ Voe 300 m sem tocar o chão ★  │
        │  ☑ Derrube 10 Parafusinhos ★★    │
        │  ☐ Use a Moto-Trilho 2× ★★★      │
        │                                  │
        │  [▶ CONTINUAR]                    │ primário, --c-primary
        │  [🔊] [🎵]          [✖ DESISTIR] │ desistir → confirmação
        └──────────────────────────────────┘
```
- `Time.timeScale = 0`; ao continuar, contagem 3-2-1 (0,9 s, tempo real) antes de retomar.
- Desistir conta como morte normal (vai para S07/S06), sem revive.

## S06 · Resultados (Ref. T06)
```
┌──────────────────────────────────────────────────────────┐
│                    1.284 m                               │ Lilita 120, centro
│               ★ NOVO RECORDE! ★ (se aplicável)           │
│  🔩 Porcas da corrida ......... 187                       │
│  🔩 Bônus de missões .......... 250                       │
│  🔩 Total → saldo ......... 12.777  (contador animado)    │
│  Missões: ☑ ☑ ☐   Patente ▓▓▓▓░░ Nv 4 "Aprendiz"         │
│                                                          │
│   [OFICINA]          [▶ DE NOVO]          [MISSÕES]      │ y 85 %
└──────────────────────────────────────────────────────────┘
```
- Sequência de animação: distância (0,6 s) → linhas de porcas (0,4 s cada) → estrelas voam para a barra de patente → se subir de nível, modal de recompensa.
- Toque acelera a animação (pula para valores finais).

## S07 · Garra da Sorte (Ref. T07 — Final Spin)
```
┌──────────────────────────────────────────────────────────┐
│  Fichas: 🎟 3                                             │
│        ┌────────────────────────────┐                    │
│        │  🎁   💰   ❤   🚀   💰   🎟 │ ← prêmios na esteira│
│        │         ╲ garra ╱            │                    │
│        └────────────────────────────┘                    │
│  [ PEGAR! (1 🎟) ]        [ Trocar fichas: +100 🔩 cada ] │
└──────────────────────────────────────────────────────────┘
```
- 1 ficha = 1 tentativa; resultado é sorteado **antes** da animação (determinístico pelo RNG), a animação só encena.
- Tabela de prêmios: ver doc 4·02.
- "Segunda Chance" ganha aqui → volta imediatamente à corrida no ponto da morte com 2 s de invulnerabilidade.
- Sem fichas → pula direto para S06.

## S08 · Oferta de revive (Ref. T08)
Modal 50 %×40 %: "Usar Segunda Chance? (x1)" com anel de contagem de 4 s; [USAR] / [NÃO]. Só aparece se o inventário tem o consumível.

## S09 · Missões (Ref. T09)
```
┌ ← ─────────────── MISSÕES ─────────────── 🔩 12.340 ┐
│ Patente Nv 4 "Aprendiz"   ▓▓▓▓▓▓░░░░  7/12 ★          │
│ ┌────────────────────────────────────────────────┐   │
│ │ ★     Voe 300 m sem tocar o chão  120/300      │ [PULAR 500🔩]
│ │ ★★    Derrube 10 Parafusinhos      ✓ concluída │
│ │ ★★★   Use a Moto-Trilho 2×          1/2        │ [PULAR 1.500🔩]
│ └────────────────────────────────────────────────┘   │
│ Nv 15 → [TROCAR POR SELO DE PRESTÍGIO]               │
└──────────────────────────────────────────────────────┘
```

## S10 · Oficina (loja + inventário) (Ref. T10–T14)
Layout mestre-detalhe:
```
┌ ← ────────────────── OFICINA ─────────── 🔩 12.340 🎟 3 ┐
│ [Propulsores][Veículos][Gadgets][Consumíveis][Visual][Especiais] │ abas, y 14 %
│ ┌──────────────┐ ┌───────────────────────────────────┐ │
│ │ grade 3×N    │ │  Preview grande do item (anim.)    │ │
│ │ cartões      │ │  Nome / descrição / efeito          │ │
│ │ 260×300      │ │  [COMPRAR 2.000🔩] ou [EQUIPAR] ou  │ │
│ │ (rolagem)    │ │  "Equipado ✓"                       │ │
│ └──────────────┘ └───────────────────────────────────┘ │
│ Loadout: [gadget 1] [gadget 2] [🔒 slot 3]              │ só na aba Gadgets
└──────────────────────────────────────────────────────────┘
```
| Aba | Conteúdo | Regra |
|---|---|---|
| Propulsores (T11) | modelos de mochila-propulsor | 1 equipado; cosmético (muda partícula do jato e som) |
| Veículos (T13) | skins por veículo | 1 skin ativa por veículo |
| Gadgets (T12) | gadgets passivos | máx. 2 equipados (3 com slot extra) |
| Consumíveis | Arrancada, Segunda Chance, Pacote de fichas | quantidade empilhável |
| Visual (T14) | macacões, chapéus, óculos | 1 por espaço (corpo, cabeça) |
| Especiais / Eventos (T17) | itens sazonais com prazo | mostram contagem regressiva |

Cartão de item: ícone, nome, preço ou selo (NOVO / EQUIPADO / ✓ COMPRADO). Compra confirma com modal se preço > 5.000.

## S15 · Conquistas (Ref. T15)
Lista vertical; cada linha: ícone (cinza se bloqueado), nome, descrição, barra de progresso, recompensa. Filtro "Todas / Concluídas / Pendentes".

## S16 · Configurações (Ref. T16)
Modal: volume música (slider), volume efeitos, vibração (toggle), idioma (PT-BR/EN), "Apagar progresso" (confirmação dupla), créditos/licenças de fontes e sons.

## S17 · Evento especial (Ref. T17)
Fora do MVP. Reservado: banner no menu principal + aba "Especiais" na Oficina, dirigido por `EventDef` com data de início/fim.

---

## Transições (resumo)
| De → Para | Transição |
|---|---|
| S01→S02→S03 | fade 300 ms |
| S03→S04 | sem corte: UI desliza, câmera começa a seguir |
| S04→S05 | painel ease-out-back 250 ms |
| morte→S08/S07/S06 | câmera desacelera 0,5 s após Nina parar, painel sobe de baixo |
| S06→S04 | fade rápido 200 ms, reinicia na porta da fábrica |
| menus ↔ submenus | slide horizontal 250 ms |
