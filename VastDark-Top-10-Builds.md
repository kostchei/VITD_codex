# VastDark: Top 10 Things to Build

This is the implementation order for turning the current map prototype into a playable travel loop. It prioritizes connecting the systems that already exist before adding new content layers.

## 1. Seamless chunked local exploration

Expand the local map by loading adjacent regional hexes as neighbouring local-map chunks. A border crossing is one local-hex move (one mile), not a separate six-mile action or a screen transition.

- Add regional hex neighbours for the current flat-top, odd-column layout.
- Draw the current local chunk and its adjacent chunks in one pannable view.
- Map an outgoing local edge and direction to the opposite edge of the destination chunk.
- Update the party marker and regional coordinate without resetting the viewport.
- Add a `Recentre on party` control and reject exits that leave the regional map.

**Done when:** a party can walk across multiple regional hexes while staying in one local-scale, pannable view.

## 2. Encounter and weather check cadence

Connect party movement to a rule-defined terrain event check. The current map movement only counts miles; `TravelService` exists but is not connected to `Campaign` or the UI.

- Decide the cadence: per local hex, per six-mile regional equivalent, or per travel stint.
- Resolve the event against the terrain of the hex being entered.
- Show the roll and result in the travel log/inspector.
- Stop movement when an event requires it.

**Done when:** travel produces terrain-appropriate outcomes without manual developer calls.

## 3. One persisted party model

Merge the map-facing `PartyTravelState` with the generic `TravelParty` / `Traveler` model. The player should have one persisted party whose data drives both map travel and rule resolution.

- Party members, health, skills, conditions, rations, equipment, companions, and exhaustion.
- A single save format and migration path for existing campaign saves.
- UI reads the same party data used by tests and rule engines.

**Done when:** a Wastes effect can change a party member and the map UI immediately reflects the change.

## 4. Inventory and provisions

Rations are counted during rest, but no gameplay system supplies, displays, trades, or loots them. Add the minimum inventory loop.

- Party inventory with stackable resources, starting provisions, and capacity rules if required.
- Ration use on rest; starvation exhaustion when no ration is available.
- Inspector or sidebar display for rations, water, ammunition, and key artifacts.
- Explicit gain/loss events with an audit log.

**Done when:** the party can acquire, spend, and run out of supplies through normal play.

## 5. Real terrain event data

The event engines are generic, but the campaign has no authored weather, terrain encounter, or Wastes-card data.

- Encode the exact Wastes outcomes for totals 2-18 from a verified source.
- Define weather and encounter tables for Wastes, Ruins, Pillars, and Settlements.
- Keep outcomes as data files or content definitions, not UI-specific code branches.
- Include benign/no-event outcomes where the rules require them.

**Done when:** every terrain can produce a complete, inspectable table-driven outcome.

## 6. Encounter presentation and resolution shell

The system can identify combat, a test, or a choice, but the player cannot resolve any of them.

- Modal or side-panel encounter view showing terrain, roll, text, and options.
- Choice controls and skill-test output.
- A paused travel state while an encounter is active.
- Result log, including exact dice and modifiers.

**Done when:** the player can complete a non-combat encounter and resume movement.

## 7. Combat vertical slice

Implement the smallest combat loop needed by Wastes and roaming hazards before building deep tactical features.

- Spawn enemy group from an encounter result.
- Turn order, attack/defence resolution, damage, retreat, victory, and defeat.
- Connect combat outcomes back to party health, conditions, loot, and map position.
- Persist an interrupted combat safely.

**Done when:** a hostile encounter can start, resolve, and return the party to travel.

## 8. Roaming-hazard interaction

Roaming hazards are generated, persisted, moved, and drawn, but the party can enter a marked hex without consequence.

- Detect the party entering a local hex with a hazard die.
- Resolve the die's documented hazard effect.
- Ensure hazards advance only when a travel day advances/rest completes, according to the final rule.
- Show hazard name and outcome in the encounter UI/log.

**Done when:** every visible hazard marker has gameplay impact.

## 9. Travel UI and log

The current buttons prove the rules, but travel needs a clear player-facing state.

- Follow/center camera on party.
- Border-exit arrows and reachable-move highlighting.
- Daily miles, forced-march state, rations, exhaustion, active terrain, and rest status.
- Persistent chronological travel log with encounter, weather, and resource changes.
- Clear distinction between inspecting a map and moving the party.

**Done when:** a player can understand the next valid action without reading source code.

## 10. Dungeon entry and progression

Dungeon maps can be viewed, but the party does not enter or move through them as a game state.

- Require the party to reach the dungeon entrance on the correct local hex.
- Persist party dungeon position and current depth.
- Add grid movement, stairs, return-to-local handling, and encounter hooks.
- Carry the same party, inventory, conditions, and log across map scales.

**Done when:** travel from regional map to local map to dungeon and back is a continuous campaign state.

## Recommended first milestone

Build items **1, 2, and 9** together. That delivers the first real loop:

```text
move one local mile
  -> cross a regional boundary when needed
  -> update daily travel
  -> resolve terrain check at the chosen cadence
  -> show result
  -> continue, forced march, or rest
```

Items 3 through 8 then make that loop consequential; item 10 extends it into the dungeon.
