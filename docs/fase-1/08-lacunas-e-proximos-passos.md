# 08 — Lacunas, dúvidas, limitações e próximos passos

## 1. Limitações desta fase
1. **Sem acesso direto às páginas-fonte**: a rede do ambiente bloqueou
   fandom.com, wikipedia.org, appgamer.com e halfbrick.com. Usaram-se apenas
   trechos de resultados de busca.
2. **Sem observação de jogo**: não há dispositivo com o jogo nem gravações
   para medir física, tempos, posições de UI ou cores.
3. **Divergências entre versões**: taxa de troca de tokens (50 vs. 100),
   prêmios em moedas, estrelas por ciclo (63 vs. 105), efeito do Lucky Last.

## 2. Itens não investigados ou não confirmados
| Área | Item |
|---|---|
| UI | Layout, cores, tipografia, ícones, transições de **todas** as telas |
| UI | Existência e conteúdo de tela de carregamento e de eventos na versão atual |
| Física | Gravidade, empuxo, velocidades reais (todos os valores são [E]) |
| Perigos | Dimensões, frequências, tempos exatos de laser; rastreamento de mísseis |
| Veículos | Controle do S.A.M.; frequência de tokens de veículo; invencibilidade pós-veículo; veículos adicionais |
| Economia | Preços na loja; lista completa de gadgets e utilitários; lista de jetpacks e roupas |
| Progressão | Tabela de estrelas por patente; nomes das patentes 1–10; lista de missões; recompensas por patente |
| Áudio | Músicas e efeitos (gatilhos e mixagem) |
| Conquistas | Lista completa |

## 3. Como fechar as lacunas (recomendado)
- **Opção A (melhor)**: o usuário grava 5–10 partidas (60 fps, tela cheia) e
  capturas de cada menu da versão instalada, e envia para o repositório
  (pasta `evidencias/`, uso interno de estudo, não redistribuir). Isso permite
  medir física quadro a quadro e documentar a UI com precisão.
- **Opção B**: liberar na configuração do ambiente os hosts
  `jetpackjoyride.fandom.com`, `en.wikipedia.org`, `www.halfbrick.com` para
  leitura integral das fontes.
- **Opção C**: prosseguir com as estimativas [E] e calibrar "por sensação"
  durante o protótipo (menor fidelidade).

## 4. Próximos passos (Fase 2 → 4)
1. **Fase 2** — documentação visual/funcional detalhada por tela (wireframes
   próprios, hierarquia, estados), assim que houver evidências visuais.
2. **Fase 3** — mapeamento de sistemas: máquinas de estado (jogador,
   veículos, laser, míssil), tabelas de geração por segmentos, eventos.
3. **Fase 4** — arquitetura Unity: os 19 managers do prompt com
   responsabilidades, entradas/saídas, eventos (event bus/ScriptableObject
   events), modelo de dados (`SaveData`, `ItemDef`, `MissionDef`,
   `VehicleDef`, `SegmentDef`).
4. **Fase 5** — protótipo de movimentação com os parâmetros [E] do doc 03.

## 5. Decisões que precisam de aprovação
- Versão de referência: **mobile atual** (doc 01).
- A implementação será um **jogo original do gênero**, com nome, personagem,
  arte e sons próprios; não reproduzirá nome, personagens ou visual do original.
- Escolha entre as opções A/B/C acima.
