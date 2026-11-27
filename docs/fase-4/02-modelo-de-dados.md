# Fase 4 · 02 — Modelo de dados

## 1. Definições (ScriptableObjects, imutáveis em runtime)

```csharp
PlayerPhysicsDef { gravity=-38, thrustAccel=70, maxUp=11, maxDown=-14,
                   startSpeed=8, accel=0.05, maxSpeed=20,
                   deathRestitution=0.4, deathFriction=0.3 }

SegmentDef { id, width, minDistance, maxDistance, baseWeight, difficulty(0..1),
             section, hazards: HazardPlacement[], coins: CoinFormation[],
             tokenSlots: Vector2[] }

HazardPlacement { type: ArcStatic|ArcRotating|Laser, localPos, length,
                  angleDeg, angularSpeed, laserRows: int[] }

CoinFormationDef { id, points: Vector2[] }          // linhas, arcos, letras

VehicleDef { id, nameKey, prefab, hitPoints=1, unlockedByDefault, controllerType }

ItemDef { id, category: Jetpack|VehicleSkin|Gadget|Consumable|Outfit|Special,
          price, currency: Porcas|Fichas, slot, maxStack, availableFrom, availableTo }

GadgetDef : ItemDef { modifiers: ModifierEntry[] }
ModifierEntry { stat, op: Add|Mul|Set, value }

MissionDef { id, type, target, scope: Run|Lifetime, stars(1..3),
             condition (ex.: veículo específico, sem coletar porcas) }

RankTableDef { levels: RankLevel[15] }  RankLevel { nameKey, starsRequired, rewardPorcas }

PrizeTableDef { entries: PrizeEntry[] } PrizeEntry { prizeId, weight, value }

AchievementDef { id, stat, threshold, rewardPorcas }
```

### Tabela inicial de prêmios da Garra da Sorte [E]
| Prêmio | Peso | Valor |
|---|---|---|
| Nada | 30 | — |
| Porcas pequeno | 30 | 100 |
| Porcas médio | 15 | 500 |
| Porcas grande | 5 | 1 000 |
| Segunda Chance | 6 | revive imediato |
| Arrancada (próx. corrida) | 6 | 750 m |
| Dobro de porcas (próx. corrida) | 4 | ×2 |
| +3 Fichas | 4 | 3 |

(Refletem as categorias [C] da referência; pesos são próprios.)

### Patentes [E] (nomes próprios)
| Nv | Nome | ★ p/ subir | Recompensa |
|---|---|---|---|
| 1 | Estagiária | 3 | 200 |
| 2 | Aprendiz de Chave | 4 | 250 |
| 3 | Ajudante | 4 | 300 |
| 4 | Aprendiz | 5 | 350 |
| 5 | Operária | 5 | 400 |
| 6 | Técnica | 6 | 450 |
| 7 | Soldadora | 6 | 500 |
| 8 | Montadora | 7 | 600 |
| 9 | Mestre de Linha | 7 | 700 |
| 10 | Engenheira | 8 | 800 |
| 11 | Inventora | 8 | 900 |
| 12 | Pilota | 9 | 1 000 |
| 13 | Ás do Jato | 9 | 1 200 |
| 14 | Lenda da Fábrica | 10 | 1 500 |
| 15 | Nina | — | troca por Selo de Prestígio |

## 2. Estado persistido (`SaveData`, JSON)

```json
{
  "schemaVersion": 1,
  "wallet": { "porcas": 0, "fichas": 0 },
  "inventory": {
    "owned": ["jet_default", "outfit_default"],
    "consumables": { "second_chance": 0, "head_start": 0 },
    "equipped": { "jetpack": "jet_default", "outfit": "outfit_default",
                  "hat": null, "gadgets": ["", ""],
                  "vehicleSkins": { "mech": "default" } },
    "gadgetSlots": 2
  },
  "progress": {
    "rank": 1, "starsInRank": 0, "prestige": 0,
    "activeMissions": [ { "id": "m_fly_300", "progress": 0, "done": false } ],
    "completedMissionIds": []
  },
  "pendingBoosts": { "headStart": false, "doubleCoins": false },
  "stats": { "bestDistance": 0, "totalDistance": 0, "runs": 0,
             "totalPorcas": 0, "workersKnocked": 0,
             "deathsBy": { "arc": 0, "laser": 0, "missile": 0 },
             "vehicleUses": {}, "clawLosses": 0 },
  "achievements": { "unlocked": [] },
  "settings": { "music": 0.8, "sfx": 1.0, "vibration": true, "lang": "pt-BR" }
}
```

## 3. Estado de corrida (não persistido)
```csharp
RunState { seed, distance, runPorcas, runFichas, state, vehicleId?,
           modifiers, missionProgressRun{}, nearMisses, timeAirborne,
           groundDistance, coinsCollectedThisRun }
RunResult { distance, porcas, fichas, deathCause, isNewBest, stats delta }
```

## 4. Migração
`SaveMigrator` com passos `v(n)→v(n+1)`; arquivo corrompido ⇒ tenta `.bak` ⇒ senão perfil novo e registro de erro.
