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
- [x] **R-09 - Terrain event cadence** (pp. 9, 12, 14, 20). Bound the source's explicit Wastes daily checkpoint to rest/day transitions: resolve 2d6 weather (including effects) and its weather-modified `1d12` encounter. Pillar mining and Ruin room events remain tied to their source-specific triggers; no generic daily procedure was invented for them. Pseudocode: `onWastesDayEnd -> weather -> modifiedEncounter`. Tests cover Wastes effects/encounter and non-Wastes non-invention. Now implemented by `DayResolutionService.ResolveDay` (`DayResolutionRules.cs`), which folds the navigation roll, weather (applying Wind Blast damage to party state), and the weather-modified encounter (via `EncounterResolver`) into one operation; the earlier `TerrainDayEventService` shim was removed once it had no callers.
- [x] **R-10 - Regional/local map generation** (p. 10). Existing generation now exposes and tests every regional terrain, local density, and local terrain d6 branch against the source tables.

## Hazards, Wastes, and Pillars

- [x] **R-11 - Roaming-hazard movement** (p. 11). Corrected the d6 table to Warband/Maelstrom/Crawlherd/Collapse/Void Lightning/Singing Sand. Existing daily movement, exit re-drop, face preservation, and rest advancement are now supplemented by a deterministic collision re-drop test.
- [x] **R-12 - Roaming-hazard resolution** (p. 11). Implemented a source-bound hazard resolver for all six faces: combat encounter counts, Maelstrom displacement/damage and shelter, Collapse flight/crush and terrain loss, Void Lightning metal strikes, and Singing Sand Breath-save prompts. Combat and saving throws are returned to their dedicated systems. Pseudocode: `onEnterHazard -> resolveHazard(d6, terrain, avoidance) -> emitCombatDamageSaves`.
- [x] **R-13 - Wastes weather table** (p. 12). Encoded all 2d6 outcomes and their travel loss, landmark obstruction, encounter modifier, light loss, damage, Breath-save prompts, protection, exhaustion, and burial consequences. Pseudocode: `weather = roll2d6(); applyWeather(weather, party, protection)`. Tests cover every total and all protective/choice branches.
- [x] **R-14 - Wastes encounter, mood, and curiosity tables** (p. 12). Encoded the 1–18 weather-modified encounter table, all Nomad/Bandit/Cutthroat d6 mood bands, and every curiosity d20 face. Pseudocode: `encounter = table[d12 + modifier]; mood = table[d6] when required; curiosity = table[d20]`. Tests verify every range and mood branch.
- [x] **R-15 - Wastes factions and abilities** (p. 13). Encoded all four faction abilities and training requirements: category-matched free barter, arm's-reach burden transfer, day-long tool-assisted Wastes hunting, and exhaustion-for-tool substitution. Pseudocode: `useFactionAbility(faction, eligibility, state)`. Tests cover costs, eligibility, and output ranges; the source states no cooldowns.
- [x] **R-16 - Pillar mining** (p. 14). Implemented hourly gathering/mining yields, mining-tool gate, 1-slot raw lodestone storage, 1d6/2d6 encounter pressure, inventory limits, and settlement refinement at 1d10 × 10 coins each. Pseudocode: `for each hour: rollYield; reserveSlots; addEncounterModifier; refineAtSettlement`. Tests cover yields, tools, limits, and value.
- [x] **R-17 - Pillar encounter/mood table** (p. 14). Encoded every accumulated encounter result from 1 through 15+ plus Lodestone Miner, Bandit, and Cutthroat mood branches and demands. Pseudocode: `resolvePillarEncounter(baseD6 + miningModifier); mood = moodTable[d6] when required`. Tests cover every range, the 15+ cap, and mood bands.
- [x] **R-18 - Pillar tunnel generation** (p. 15). Implemented six tunnel shapes, 10-minute travel/30-minute search timing, depth tracking, and duplicate-shape split markers. Pseudocode: `enterPillar -> rollShape -> if repeated rollSplitMarker -> appendDepth`. Tests cover shapes, depth, split creation, and timing.
- [x] **R-19 - Pillar events and loot tables** (p. 15). Encoded every event and loot range, including 15+ Call of the Dark and 14+ Hoard. Event rolls add previous tunnel rolls and loot adds depth. Pseudocode: `eventTotal=d6+previousRolls; lootTotal=d6+depth`. Tests cover escalation and caps.

