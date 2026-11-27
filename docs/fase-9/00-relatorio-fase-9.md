# Relatório da FASE 9 — Testes e refinamento

**Data:** 2026-11-27 · **Código:** `unity/Propulsor`

## 1. Desempenho (O-06, PF-01, PF-02)
| Medida | Antes | Depois |
|---|---|---|
| Memória alocada em 5.000 passos (~100 s de corrida) após aquecimento | ~5.200 B | **88 B** |
| Tempo por passo da simulação (.NET 8, neste contêiner) | — | **~4 µs** (orçamento por quadro: 16.600 µs) |
| Publicar um evento | 1 array por publicação | **0 B** (lista de ouvintes "copy-on-write") |

Como a medição foi feita: uma sonda registrou os passos que alocavam memória e a causa de cada um.

| Causa encontrada | Correção |
|---|---|
| Enumerador alocado ao percorrer os trechos por interface (40 B por trecho novo) | Laço com índice |
| Veículo criado a cada montagem; nomes de contadores montados na hora | Veículos criados no início da corrida e reaproveitados; textos pré-montados |
| `RemoveAll` com expressões lambda e objetos novos a cada trecho, coletável e drone | Pools para trechos, coletáveis, robôs, drones e sequências de feixes |

Foi criado o `RunConfig.GodMode`, um modo sem dano usado só em testes de desempenho e de sensação. Ele nunca é ligado no jogo.

## 2. Mecânicas que faltavam (observadas na referência)
| Mecânica | Implementação própria |
|---|---|
| Personagens derrubáveis | **Robôs operários** (0–2 por trecho) andam pelo chão; caem com esbarrão, com o jato do propulsor ou com veículos; nunca matam |
| "Raspão" | Passar a até 0,45 u de um arco de faísca (uma vez por arco) ou de um drone sem ser atingida → evento `HazardNearMiss` |
| Missões e conquistas novas | "Derrube N robôs" (por corrida e total), "Passe raspando por N perigos" (por corrida e total); conquistas *Desmontadora* e *Por um Fio* |

## 3. Áudio (todo sintetizado, sem arquivos)
- **`AudioDirector`** (Core, testado): 21 efeitos ligados a eventos; música por estado (menu / corrida / garra); música mais baixa durante aviso de drone (−6 dB) e na pausa (−10 dB); porcas seguidas sobem meio tom cada (até 1 oitava); chiado do propulsor só enquanto sobe.
- **`AudioManager` + `ProceduralSynth`** (Unity):
  - Efeitos gerados com ondas senoidal, quadrada, triangular e dente de serra, além de ruído filtrado.
  - Três trilhas curtas próprias em loop, cada uma com baixo, arpejo e chimbal:

    | Trilha | Andamento | Acordes |
    |---|---|---|
    | Menu | 100 BPM | Lá menor, Fá, Dó, Sol |
    | Corrida | 140 BPM | Mi menor, Dó, Sol, Ré |
    | Garra | 120 BPM | Dó, Fá, Sol, Dó |

  - 10 vozes para efeitos, transição cruzada de 0,5 s entre músicas e volumes lidos do perfil.
- **Clique de botão:** todo botão da interface tem som.

## 4. Visual
| Item | O que entrou |
|---|---|
| Fontes do guia | **Lilita One** (títulos e botões) e **Nunito Bold** (textos), licença SIL OFL 1.1, em `Assets/_Project/Resources/Fonts` com os arquivos de licença; a fonte embutida do Unity fica de reserva |
| Robôs operários | corpo cinza com visor amarelo, balançam ao andar, giram ao cair |
| Efeitos de impacto | explosão de partículas ao perder veículo; faíscas ao perder blindagem; brilho ao pegar porca; texto flutuante "raspão!" |
| Tremor de câmera | ao perder veículo, morrer, levar dano na blindagem e no início da arrancada |

## 5. Testes
| Conjunto | Executado | Resultado |
|---|---|---|
| Lógica — **177 testes** (16 novos: robôs, raspões, áudio, desempenho) | Sim, `dotnet test` | **177/177** |
| Sintaxe C# de todos os scripts | Sim (Roslyn) | 0 erros |
| PlayMode (+1: síntese de todos os sons no Unity) | Não (sem Unity) | pendente |

**Bugs encontrados nesta fase e corrigidos:**
1. A checagem de dano de feixes e drones ignorava o `GodMode`: uma substituição de texto não tinha casado por causa de uma linha em branco. Os testes de desempenho detectaram.
2. Alocações de memória escondidas, listadas na §1.

**Ajustes só nos testes, sem mudar regras do jogo:**
- O bot de "raspão" pairava com controle liga-desliga e mergulhava no arco; agora o controle antecipa pela velocidade.
- O teste de robô derrubado observava um objeto que o pool reciclava; agora confere que é o mesmo robô.

## 6. Matriz de testes — situação consolidada
`docs/fase-4/04-plano-de-testes.md`: **57 itens ✅** na lógica. Pendentes:

| Pendência | Por quê | Como fechar |
|---|---|---|
| F-06 (tempo chão→teto vs. referência), M-01 (pairar) | precisam de vídeo de referência e teste manual | gravar o original e comparar quadro a quadro; ajustar `PlayerPhysics.asset` ao vivo |
| N-01…N-04, R-01, AU-06 e todos os PlayMode | sem Unity neste ambiente | rodar o Test Runner no Unity 6 |
| O-06 no Profiler, D-01 (60 fps em Android) | precisam do Unity e de um aparelho | Profiler + build de desenvolvimento |
| V-01…08 (sensação) | teste manual | jogar com a tecla V |

## 7. Próximo passo — Fase 10 (entrega final)
Documento final consolidado com todos os entregáveis do Prompt Mestre e o mapa de onde cada um está, configuração de build Android/WebGL e roteiro de validação no Unity.
