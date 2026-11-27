# Módulo 05 — Direção de arte e reconstrução gráfica (identidade própria)

**Data:** 2026-11-27 · **Código da arte:** `web/art.js` · **Gerador de sprites:** `web/tools/bake-sprites.js`
**Sprites do Unity:** `unity/Propulsor/Assets/_Project/Resources/Art/` (+ `manifest.json`)

## 1. Escopo
- O pedido era uma reconstrução visual "muito semelhante", em que o jogador reconhecesse o original de imediato. Isso equivaleria a recriar a aparência de um jogo protegido: o personagem, o laboratório e o visual específico do *Jetpack Joyride*.
- Por isso o módulo foi aplicado assim:
  - **Usado das referências:** técnicas e padrão de qualidade.
  - **Mantido próprio:** a identidade do PROPULSOR, com Nina e a fábrica de brinquedos Engrenópolis.
- As 4 capturas enviadas são arte da Halfbrick e **não foram copiadas, traçadas nem incluídas no repositório**. Serviram só para a análise da §2.

## 2. Análise das referências (técnicas observadas)
| Aspecto | O que as referências mostram | Como foi aplicado no PROPULSOR |
|---|---|---|
| Traço | Contorno escuro e grosso em tudo; sombreamento em 2–3 tons (luz em cima) | Função `cel()`: gradiente em 3 faixas + contorno `#1a1630` em todos os elementos |
| Proporção | Personagem com cabeça grande e expressões fortes | Nina com cabeça ~1/3 do corpo, 6 expressões (sorriso, determinação, euforia, preocupação, susto, nocaute) |
| Efeitos | Brilhos em estrela nas pontas; feixes com núcleo branco; chamas em camadas | `burst()` em estrela com núcleo branco; feixes em 4 camadas; chamas em 3 camadas |
| Perigos | Silhuetas muito legíveis e contrastantes com o fundo | Bobinas de cobre + arco ciano; emissores laranja; foguetes listrados; selo de aviso |
| Cenário | Corredor com teto e chão em faixas com volume; parede rica em painéis | Teto com treliça e lâmpadas, chão em placas com borda iluminada; 2 camadas parallax cheias de máquinas |
| Fumaça | Rastro denso atrás de projéteis | Fumaça que cresce e some atrás de cada foguete |
| Interface | Números grandes no canto superior, botão de pausa discreto | Placar em Lilita One com contorno; pausa redonda; selo de aviso na borda direita |

## 3. O que foi mudado de propósito
| Original | PROPULSOR |
|---|---|
| Protagonista homem de jaleco e terno | **Nina**, mecânica de macacão verde-água, rabo de cavalo, óculos de solda, extintor como propulsor |
| Laboratório azul-acinzentado com faixa amarela | **Fábrica de brinquedos** com 4 setores de paleta própria: Montagem (azul aço), Caldeiras (vinho/laranja), Testes (verde-azulado), Depósito (roxo) |
| Cientistas de traje branco | **Parafusinhos**: robôs operários com visor e antena |
| Zappers amarelos com esferas | **Bobinas de cobre** com isoladores e arco **ciano** (laranja quando giram) |
| Mísseis cinza com ponta vermelha | **Foguetes de brinquedo** brancos com listras vermelhas e verde-água |
| "!" em estrela amarela | **Selo de engrenagem** (motivo da Engrenópolis) |
| Moedas redondas | **Porcas sextavadas** douradas que giram |

