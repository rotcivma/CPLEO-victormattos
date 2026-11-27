# 04 — Obstáculos e sistemas de perigo

Todos os perigos abaixo matam instantaneamente o personagem a pé [C] (S1).
Em veículo, destroem o veículo [C] (S2).

## 4.1 Zappers (barreiras elétricas)

| Atributo | Descrição | Conf. |
|---|---|---|
| Aparência | Barra/feixe elétrico entre dois emissores esféricos | [C] (S1) |
| Variantes | **Estático** (amarelo) e **rotativo** (laranja) | [C] (S1) |
| Orientações | Horizontal, vertical, diagonal; comprimentos variados | [G] |
| Surgimento | Parte dos padrões/segmentos pré-desenhados, entra pela direita com o cenário | [G] |
| Movimento | Estático: nenhum (move só com o mundo). Rotativo: gira em torno do centro a velocidade constante | [C] variantes / [G] detalhe |
| Esquiva | Sempre há um caminho livre | [H] — regra de design obrigatória na implementação |

## 4.2 Lasers

| Atributo | Descrição | Conf. |
|---|---|---|
| Aparência | Feixes horizontais de borda a borda, disparados por emissores nas laterais da tela | [C] (S1) "high-intensity laser beams" / [G] layout |
| Ciclo | Aviso (linha fina/piscando) → carga → disparo por ~1–2 s → desliga | [G] |
| Padrões | Várias linhas simultâneas, deixando faixas seguras | [G] |
| Interação com mísseis | Mísseis não aparecem enquanto o jogador passa por lasers | [C] (S2) |
| Rolagem | Emissores ficam presos à tela (não rolam com o mundo) | [G] |

## 4.3 Mísseis

| Atributo | Descrição | Conf. |
|---|---|---|
| Entrada | Pela borda direita da tela | [C] (S2) |
| Aviso | Indicador na borda direita na altura do jogador, ~1–2 s antes | [C] (S2) |
| Tipos | Simples (reto); "teleguiado" — aviso segue a altura do jogador e trava antes do disparo | [C] simples / [G] rastreamento do aviso |
| Rajadas | Às vezes 5+ mísseis em menos de 2 s | [C] (S2) |
| Tempo de reação | Diminui com a distância percorrida | [C] (S2) |
| Bloqueios | Não surgem durante lasers nem com o veículo Bad As Hog | [C] (S2) |
| Contra veículo | Destrói qualquer veículo em 1 acerto, exceto S.A.M. (3) | [C] (S2) |

## 4.4 Outros elementos
| Elemento | Perigo? | Conf. |
|---|---|---|
| Cientistas/NPCs | Não; decoração interativa (derrubáveis) | [C] (S5) |
| Ambientes/seções do laboratório (corredores, salas, áreas externas) | Não; mudam o fundo | [G] |

## 4.5 Progressão de dificuldade (hipótese de modelo)

Não há valores públicos. Modelo proposto [H]/[E]:

| Faixa (m) | Velocidade | Padrões | Mísseis | Lasers |
|---|---|---|---|---|
| 0–500 | 8–10 u/s | zappers estáticos simples | raros, aviso 1,8 s | raros |
| 500–1500 | 10–13 | + rotativos, + diagonais | moderados, 1,5 s | ocasionais |
| 1500–3000 | 13–16 | combinações densas | rajadas de 2–3, 1,2 s | frequentes |
| 3000+ | 16–20 | máxima densidade | rajadas 5+, 1,0 s | frequentes |

Implementação: gerador por **segmentos pré-desenhados** com peso por faixa de
dificuldade + validador de caminho livre.
