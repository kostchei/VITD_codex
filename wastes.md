# Wastes / Decay Engine

This reference describes a card-driven consequence engine for Wasteland-style travel. Draw a Wastes card, resolve the selected numbered entry, then apply any resulting movement, damage, resource loss, encounter, or story effect.

The entries are stateful: they can change party health, water, supplies, weapons, armour, companions, location, and ongoing conditions such as injury, illness, radiation, or an escort obligation.

## Core rule patterns

- **Random resolution:** Roll `1d12 + 1d6` for a total from **2 through 18**. The total selects the matching outcome row on the card; row 1 is not reachable from this roll.
- **Tests and saves:** Skill or attribute checks (for example Intelligence, Strength, Survival, Medicine, Luck, or a combat skill) avoid, reduce, or redirect a penalty.
- **Resource pressure:** Hazards consume water, supplies, ammunition, armour integrity, or weapon condition. Some force an item discard or a return to a settlement.
- **Health and conditions:** Outcomes deal wounds, apply ongoing conditions, or reduce a character statistic. Store conditions separately from immediate damage so later rules can reference them.
- **Encounters:** Results can create NPC, creature, raider, or settlement interactions, which may branch to dialogue, combat, trade, escorting, or a new objective.
- **Combat and escape:** Hostile results can start a fight. Retreat or escape may change position while still imposing damage or loss.
- **Special results:** Some outcomes grant a discovery, treasure, information, shortcut, temporary aid, or story hook rather than a penalty.

## Resolution model

| Phase | Engine responsibility |
| --- | --- |
| Draw | Select or draw a Wastes card and identify its numbered entry. |
| Parse | Identify required rolls, tests, choices, costs, targets, and follow-up instructions. |
| Resolve | Roll `1d12 + 1d6` (2-18), then apply any modifier, test or choice, and consequence. |
| Update | Commit all changes to party and world state, including conditions and spawned encounters. |
| Continue | Start combat, move or return the party, add a hook, or end the encounter as instructed. |

## Movement and daily travel

Travelers on foot can cover **18 miles per 24-hour day** before they need rest. They can push an additional **6 miles per level of exhaustion** gained that day. Each traveler must consume **1 ration per day**; a traveler who cannot do so gains 1 level of exhaustion.

Terrain determines which encounter and weather tables apply. Resolve those events at the campaign's configured travel-event cadence using the terrain that the party is currently crossing. If the party crosses multiple terrain types, resolve each event against the terrain profile for the segment in which it occurs.

| Rule | Effect |
| --- | --- |
| Normal daily movement | Up to 18 miles on foot. |
| Forced movement | +6 miles for each exhaustion level voluntarily gained to keep moving. |
| Food | Consume 1 ration per traveler per day. |
| No ration | Gain 1 exhaustion level for each unfed traveler. |
| Terrain events | Use the current terrain's encounter and weather tables. |

## Pseudocode

```text
resolveWastesEncounter(party, world):
    card = drawWastesCard(world.deck)
    rollTotal = rollD12() + rollD6()  // minimum 2, maximum 18
    entry = card.outcomes[rollTotal]
    log("Wastes: " + card.name + " / " + entry.title)

    context = {
        party: party,
        world: world,
        card: card,
        entry: entry,
        pendingEffects: []
    }

    for each step in entry.steps in listed order:
        result = resolveStep(step, context)
        context.pendingEffects.append(result.effects)

        if result.endsEncounter:
            break

    applyEffectsAtomically(context.pendingEffects, party, world)
    runFollowUps(entry, party, world)
    // Follow-ups may include combat, movement, NPC creation,
    // objectives, forced discards, or a return to settlement.

    return buildEncounterReport(card, entry, context)

travelDay(party, route, world):
    remainingMiles = 18
    dailyTravel = createTravelLog()

    for each traveler in party.members:
        if traveler.rations > 0:
            traveler.rations -= 1
            dailyTravel.log(traveler.name + " consumes 1 ration")
        else:
            addExhaustion(traveler, 1)
            dailyTravel.log(traveler.name + " gains 1 exhaustion (no ration)")

    while remainingMiles > 0 and route.hasNextSegment():
        segment = route.nextSegment()
        terrain = world.terrainAt(segment.location)
        milesMoved = movePartyAlongSegment(party, segment, remainingMiles)
        remainingMiles -= milesMoved
        dailyTravel.miles += milesMoved

        if shouldResolveWeather(segment, world.travelEventCadence):
            resolveWeather(terrain.weatherTable, party, world, dailyTravel)
        if shouldResolveEncounter(segment, world.travelEventCadence):
            resolveTerrainEncounter(terrain.encounterTable, party, world, dailyTravel)

        if party.mustStopTraveling():
            break

    while party.wantsToPushOn() and route.hasNextSegment():
        for each traveler in party.members:
            addExhaustion(traveler, 1)

        extraMiles = 6
        segment = route.nextSegment()
        terrain = world.terrainAt(segment.location)
        milesMoved = movePartyAlongSegment(party, segment, extraMiles)
        dailyTravel.miles += milesMoved

        if shouldResolveWeather(segment, world.travelEventCadence):
            resolveWeather(terrain.weatherTable, party, world, dailyTravel)
        if shouldResolveEncounter(segment, world.travelEventCadence):
            resolveTerrainEncounter(terrain.encounterTable, party, world, dailyTravel)

        if party.mustStopTraveling() or milesMoved < extraMiles:
            break

    requireRest(party)
    return dailyTravel

resolveStep(step, context):
    if step.type == "roll":
        total = rollD12() + rollD6() + getModifier(step, context.party)
        return selectOutcome(step.outcomes, total)

    if step.type == "test":
        score = rollTest(step.skill, step.difficulty, context.party)
        if score >= step.difficulty:
            return step.onPass
        return step.onFail

    if step.type == "choice":
        choice = requestPlayerChoice(step.options)
        return step.outcomes[choice]

    if step.type == "combat":
        return startCombat(step.enemyGroup, context.party, context.world)

    return evaluateRule(step, context)
```

## Implementation notes

- Keep card text and outcome tables as data. The engine should expose generic operations such as `roll`, `test`, `applyDamage`, `loseResource`, `addCondition`, `spawnEncounter`, and `moveParty`.
- Keep terrain as data too: each terrain definition should supply its encounter table, weather table, movement modifiers (if any), and any travel restrictions.
- Validate costs before committing an effect. If a player cannot pay a stated cost, use the card's fallback; if none exists, flag it as a rules-authoring decision rather than allowing negative resources.
- Record the card, rolls, modifiers, choices, and state changes in a resolution log. This keeps tabletop adjudication and software debugging reproducible.
- The supplied image is low resolution. Verify any exact card wording against the original source before implementing individual numbered outcomes verbatim.
