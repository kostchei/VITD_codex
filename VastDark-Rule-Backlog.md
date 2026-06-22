# VastDark Rule Backlog

This backlog is generated from the page-ordered rules source, [the-vast-in-the-dark.md](the-vast-in-the-dark.md), and is the implementation contract for the domain layer. A task is complete only when its source page is linked, its pseudocode contract is represented in code, deterministic tests pass, and the work is committed.

## Delivery process

1. Read the cited source page and transcribe the mechanical rule into a data definition or pseudocode contract.
2. Add a failing domain test that fixes the rule's expected outcome with scripted dice.
3. Implement the smallest domain API that satisfies the test.
4. Run the domain test executable and full Godot build.
5. Correct failures, re-run both checks, update this item, and commit the slice.

Content tables are data tasks: every table row must become data and must be validated for its documented dice range. They are not to become untestable UI branches.

## Page audit

| Pages | Rule area | Backlog IDs |
| --- | --- | --- |
| 1-5 | Cover, map reference, setting, compatibility | Reference only |
| 6 | Traveler quirks | R-01 |
| 7 | Inventory, exhaustion, Grit, Flesh, injury, recovery | R-02 to R-05 |
| 8 | Harrowing and memories | R-06 |
| 9 | Travel, navigation assets, becoming lost | R-07 to R-09 |
| 10 | Regional/local map generation | R-10 |
| 11 | Roaming hazards | R-11 to R-12 |
| 12-13 | Wastes weather, encounters, curiosities, factions | R-13 to R-15 |
| 14-15 | Pillar mining, encounters, tunnels, events, loot | R-16 to R-19 |
| 16-19 | Settlements, services, denizens, factions | R-20 to R-23 |
| 20-31 | Ruin generation, rooms, encounters, treasure | R-24 to R-30 |
| 32-37 | Deep, Minotaur, trials, escape | R-31 to R-34 |
| 38-39 | Rites and spell casting | R-35 to R-37 |
| 40-41 | Crawl creatures and stat blocks | R-38 |
| 42-44 | Credits, reference sheet, advertisement | Reference only |

## Core traveler rules

- [x] **R-00 - Ability scores** (user-confirmed DCC convention). Implemented 3d6 scores with `floor((score - 10) / 2)` modifiers, including the -4 through +4 range tests. This feeds all later saves, inventory slots, Grit, and Flesh rules.
- [x] **R-01 - Traveler quirks table** (p. 6). Encoded all 20 character-creation/advancement quirks as source-linked content, including Ruin Plucker's repeatability. Pseudocode: `quirk = travelerQuirkTable[rollD20()]`. Tests verify every face, unique identities, and deterministic rolling.
- [x] **R-02 - Inventory slots and loadouts** (p. 7). Implemented Constitution-derived capacity, purchased pack capacity, settlement-only loadout assignment at 10 coins per slot, common-item draw replacement, and separately recorded unique or magical items. Tests cover slot accounting, settlement restriction, and unique-item rejection from loadouts.
- [x] **R-03 - Exhaustion** (p. 7). Implemented source-tagged exhaustion for lost sleep, severe wounds, hunger, and forced marching; a full day of rest removes one active level. Tests cover every documented source, persistence, and recovery. The source specifies no exhaustion thresholds, so none were invented.
- [x] **R-04 - Grit, Flesh, injury, and healing** (p. 7). Traveler vitality now derives from DCC scores and level, absorbs damage with Grit before Flesh, assigns a random injured ability on Flesh damage, heals 1d6/2d6 Grit after rest, and heals one Flesh at a settlement. Tests cover formulas, injury, source-tagged severe wounds, recovery, and persistence. Pseudocode: `damage -> grit -> flesh -> randomInjury; rest -> healGrit; settlement -> healFlesh`.
- [x] **R-05 - Packs and supply transport** (p. 7 / reference p. 43). `InventoryRules` encodes Bindle/Sack/Backpack capacities and costs plus Pulk/Sleigh capacity and puller speed limits, with deterministic tests.
- [x] **R-06 - Harrowing and memories** (p. 8). Implemented five distinct memories/drives, all four hardship triggers, controlled memory loss, duplicate prevention, and fifth-memory completion. The source omits a loss probability and final disposition choice, so both remain caller-facing decisions rather than invented mechanics. Pseudocode: `onTrigger -> callerDeterminesLoss -> removeRemainingMemory -> signalFinalMemory`.

## Travel and world generation

