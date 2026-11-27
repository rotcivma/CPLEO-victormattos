# 05 — Veículos

## Regras gerais
| Regra | Conf. |
|---|---|
| Veículos são obtidos coletando um **token de veículo** (caixa/ícone) durante a corrida | [G] |
| O veículo funciona como "escudo": um impacto o destrói (explosão) e o personagem volta a pé | [C] (S2) |
| Exceção: S.A.M. resiste a 3 mísseis | [C] (S2) |
| Cada veículo reinterpreta o botão único | [C] (S3, S9, S10) |
| Upgrades/skins de veículos são vendidos na loja | [C] (S1) |

## Catálogo

| Veículo (original) | Controle | Comportamento | Fontes / Conf. |
|---|---|---|---|
| **Lil' Stomper** (mecha bípede) | Toque = salto; segurar = retarda a queda (propulsores nas mãos) | Salta baixo; aterrissagem faz o chão tremer; pisa em cientistas; vulnerável a zappers, lasers, mísseis | [C] (S3, S10) |
| **Profit Bird** (pássaro mecânico) | Toque = bate asas/sobe; soltar = desce | Considerado o mais fácil; dica de ficar no meio da tela | [C] (S3, S10) |
| **Crazy Freaking Teleporter** | Toque = teletransporta na direção apontada por um ponteiro oscilante | Pode atravessar zappers se não terminar dentro deles | [C] (S9, S10) |
| **Bad As Hog** (moto) | Toque = salto | Veículo mais rápido; acelera progressivamente; mísseis não surgem durante seu uso | [C] (S2, S9) |
| **Mr. Cuddles** (dragão mecânico) | Segurar = desce; soltar = sobe com movimento ondulante | Flutua perto do teto; sopro de fogo atinge cientistas | [C] (S4) |
| **Gravity Suit** | Toque = inverte a gravidade (também no ar) | Gruda no chão ou teto; inversão no ar precisa vencer o momento atual; considerado lento | [C] (S4, S10) |
| **Wave Rider** (jet ski) | Segurar = mergulha; soltar = salta para fora d'água; tocar no ar acelera a queda | O laboratório é inundado durante o uso; ao ser destruído, a água escoa | [C] (S4) |
| **S.A.M.** (Strong Arm Machine) | [G] braço que agarra/arremessa | Aguenta 3 mísseis | [C] resistência (S2) / [G] controle |

## Pendências
- Duração (os veículos duram até serem destruídos? [G] sim, sem temporizador).
- Frequência de tokens de veículo por distância.
- Comportamento da invencibilidade breve após perder o veículo ([H] ~1 s).
- Lista completa de veículos na versão atual (pode haver outros, sazonais).

## Mapeamento para a implementação própria (nomes provisórios)
| Mecânica | Nome próprio provisório |
|---|---|
| mecha saltador com planar | "Stomp Mech" → renomear |
| voador por batida | "Flap Drone" |
| teleporte direcional | "Blink Pod" |
| moto rápida com salto | "Rail Bike" |
| voador invertido (segurar desce) | "Sky Serpent" |
| inversão de gravidade | "Flip Suit" |
| veículo aquático mergulha/salta | "Splash Ski" |
| veículo blindado (3 acertos) | "Heavy Rig" |

Os nomes finais e todos os visuais serão originais.
