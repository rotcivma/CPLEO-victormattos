# 06 — Economia, Final Spin, loja e equipamentos

## 1. Moedas
| Regra | Conf. |
|---|---|
| Moeda comum coletada em formações (linhas, arcos, formas/letras) ao longo do percurso | [G] formações / [C] existência |
| Moedas coletadas na corrida somam ao saldo ao fim da partida | [G] |
| Prêmio "Double Coins" dobra as moedas da próxima corrida (não as da Final Spin) | [C] (S6, S9) |
| Missões e subida de patente rendem moedas | [C] (S12, S13) |
| Compra de moedas com dinheiro real (IAP) | [G] |

## 2. Spin Tokens e Final Spin
| Regra | Conf. |
|---|---|
| Spin Tokens aparecem durante a corrida; 1 token = 1 giro | [C] (S1, S6) |
| Final Spin = caça-níqueis no fim da partida; só aparece se houver tokens | [C] (S6) |
| Resultado aleatório | [C] (S6) |
| Tokens não usados podem ser trocados por moedas | [C*] 100 moedas (S1) vs. 50 (S8, versão antiga) |
| Gadget "Lucky Last": último token sempre ganha | [C*] (S5, S15) — fontes divergem entre "último" e "todos" |

### Prêmios conhecidos
| Prêmio | Efeito | Conf. |
|---|---|---|
| Moedas (bônus / prêmio / jackpot) | 100 / 500 / 1 000 (outra fonte: 200 / 1 000) | [C*] |
| Second Chance | Revive o jogador que acabou de morrer | [C] |
| Head Start | ~750 m de avanço ativável no início da próxima corrida; 1 por corrida | [C] |
| Small / Big / Atom Blast | Avanço explosivo; Atom Blast ~800 m | [C*] |
| Double Coins | ×2 moedas na próxima corrida | [C] |
| Extra Spins | +3 tokens | [C] |

## 3. Loja ("Stash") — categorias
| Categoria | Conteúdo | Conf. |
|---|---|---|
| Jetpacks | Modelos alternativos (cosméticos: mudam o escapamento — balas, bolhas, chamas etc.) | [G] |
| Veículos | Upgrades/skins | [C] (S1) |
| Gadgets | Modificadores passivos (abaixo) | [C] (S1, S5) |
| Utilitários | Consumíveis de uso limitado (ex.: head starts, revives, tokens) | [C] existência (S1) / [G] itens |
| Visual (roupas/acessórios) | Cosméticos | [G] |
| Pular missão | 500 moedas por estrela | [C] (S1/S12 via busca) |

Preços individuais **não foram confirmados** — ver doc 08.

## 4. Gadgets
| Regra | Conf. |
|---|---|
| 2 slots por padrão; 3º slot comprável (IAP) | [C] (S5) |

| Gadget | Efeito | Conf. |
|---|---|---|
| Coin Magnet | Atrai moedas ao redor do personagem | [C] (S10, S11) |
| Gravity Belt | Queda mais rápida | [C] (S10) |
| Air Barrys | Pulos ao tocar o chão (~3× a altura) | [C] (S11) |
| Flash (cão-robô) | Segue o personagem coletando moedas/tokens e derrubando cientistas | [C] (S5) |
| Lucky Last | Garantia de prêmio na Final Spin | [C*] (S5, S15) |
| Outros (lista completa) | Não verificados | lacuna |

## 5. Persistência
| Dado | Proposta para implementação |
|---|---|
| saldo de moedas, tokens, recorde, itens comprados/equipados, missões e patente, conquistas, configurações | `SaveData` serializado em JSON local (`Application.persistentDataPath`), versão de esquema, escrita atômica (temp + rename), salvo ao fim da corrida e a cada compra |
| Original: salvamento em nuvem/conta | [G] existe em versões atuais; fora do escopo do protótipo |
