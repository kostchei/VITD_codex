# Roaming Hazards

This reference adds a moving, shared hazard to a hex map. Roll 1d6 to determine how many dice to place on map hexes. A hex containing a die has a dangerous hazard that units must avoid or deal with.

Each day, roll a direction for every hazard die and move it one hex. The hazards therefore remain on the map as mobile disruptions that players can anticipate and navigate around. If a die collides with another die or leaves the map, re-drop it onto the map.

When a unit ends its movement on a hex with a die, roll that die and resolve the hazard matching the result:

| Die | Hazard | Effect summary |
| --- | --- | --- |
| 1 | Warband | A Demagogue-led band disperses and then attacks/recruits locally. |
| 2 | Healing Columns | A vertical column of light restores the unit's morale; it must give up a random artifact. |
| 3 | Collapse | A building collapses. Units in the hex take 3 damage; if it is a sacred building, the unit must make a difficult saving test or lose a random artifact. |
| 4 | Void Lightning | Lightning harms the unit and affects nearby hexes/buildings, with several escalating effects. |
| 5 | Singing Sand | The unit finds valuable material, then makes a difficult test or loses a random artifact. |
| 6 | Souls | A screaming soul cloud forces the unit to discard a random artifact. If it has none, it loses health instead. |

## Pseudocode

```text
setupRoamingHazards(map):
    hazardCount = rollD6()  // random count from 1 through 6
    repeat hazardCount times:
        die = rollD6()
        hex = chooseRandomHex(map)
        placeDie(die, hex)

eachDay(map):
    for each hazardDie on map:
        direction = chooseRandomDirection()
        target = adjacentHex(hazardDie.hex, direction)

        if target is on map and not target.containsRoamingHazardDie():
            move(hazardDie, target)
        else:
            reDropDieOntoMap(hazardDie, map)

onUnitEncountersHazard(unit, die):
    switch die.value:
        case 1:
            resolveWarband(unit)       // Demagogue event: dispersal, attack/recruitment
        case 2:
            restoreMorale(unit)
            discardRandomArtifact(unit)
        case 3:
            dealDamage(unit, 3)
            if hexHasSacredBuilding(unit.hex) and failsDifficultTest(unit):
                discardRandomArtifact(unit)
        case 4:
            resolveVoidLightning(unit) // damage plus nearby area/building consequences
        case 5:
            grantSingingSandReward(unit)
            if failsDifficultTest(unit):
                discardRandomArtifact(unit)
        case 6:
            if unit.hasArtifact():
                discardRandomArtifact(unit)
            else:
                loseHealth(unit)
```

## Practical reading

Treat the dice as mobile, map-level encounter markers. Their position creates temporary no-stop zones, while landing on one triggers an encounter determined by its face. The detailed outcomes for Warband and Void Lightning have additional scenario-specific rules on the source sheet; the pseudocode intentionally keeps those as dedicated resolution functions.
