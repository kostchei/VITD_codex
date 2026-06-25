# Vast Rules Coverage TODO

This checklist tracks what still needs finishing after comparing the implemented
Codex rules against the extracted main text in
`tmp/pdfs/the-vast-in-the-dark-layout.md` and the mechanical index in
`tmp/rules/Vast-Rules-Page-Index.md`.

## Current Coverage Shape

- Map generation from page 10 and `vast-generation.md` is covered by domain
  logic and tests.
- Many main-text rule tables have been transcribed into `src/VastDark.Domain/`,
  but several are table-only and not yet connected to persistent campaign play.
- The presentation layer now exposes settlement and Pillar affordances, but the
  campaign model still needs durable settlement identities, complete encounter
  resolution, and full Ruin/Deep workflows.
- `RuleTableTests` is the first guardrail to build out: it should catch table
  drift, OCR cleanup regressions, bad ranges, and missing edge rows before more
  gameplay is added.

## Suggested Order

- [ ] 1. Fill `RuleTableTests` with page-by-page table assertions.
  - [ ] Pages 6-8: Traveler quirks, inventory/vitality/exhaustion, Harrowing.
  - [ ] Pages 9-12: navigation assets/lost effects, Wastes weather, encounters,
        curiosities, and roaming hazards.
  - [ ] Pages 13-15: Wastes factions, Pillar work, encounters, delves, events,
        and loot.
  - [ ] Pages 16-19: settlement generation, scarcity, services, denizens, and
        settlement factions.
  - [ ] Pages 20-31: Ruin generation, rooms, effects, features, discoveries,
        encounters, creature stats, and treasure.
  - [ ] Pages 32-41: Deep gifts, Minotaur, trials, escape ritual, Rites, spells,
        and Crawl statblocks.
  - [ ] Add explicit tests for known OCR/text cleanup risks such as broken
        multiplication signs, smart quotes, and merged headings.

- [ ] 2. Build the daily travel/day-resolution engine.
  - [ ] Resolve a route/day using navigation, weather, encounters, rations,
        forced march, and rest as one campaign operation.
  - [ ] Persist daily outcomes and expose them through the UI travel log.
  - [ ] Apply Wastes weather effects to party state instead of only reporting
        pending saves or damage.

- [ ] 3. Add persistent settlement generation and services.
  - [ ] Generate and save settlement population, scarcity, atmosphere, locations,
        factions, and denizens per discovered settlement local cell.
  - [ ] Replace the generic `Middling` shop shell with the settlement's actual
        scarcity and remaining purchase limits.
  - [ ] Wire rest, recovery, barter, Raw Lodestone services, faction abilities,
        and denizen obligations into campaign state.

- [ ] 4. Finish Ruin room entry/search/feature/encounter/treasure resolution.
  - [ ] Replace the prototype dungeon fixture with the source Ruin procedure as
        the playable dungeon surface.
  - [ ] Apply room effects, feature rolls, discoveries, encounters, treasure, and
        depth changes to party/campaign state.
  - [ ] Persist Ruin room visit/search state, collapses, shortcuts, and exits.

- [ ] 5. Add a shared encounter/save/combat resolver.
  - [ ] Centralize save prompts, damage application, combat starts, tribute,
        trade, mood outcomes, and choice points.
  - [ ] Use the same resolver for roaming hazards, Wastes encounters, Pillar
        encounters, Ruin encounters, and Crawl creature specials.
  - [ ] Record unresolved player/referee choices explicitly instead of hiding
        them in plain text logs.

- [ ] 6. Treat Deep/Rites/Minotaur as the final campaign layer.
  - [ ] Add a campaign mode for entering the Deep and progressing through trials.
  - [ ] Persist Gifts of the Deep, Minotaur pursuit, trial state, and terminal
        escape outcomes.
  - [ ] Wire Rites and spells to party resources, exhaustion, memory, navigation,
        and encounter systems.
