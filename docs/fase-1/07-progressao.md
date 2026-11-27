# 07 — Missões, patentes, conquistas e recordes

## 1. Missões
| Regra | Conf. |
|---|---|
| 3 missões ativas simultaneamente | [C] (S1, S13) |
| Cada missão vale 1 a 3 estrelas; o valor não indica dificuldade | [C] (S1) |
| Concluir libera a vaga para nova missão | [G] |
| Progresso pode ser acumulado dentro de uma corrida ou entre corridas, conforme a missão | [G] |
| É possível "comprar" a conclusão: 500 moedas por estrela | [C] (via busca, S1) |
| Notificação na tela ao concluir durante a corrida | [C] (S14) |

### Tipos de missão (taxonomia proposta)
Exemplos típicos [G], a validar: voar X m sem tocar o chão; correr X m pelo
chão; coletar X moedas em uma corrida; derrubar X cientistas; passar raspando
por X zappers/mísseis; usar veículo X; morrer de forma específica; atingir
distância X sem coletar moedas.

Modelo de dados proposto: `MissionDef { id, tipo, alvo, escopo(corrida|acumulado), estrelas, condiçõesExtras }`.

## 2. Patentes (níveis)
| Regra | Conf. |
|---|---|
| Estrelas preenchem a barra de patente; cada patente exige N estrelas | [C] (S12, S13) |
| Subir de patente rende moedas | [C] (S1, S13) |
| Ciclo de 15 níveis; nível 15 tem o nome do protagonista | [C] (S13) |
| Últimas patentes do ciclo: Hotshot, Wizard, Ninja, Superstar, (protagonista) | [C*] (S13, versão Xbox) |
| Estrelas por ciclo | [C*] 63 (S12, FAQ oficial) vs. 105 (S13) |
| Ao completar o ciclo: troca-se por um **badge** (prestígio) e reinicia com novas missões; total 125 badges | [C] (S1) |
| Alternativa: continuar jogando sem missões | [C] (S1) |

## 3. Conquistas
| Regra | Conf. |
|---|---|
| Conquistas por plataforma (Game Center / Xbox / Steam / Play) | [C] existência (S13, S14) |
| Exemplos: perder a Final Spin 100 vezes no total; ganhar 3 Atom Blasts | [C] (busca: Exophase/RetroAchievements) |
| Lista e quantidade completas | lacuna |

## 4. Recordes e estatísticas
| Item | Conf. |
|---|---|
| Melhor distância (recorde) | [G] |
| Estatísticas acumuladas (distância total, moedas totais, mortes por tipo...) | [G] — necessárias para missões acumuladas |
| Marcador de recorde no cenário durante a corrida | [G] |