## Settlements and social systems

- [x] **R-20 - Settlement generation** (p. 16). Encoded all population bands with location/faction dice, scarcity outcomes, and six atmosphere outcomes. Pseudocode: `settlement = roll(population, scarcity, atmosphere)`. Tests cover all d6 results and deterministic generation.
- [x] **R-21 - Settlement rest, purchase, and resupply rules** (pp. 16-17). Implemented scarcity-adjusted purchases: sell-only, shared 1d6 inventory, doubled prices, barter gate, normal market, and a free additional supply. Storyteller rest gets its source-defined 1-in-6 extra exhaustion recovery. Pseudocode: `purchase -> validateScarcity -> consumeSharedStock -> grantItems; rest -> storytellerRecovery`. Tests cover all purchase/recovery modifiers.
- [x] **R-22 - Settlement services** (p. 17). Encoded all twelve locations and every stated service, barter/material prerequisite, timed benefit, and numeric result. Lodestone Carver exchange plus Remedy/Malady branches are executable; services without a source-stated price remain explicit trade decisions. Pseudocode: `resolveService(service, tradePrerequisites) -> result`. Tests verify all locations and defined numeric branches.
- [x] **R-23 - Denizens and settlement factions** (pp. 18-19). Encoded every denizen's offer/obligation and each settlement faction's ability/training trigger. Implemented source-defined Jarred Fire timing, Seeker Keeper daily pocket reset, Black Helm memory-loss benefit, and Grafter willingness/cost. Pseudocode: `interact(entity, fulfilledObligation) -> offer; useFactionAbility -> validateLimitAndCost`. Tests cover data completeness, refusal, dice outputs, and daily reset.

## Ruins and the Deep

- [x] **R-24 - Ruin graph generation** (p. 20). Implemented five-visible-face ruin graph construction: a face-up cluster and north/south/east/west attached lines, with rooms and connecting passages. Pseudocode: `rollFiveVisibleFaces -> centralCluster -> attachFourLines -> connectPassages`. Tests verify source-face room counts, deterministic rolls, and graph connectivity.
- [x] **R-25 - Ruin exploration** (p. 21). Implemented passage-constrained room discovery, 10-minute hallway/room movement, 30-minute searches, and depth transitions that replace the layout and mark the descent room. The page contains no Ruin-specific lost procedure, so none was invented. Pseudocode: `exploreConnectedRoom -> advanceTime -> markVisited; descend -> generateNewLayout`. Tests cover blocked movement, timing, search, and depth transition.
- [x] **R-26 - Ruin room table** (pp. 22-27). Completed all printed room rows with source-linked effect definitions covering hazards, searches, saves, damage, timing, encounters, depth changes, rest safety, and persistent effects. The duplicate `45` (Reliquary/Fountain) remains two selectable rows and absent `32` remains absent. Pseudocode: `roomChoices = registry[firstD6, secondD6]; effect = roomEffects[code, chosenRoom]`. Tests verify source-row coverage, duplicate handling, saves, damage, and chance branches.
- [x] **R-27 - Ruin room features** (p. 28). Encoded page 28's full 1d20 + depth feature registry, preserving the printed duplicate `25` and absent `24`, and implemented Unstable room/pathway removal. Pseudocode: `featureChoices = table[d20+depth]; if unstableDamaged -> removeRoomAndPassages`. Tests cover bounds, source ambiguities, and collapse removal.
- [x] **R-28 - Ruin discoveries** (p. 29). Implemented Irretrievable Scribe 1d20 + depth discovery checks, one-per-depth-room resolution, and all six discovery outcomes including safe rest. Pseudocode: `if firstEntry(room) and d20+depth >= 20 -> rollDiscovery(d6)`. Tests cover all outcomes, threshold, and duplicate prevention.
- [x] **R-29 - Ruin encounters** (p. 30). Encoded all `1d12 + depth` encounter totals through Wyrm at 22+, Bandit/Delver mood tables, and the supplied Bandit/Traveler-or-Cutthroat/Delver stat blocks. Pseudocode: `encounter = table[d12+depth]; mood = table[d6] when required`. Tests cover ranges, moods, and stat integrity.
- [x] **R-30 - Ruin treasures** (p. 31). Encoded all 34 source treasures in Useful/Special/Great-and-Terrible bands selected by `1d20 + depth`, preserving each listed ongoing effect, cost, duration, and daily constraint. Pseudocode: `magnitude = band[d20+depth]; treasure = table[magnitude, roll]`. Tests cover category thresholds and every unique entry.
- [x] **R-31 - Gifts of the Deep** (p. 32). Encoded all ten gifts, enter/new-level gain, source-listed costs and effects, plus Grafted Limbs/Void of Presence daily limits. Pseudocode: `onDeepEntryOrLevel -> gainGift(d10); useGift -> validateOwnershipAndDailyLimit`. Tests cover every gift, ownership, and reset.
- [x] **R-32 - Minotaur and Touch effects** (p. 33). Implemented persistent 1-in-6 Deep-area arrival, source stat block, and every Touch branch: tool decay, 3d6 damage, 1d6 exhaustion, permanent 1d3 Flesh loss, and memory loss. Pseudocode: `enterDeepArea -> if d6==1 beginPursuit; touch -> table[d6]`. Tests cover arrival persistence and all touch results.
- [x] **R-33 - Trials of the Deep** (pp. 34-36). Implemented all six trials: Scale, Repetition, Change, Emptiness, Sacrifice, and Lies, including its false-world death/reawakening procedure. Pseudocode: `trialState.advance(choice) -> exitOrConsequence`. Tests cover every trial transition and exit condition.
- [x] **R-34 - Escaping the Vast** (pp. 36-37). Decoded and encoded the source's exact Leaving the Vast sequence as ordered ritual state, plus the Lies reawakening terminal state. Pseudocode: `completeRitualSequence -> fallForward -> escapedHome; dieInLies -> reawaken`. Tests cover incomplete rejection, escape, and false-world death.