- [x] **R-07 - Daily travel** (p. 9). Local one-mile movement, 18-mile daily stints, a single 6-mile forced-march increment, and per-Traveler ration consumption are implemented. Rest starts a new 18-mile stint, recovers pre-existing exhaustion, then applies hunger exhaustion if the day's ration is absent. Tests cover normal, forced, hungry, and early-rest travel.
- [x] **R-08 - Navigation roll and assets** (p. 9). Implemented as `DailyNavigationService`: a d6 failure band reduced by five distinct assets, with Late/Off Course/Dangerously Off Course/Utterly Lost results. Tests cover base rolls, source example, full asset coverage, and duplicate assets.
- [ ] **R-09 - Terrain event cadence** (pp. 9, 12, 14, 20). Define the explicit campaign event cadence and bind it to movement/day transitions. Pseudocode: `if eventCheckpoint then resolveTerrainEvent(currentTerrain)`. Test terrain selection, cadence carry-over, and interrupted travel.
- [x] **R-10 - Regional/local map generation** (p. 10). Existing generation now exposes and tests every regional terrain, local density, and local terrain d6 branch against the source tables.

## Hazards, Wastes, and Pillars

- [x] **R-11 - Roaming-hazard movement** (p. 11). Corrected the d6 table to Warband/Maelstrom/Crawlherd/Collapse/Void Lightning/Singing Sand. Existing daily movement, exit re-drop, face preservation, and rest advancement are now supplemented by a deterministic collision re-drop test.
- [x] **R-12 - Roaming-hazard resolution** (p. 11). Implemented a source-bound hazard resolver for all six faces: combat encounter counts, Maelstrom displacement/damage and shelter, Collapse flight/crush and terrain loss, Void Lightning metal strikes, and Singing Sand Breath-save prompts. Combat and saving throws are returned to their dedicated systems. Pseudocode: `onEnterHazard -> resolveHazard(d6, terrain, avoidance) -> emitCombatDamageSaves`.
- [x] **R-13 - Wastes weather table** (p. 12). Encoded all 2d6 outcomes and their travel loss, landmark obstruction, encounter modifier, light loss, damage, Breath-save prompts, protection, exhaustion, and burial consequences. Pseudocode: `weather = roll2d6(); applyWeather(weather, party, protection)`. Tests cover every total and all protective/choice branches.
- [ ] **R-14 - Wastes encounter, mood, and curiosity tables** (p. 12). Encode 1d12 encounter + 1d6 mood and 1d20 curiosity data. Pseudocode: `encounter = table[d12]; mood = table[d6]; curiosity = optionalTable[d20]`. Test every range and mood branch.
- [ ] **R-15 - Wastes factions and abilities** (p. 13). Encode faction traits, trade rules, recruitment, exhaustion transfer, hunting, and travel bonuses. Pseudocode: `useFactionAbility(faction, action, state)`. Test cooldowns, costs, and eligibility.
- [ ] **R-16 - Pillar mining** (p. 14). Model gathering/mining time, raw lodestone, inventory slots, encounter modifiers, and refined value. Pseudocode: `for hour: collectLodestone(); incrementEncounterRisk()`. Test hourly yields, tool requirements, and slot limits.
- [ ] **R-17 - Pillar encounter/mood table** (p. 14). Encode the documented 1d6 encounter and mood outcomes. Pseudocode: `resolvePillarEncounter(d6, moodD6)`. Test all faces and demands.
- [ ] **R-18 - Pillar tunnel generation** (p. 15). Generate tunnels, splits, depth, events, and loot from source dice. Pseudocode: `enterPillar -> generateTunnel -> applyDepthRules -> branchOrContinue`. Test depth progression, split generation, and repeat prevention.
- [ ] **R-19 - Pillar events and loot tables** (p. 15). Encode escalated event/loot rolls by previous rolls/depth. Pseudocode: `eventTotal=d6+previousRolls; lootTotal=d6+depth`. Test table bounds and escalation.

## Settlements and social systems

- [ ] **R-20 - Settlement generation** (p. 16). Encode population, scarcity, and atmosphere tables. Pseudocode: `settlement = roll(population, scarcity, atmosphere)`. Test every d6 result and settlement state.
- [ ] **R-21 - Settlement rest, purchase, and resupply rules** (pp. 16-17). Implement scarcity-adjusted purchases, rest/recovery, and limited/bountiful inventory. Pseudocode: `purchase -> validateScarcity -> applyInventory; restAtSettlement -> recover`. Test price/availability modifiers and recovery.
- [ ] **R-22 - Settlement services** (p. 17). Encode stories, repair, renew, medicine, hellfire, scrolls, companions, and barter. Pseudocode: `resolveService(service, cost, prerequisites)`. Test every service cost and result.
- [ ] **R-23 - Denizens and settlement factions** (pp. 18-19). Encode their triggers, offers, obligations, and once-per-day abilities. Pseudocode: `interact(entity, choice) -> consequence`. Test acceptance/refusal and daily limits.

