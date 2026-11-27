# Relatório da FASE 1 — Investigação do jogo original

**Data:** 2026-11-27 · **Versão de referência:** Jetpack Joyride, mobile atual (ver doc 01)

## 1. O que foi feito
- Definição de escopo legal: engenharia reversa **observacional e funcional**;
  implementação futura será um **jogo original do gênero**.
- Pesquisa em 17 fontes públicas (wikis, guias, FAQ oficial Halfbrick, lojas).
- Inventário inicial de 17 telas e mapa de navegação.
- Documentação inicial de: loop de gameplay, controles, física (com valores
  estimados), perigos (zappers, lasers, mísseis), 8 veículos, economia,
  Final Spin, gadgets, missões, patentes, conquistas.
- Registro explícito de confiabilidade de cada informação ([C], [C*], [G], [H], [E]).

## 2. Principais achados confirmados
| Área | Achado |
|---|---|
| Controle | Botão único: segurar sobe, soltar cai |
| Perigos | Zappers (estático amarelo / rotativo laranja), lasers, mísseis; morte instantânea a pé |
| Mísseis | Entram pela direita com aviso de ~1–2 s; tempo de reação cai com a distância; rajadas de 5+; não surgem durante lasers nem com Bad As Hog |
| Veículos | Um acerto destrói o veículo e devolve o personagem a pé; S.A.M. aguenta 3 mísseis; controles específicos de 7 veículos documentados |
| Final Spin | 1 token = 1 giro; prêmios: moedas, Second Chance, Head Start (~750 m), Blasts (Atom ~800 m), Double Coins, +3 tokens |
| Gadgets | 2 slots (+1 pago); Coin Magnet, Gravity Belt, Air Barrys, Flash, Lucky Last |
| Missões | 3 simultâneas; 1–3 estrelas; pular = 500 moedas/estrela |
| Patentes | Ciclo de 15 níveis → badge de prestígio (125 no total) |

## 3. Evidências utilizadas
Trechos de busca das fontes listadas em [01-versao-e-fontes.md](01-versao-e-fontes.md).
**Nenhuma** observação direta do jogo foi feita (sem dispositivo/gravações).

## 4. Ainda não investigado
Ver [08-lacunas-e-proximos-passos.md](08-lacunas-e-proximos-passos.md):
layout visual de todas as telas, parâmetros reais de física, tempos e
dimensões de perigos, preços, listas completas (gadgets, jetpacks, roupas,
missões, conquistas), áudio.

## 5. Próximos passos
1. Decidir a forma de obter evidências (gravações do jogo / liberar hosts / seguir com estimativas).
2. Fase 2: documentação visual e funcional detalhada.
3. Fases 3–4: mapeamento de sistemas e arquitetura Unity (19 componentes).
4. Somente então: Fase 5 (protótipo de movimentação).

**Nenhum código foi escrito nesta fase**, conforme a ordem de trabalho.