## 4. Catálogo de recursos gerados
| Categoria | Arquivos | Detalhe |
|---|---|---|
| Characters | Nina em **3 camadas** × 8 poses: `Nina_<pose>_<roupa>` (4 roupas), `NinaJet_<pose>_<propulsor>` (4 propulsores, com chama), `NinaHat_<pose>_<acessório>` (capacete, boné); `Worker_{walk,knocked}` | As 8 animações pedidas no módulo (parada, correndo, voando, subindo, descendo, colisão, lançada, morte), em todas as combinações de visual |
| Vehicles | `Vehicle_{mech,drone,blink,bike,serpent,flip,ski,rig}` | 8 quadros de movimento cada; entrada (escala com brilho) e destruição (destroços + explosão) são animadas em código |
| Jetpacks | `jet_padrao`, `jet_bolhas`, `jet_confete`, `jet_vapor` | Formas diferentes (extintor, tanques de bolha, canhão de confete, caldeira de cobre com chaminé), não só troca de cor |
| Obstacles | `Zapper_{Coil,Glow,Arc}`, `Laser_{Emitter,Beam}`, `Rocket`, `Warning`, `Warning_Locked` | Emissor com 3 estados (desligado/aviso/disparo) |
| Pickups | `Nut`, `Token`, `VehicleCrate` | Porca com giro em 8 quadros |
| Particles | `Explosion` (10 q.), `Smoke`, `Burst` | — |
| Icons | 19 itens × 3 estados (`_locked`, `_owned`, `_equipped`), 256 px | Bloqueado: cinza + cadeado; equipado: selo verde |
| Backgrounds | `Tiles/Far_<setor>_0..3`, `Tiles/Mid_<setor>_0..5`, `Tiles/Floor`, `Tiles/Ceiling`, `Setor_<nome>` | Blocos que emendam sem corte; 2 camadas de parallax (0,22 e 0,55) |

Todos os arquivos são PNG com transparência e quadros em faixa horizontal. O `manifest.json` informa, para cada folha:
- número de quadros e velocidade (fps);
- tamanho do quadro e pixels por unidade;
- ponto de apoio.

No Unity:
- **`SpriteLibrary`** fatia as folhas em tempo de execução.
- **`ArtImportSettings`** configura a importação sem redimensionar.
- **`PlayerView`, `WorldView` e `CorridorView`** usam os sprites quando existem e voltam para a arte geométrica se faltarem. A `PlayerView` empilha as camadas da Nina conforme o que está equipado na Oficina.

Para gerar tudo de novo depois de mudar a arte: `node web/tools/bake-sprites.js`.

## 5. Validação visual (contra o guia próprio da Fase 2)
| Critério | Situação | Evidência |
|---|---|---|
| Contorno e cel shading consistentes | ✅ | `img/v2-run.png`, `img/folha-nina-corrida.png` |
| Cenário rico, sem vazios, sem cortes no parallax | ✅ | `img/v2-run.png`, `img/v2-hazards.png` (blocos emendam pela tubulação contínua) |
| Perigos legíveis sobre o fundo | ✅ | `img/v2-hazards.png` (feixes, bobinas, foguete com fumaça, aviso) |
| Animações fluidas da personagem | ✅ na web; ⏳ no Unity | folha de corrida com 12 quadros |
| Ícones com 3 estados | ✅ | `img/v2-shop.png` |
| Celular em pé | ✅ | `img/v2-portrait.png` (câmera afastada, teto e subsolo preenchem a tela) |
| Sem pixelização | ✅ na web (desenho vetorial na resolução da tela); Unity: folhas a 96–128 px/u | — |

**Diferenças conhecidas e correções pendentes:**
1. ~~Roupas no Unity~~ — **resolvido**: a Nina é montada em 3 camadas (corpo por roupa + propulsor + acessório). Cada camada é extraída comparando os desenhos quadro a quadro, então encaixa exatamente na pose. Isso evitou gerar as 32 combinações completas (~13 MB → ~5 MB).
2. **Arco elétrico no Unity:** é uma folha esticada pelo comprimento, então a espessura fica igual em arcos curtos e longos.
3. **Arte procedural:** tem acabamento cartoon consistente, mas não é pintura à mão. Para um visual de nível comercial, o próximo passo é um artista redesenhar Nina e os veículos usando esta base como modelo de proporções e poses.
4. **Ainda não validado no Unity**, porque não há Unity neste ambiente.