## Ruins and the Deep

- [ ] **R-24 - Ruin graph generation** (p. 20). Generate visible rooms, passages, and initial ruin layout. Pseudocode: `rollVisibleRooms(); connectRooms(); markEntrances()`. Test graph connectivity and documented room count.
- [ ] **R-25 - Ruin exploration and getting lost** (p. 21). Encode movement constraints, navigation/room discovery, and ruin-specific lost outcomes. Pseudocode: `exploreRoom -> updateGraph -> resolveLostIfTriggered`. Test new layout and loop prevention.
- [ ] **R-26 - Ruin room table** (pp. 22-27). Encode two-d6 room construction and all room-specific hazards, searches, and saves as data. Pseudocode: `room = combine(roomTypeD6, roomDetailD6); resolveRoom(room)`. Test every table row and every required save/cost.
- [ ] **R-27 - Ruin room features** (p. 28). Encode `1d20 + depth` feature table, including unstable collapse. Pseudocode: `featureTotal=d20+depth; applyFeature(feature)`. Test depth offsets and room-removal effects.
- [ ] **R-28 - Ruin discoveries** (p. 29). Encode discovery trigger, one-per-room behavior, rest safety, and memory costs. Pseudocode: `if discoveryCheck passes then applyDiscovery()`. Test trigger boundaries and unique-use tracking.
- [ ] **R-29 - Ruin encounters** (p. 30). Encode `1d12 + depth` encounter/mood generation and stat-block references. Pseudocode: `encounterTotal=d12+depth; mood=d6; createEncounter()`. Test table ranges and encounter persistence.
- [ ] **R-30 - Ruin treasures** (p. 31). Encode `1d20 + depth` treasure categories and each item effect. Pseudocode: `treasureTotal=d20+depth; grantTreasure(item)`. Test category thresholds, inventory cost, and persistent effects.
- [ ] **R-31 - Gifts of the Deep** (p. 32). Encode 1d10 gifts and their daily/resource constraints. Pseudocode: `grantGift(d10); useGift -> validateLimitAndCost`. Test every gift and once-per-day reset.
- [ ] **R-32 - Minotaur and Touch effects** (p. 33). Encode appearance trigger, d6 touch, damage, exhaustion, memory loss, and pursuit behavior. Pseudocode: `enterDeepRoom -> maybeSpawnMinotaur -> resolveTouch(d6)`. Test all touch results.
- [ ] **R-33 - Trials of the Deep** (pp. 34-35). Encode trial room procedures, repetitions, sacrifices, simulacra, exits, and death consequences. Pseudocode: `trialState.advance(choice) -> nextRoomOrOutcome`. Test state transitions and escape condition.
- [ ] **R-34 - Escaping the Vast** (pp. 36-37). Encode source-specific death/escape transition and campaign-end handling. Pseudocode: `onEscapeOrDeath -> resolveOriginReturnOrAftermath`. Test each documented terminal outcome.

## Rites and creatures

- [ ] **R-35 - Rite acquisition and casting** (p. 38). Encode Rite gain costs, casting procedure, coin flip/outcome, and erosion costs. Pseudocode: `gainRite(cost); castRite -> resolveCoinFlipAndEffect`. Test every gain route and casting branch.
- [ ] **R-36 - Rites of Sparks** (p. 39). Encode each listed rite, prerequisites, durations, damage, and failure/cost rules. Pseudocode: `castRite(riteId, target) -> applyEffect`. Test each rite's deterministic branches.
- [ ] **R-37 - Rite daily resets and interaction constraints** (pp. 38-39). Centralize once-per-day, rest, damage, and concentration-like constraints. Pseudocode: `onDayStart -> resetUses; onDamage -> interruptIfRequired`. Test reset and interruption semantics.
- [ ] **R-38 - Crawl creature stat blocks and special abilities** (pp. 40-41). Encode creature data and combat hooks. Pseudocode: `spawnCreature(id) -> combatant; onSpecialTrigger -> resolveAbility`. Test stat integrity and every special ability's save/damage branch.

## Current implementation status

Existing repository code covers partial local travel, map generation, roaming-hazard movement, party persistence, rest, forced march, and generic Wastes/travel abstractions. It does **not** yet satisfy any backlog item until its source-table coverage and deterministic tests are explicitly completed.
