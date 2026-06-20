# Generating the Vast

This procedure has two ordered stages. Generate the regional map first, then select one regional hex and generate its local map.

- **Regional scale:** each hex represents roughly **6 miles** and identifies the dominant terrain in that area.
- **Local scale:** select one regional hex and subdivide it into a smaller hex map; each local hex represents roughly **1 mile** and identifies locations within that 6-mile area.

`Regional.png` is the blank regional-scale grid to use for the first pass.

## Step 1: Generate the regional map

1. Use the regional hex grid (one hex = 6 miles).
2. Roll and place **8 six-sided dice (d6)** into **8 different, randomly selected hexes**. Each die occupies exactly one hex; never place more than one die in a hex.
3. For every hex containing a die, record terrain from its face-up value:

   | d6 | Terrain | Meaning |
   |---:|---|---|
   | 1 | Wastes | Barren dry dust and sand, prone to sandstorms and with little to find. |
   | 2-4 | Ruins | Hives of organic/industrial architecture, settlements populated with ruins. |
   | 5-6 | Pillars | Enormous stone towers stretching for miles; their tops are lost beyond the ceiling. |

4. Mark every unoccupied hex as **Wastes**.

## Step 2: Generate a local map

Choose one terrain result from Step 1 to explore. Treat that single 6-mile regional hex as a new small hex map where each hex is approximately 1 mile.

1. Select one 6-mile regional hex.
2. Roll one d6 to determine how densely populated this local map is.
3. Draw or use a local hex map, then place the indicated number of d6 into distinct local hexes.
4. Convert each die to terrain according to the selected regional hex type.
5. Fill any unoccupied local hexes with the default terrain for that area.

For a Pillars regional hex, still make the density roll and place the resulting dice, but every local hex remains a Pillar structure; the Ruins/Wastes table does not apply.

| Local density roll (d6) | Density | Dice placed on the local map |
|---:|---|---:|
| 1-3 | Barren | 6 |
| 4-5 | Standard | 12 |
| 6 | Dense | 32 |

### Local terrain table

For each local die, choose the column that matches the selected regional hex's terrain.

| d6 | Ruins regional hex | Wastes regional hex |
|---:|---|---|
| 1 | Wastes | Wastes |
| 2-4 | Ruins | Wastes |
| 5 | Settlements | Wastes |
| 6 | Settlements | Ruins |

#### Pillars regional hex

Pillar hexes are treated as completely filled with massive skyscrapers and columns. They are still explorable; the local map describes routes, structures, and discoveries within that vertical built environment rather than open terrain.

## Pseudocode

```text
function generateRegionalMap(grid):
    DICE_COUNT = 8
    selectedHexes = chooseRandomDistinctHexes(grid, DICE_COUNT)
    placeOneD6InEach(selectedHexes)

    for each hex in grid:
        if hex has a die:
            roll = hex.die.faceUp
            if roll == 1:
                hex.terrain = WASTES
            else if roll in [2, 3, 4]:
                hex.terrain = RUINS
            else:
                hex.terrain = PILLARS
        else:
            hex.terrain = WASTES

    return grid


function generateLocalMap(regionalHex, localGrid):
    densityRoll = rollD6()
    if densityRoll in [1, 2, 3]: diceCount = 6
    else if densityRoll in [4, 5]: diceCount = 12
    else: diceCount = 32

    selectedHexes = chooseRandomDistinctHexes(localGrid, diceCount)
    placeOneD6InEach(selectedHexes)

    for each localHex in localGrid:
        if localHex has no die:
            localHex.terrain = regionalHex.terrain == PILLARS ? PILLAR_STRUCTURE : WASTES
            continue

        roll = localHex.die.faceUp
        if regionalHex.terrain == PILLARS:
            localHex.terrain = PILLAR_STRUCTURE
        else if regionalHex.terrain == RUINS:
            if roll == 1: localHex.terrain = WASTES
            else if roll in [2, 3, 4]: localHex.terrain = RUINS
            else: localHex.terrain = SETTLEMENTS
        else if regionalHex.terrain == WASTES:
            if roll == 6: localHex.terrain = RUINS
            else: localHex.terrain = WASTES

    return localGrid
```

## Interpretation

The process is a quick random-map generator. Dice placement determines **where** unusual terrain appears; the die value determines **what** it is. The regional map provides travel-scale structure, while a local map zooms into a single regional hex for exploration-scale detail.
