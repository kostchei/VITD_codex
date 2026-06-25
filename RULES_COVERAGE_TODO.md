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

- [~] 1. Fill `RuleTableTests` with page-by-page table assertions.
  - [~] Pages 6-8: Traveler quirks asserted in `RuleTableTests`; inventory,
        vitality/exhaustion, and Harrowing remain covered by `TravelerRuleTests`
        rather than the table guardrail.
  - [x] Pages 9-12: navigation assets/lost effects, Wastes weather, encounters,
        curiosities, and roaming hazards.
  - [x] Pages 13-15: Wastes factions, Pillar work, encounters, delves, events,
        and loot.
  - [x] Pages 16-19: settlement generation, scarcity, services, denizens, and
        settlement factions.
  - [x] Pages 20-31: Ruin rooms (absent 32, duplicate 45), room effects with
        saves/damage, features (absent 24, duplicate 25), discoveries,
        encounters/stat blocks, and depth-banded treasure.
  - [x] Pages 32-41: Deep gifts, Minotaur and Touch effects, trials, escape
        ritual ordering, Rite ledger, Rite spells/schools, and Crawl statblocks.
  - [x] Add explicit tests for known OCR/text cleanup risks such as broken
        multiplication signs, smart quotes, and merged headings
        (`TextStaysFreeOfOcrArtifacts`).

- [~] 2. Build the daily travel/day-resolution engine.
  - [~] `DayResolutionService` resolves navigation, Wastes weather, and the
        weather-modified encounter as one operation, and `Campaign.TryRestParty`
        ties it to rations, rest, and hazard advance. Still outstanding:
        Travelers do not yet carry navigation assets (none are supplied), and
        movement remains hex-by-hex rather than a single route/day call.
  - [x] Persist daily outcomes: the day's log lines and pending decisions are
        appended to the campaign travel log (persisted to the save).
  - [x] Apply Wastes weather effects to party state: Wind Blast damage is now
        applied to Travelers; Breath saves and buried Travelers are recorded as
        explicit pending decisions instead of silent reports.

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

- [~] 5. Add a shared encounter/save/combat resolver.
  - [x] Centralize save prompts, damage application, combat starts, tribute,
        trade, mood outcomes, and choice points in `EncounterResolver`
        (`EncounterResolution.cs`).
  - [x] Use the same resolver for roaming hazards, Wastes encounters, Pillar
        encounters, Ruin encounters, and direct Crawl encounters;
        `TravelInterruptionResolver` now delegates its hazard path to it.
  - [x] Record unresolved player/referee choices explicitly via the
        `PendingDecision` hierarchy (saves, combat, tribute via mood, trade,
        mood, and referee choices) instead of plain log text.
  - [ ] Migrate the remaining `Campaign` Wastes/Pillar/Ruin encounter call sites
        and the presentation `EncounterScreen` onto `EncounterResolution` so the
        UI renders structured decisions rather than the legacy summary string.

- [ ] 6. Treat Deep/Rites/Minotaur as the final campaign layer.
  - [ ] Add a campaign mode for entering the Deep and progressing through trials.
  - [ ] Persist Gifts of the Deep, Minotaur pursuit, trial state, and terminal
        escape outcomes.
  - [ ] Wire Rites and spells to party resources, exhaustion, memory, navigation,
        and encounter systems.