## Rites and creatures

- [x] **R-35 - Rite acquisition and casting** (p. 38). Implemented unbounded Rite currency, all four source gain routes, first-entry/first-descent gating, sacrifice validation, Erosion of Self's unhealable lock, and one-Rite casting spend. Coin flips are rite-specific (Fickle Descent), not a general casting procedure. Pseudocode: `gainRite(method, cost); spendRiteToCast()`. Tests cover every gain route and Erosion release.
- [x] **R-36 - Rites** (pp. 38-39). Encoded all 15 Labyrinth, Dark, Harrow, and Sparks rites, including Rite versus body cost, level scaling, coin direction, duration, Cinderhowl alert chance, and Like Clay swaps. Pseudocode: `castRite(rite, caster, target) -> validateCost -> applySourceEffect`. Tests cover every spell registration and deterministic numeric branches.
- [x] **R-37 - Daily-use constraints** (pp. 31-32, 38-39). Implemented a reusable, case-insensitive once-per-day gate and reset for only effects explicitly marked daily (for example Spell Eater, Oracle Skull, and applicable Gifts). The source defines no universal damage interruption or concentration system, so none was invented. Pseudocode: `if effectHasDailyLimit -> claim(effectId); onDayStart -> resetUses`. Tests cover independent effects and reset.
- [x] **R-38 - Crawl creature stat blocks and special abilities** (pp. 40-41). Encoded all ten Crawl stat blocks plus tested helpers for Cyclops Call, Medusa Scream, Harpy Meld, Griffon Devour, Hydra Frenzy/Venom, Shade Eye-bite, Ogre Split, and Wyrm Howl. Pseudocode: `spawnCreature(id) -> statBlock; onSpecialTrigger -> resolveAbility`. Tests cover stat integrity and each fully specified special branch.

## Current implementation status

Existing repository code covers partial local travel, map generation, roaming-hazard movement, party persistence, rest, forced march, and generic Wastes/travel abstractions. It does **not** yet satisfy any backlog item until its source-table coverage and deterministic tests are explicitly completed.
