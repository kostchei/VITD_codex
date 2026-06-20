# VastDark: Map and UI Implementation Plan

## Objective

Build a Windows-first, top-down hex-map prototype in **Godot 4 with C#**. The
prototype must support three map scales—regional, local, and dungeon—and let
the player navigate a dungeon made of independently rendered stacked levels.
The first milestone is a playable map/UI vertical slice, not combat or full
world generation.

## Decisions and constraints

- **Engine and language:** Godot 4 + C#.
- **Initial target:** Windows desktop; produce a Steam-compatible executable
  later. Steamworks integration is deliberately deferred until the core loop is
  stable.
- **Coordinate system:** axial hex coordinates (`q`, `r`) for all maps. Screen
  coordinates are derived only while drawing or hit-testing.
- **Separation of concerns:** plain C# data models hold game state; Godot
  scenes and nodes render that state and send commands to it.
- **Persistence:** all maps and links use stable IDs so save data does not
  depend on scene-node paths.

## Map model

```text
Campaign
├─ RegionalMap (regional hexes)
│  └─ MapLink -> LocalMap
├─ LocalMap (local hexes)
│  └─ MapLink -> Dungeon / DungeonLevel entrance
└─ Dungeon
   ├─ DungeonLevel z = -1
   ├─ DungeonLevel z = 0
   └─ DungeonLevel z = 1
      └─ MapLink -> another DungeonLevel / LocalMap
```

### Core types

- `HexCoord(q, r)`: immutable axial coordinate with neighbour and distance
  operations.
- `MapScale`: `Regional`, `Local`, or `Dungeon`.
- `HexCell`: terrain, movement/visibility flags, optional feature ID, and
  exploration state.
- `HexMap`: map ID, scale, dimensions or sparse-cell storage, and cells keyed
  by `HexCoord`.
- `RegionalMap` and `LocalMap`: `HexMap` specializations with scale-specific
  terrain/features.
- `Dungeon`: stable dungeon ID, display name, and a collection of
  `DungeonLevel` objects keyed by integer depth.
- `DungeonLevel`: one `HexMap`, its depth, and explicit up/down/portal links.
- `MapLink`: source map/cell and destination map/cell. A link owns the
  transition metadata (stairs, entrance, exit, portal), not the renderer.
- `MapViewState`: currently selected map, selected cell, camera position,
  zoom, and active level depth.

Use links rather than attempting to mathematically transform regional cells
into local cells. That keeps authored, procedural, and partially revealed maps
compatible and allows multiple local areas or dungeon entrances per regional
hex.

## UI design

### Persistent shell

- Top bar: current area name, map scale, and placeholder game controls.
- Centre: pan-and-zoom hex-map viewport.
- Left panel: scale switcher (`Regional`, `Local`, `Dungeon`). Disabled when
  the target map cannot be reached from the current location.
- Right panel: selected hex inspector (terrain, features, exits, debug data in
  the prototype).
- Bottom hints: pan, zoom, select, level transition, and return actions.

### Dungeon controls

- Level list or vertical depth selector showing all discovered levels.
- Keyboard actions to use stairs/links when standing on an appropriate cell.
- Distinguish visible stacked levels in the selector; render only the active
  level in the primary viewport for the first milestone.
- A later enhancement may add a minimap/ghost overlay for adjacent levels;
  this is not required for the vertical slice.

### Interaction rules

- Click selects a hex. Double-click or an explicit action moves only after
  movement exists.
- Mouse wheel zooms around the pointer; middle-mouse drag pans.
- Scale changes preserve a meaningful focus point: current entity location,
  selected entrance, or map centre as fallback.
- Map renderer emits semantic input (`HexSelected`, `MapPanned`) and does not
  mutate campaign data directly.

## Delivery phases

### Phase 0 — Project foundation

1. Create a Godot 4 C# project with a predictable source layout:

   ```text
   src/
   ├─ Domain/        # C# map, coordinate, link, and save models
   ├─ Application/   # map navigation and view-state commands
   ├─ Presentation/  # Godot controls, scenes, map renderer
   └─ Tests/         # pure C# domain tests
   assets/
   scenes/
   ```

2. Enable nullable reference types and add a formatter/analyzer configuration.
3. Add a minimal test project for domain logic; renderer tests are manual at
   this stage.

**Exit criterion:** the empty project runs and the domain test suite runs from
the command line.

### Phase 1 — Hex foundation

1. Implement `HexCoord`, axial directions, range, distance, and pixel/hex
   conversion for a chosen pointy-top or flat-top layout. Choose one and keep
   it universal.
2. Implement `HexMap` and `HexCell` with deterministic sample data.
3. Create a reusable `HexMapView` renderer with terrain colours, grid lines,
   selection highlight, panning, and cursor-centred zoom.
4. Add unit tests for coordinate math, hit-testing conversion, map bounds, and
   neighbour lookup.

**Exit criterion:** a user can pan, zoom, and select cells accurately on a
sample hex map.

### Phase 2 — Three-scale navigation

1. Implement `MapLink` and `Campaign` navigation commands.
2. Create one hand-authored regional map, two local maps, and a dungeon
entrance to exercise real links.
3. Implement the scale switcher and selected-cell inspector.
4. Preserve or deliberately reset camera/selection state according to the UI
rules above.

**Exit criterion:** the user can move regional → local → dungeon and back
through explicit links without relying on scene paths or hard-coded UI state.

### Phase 3 — Stacked dungeon levels

1. Implement `Dungeon` and `DungeonLevel` with arbitrary integer depths.
2. Build sample levels with up/down stairs and at least one non-adjacent portal
link.
3. Add the depth selector, discovered-level state, and controlled level
transitions.
4. Add visual entrance/exit markers and selected link details in the
inspector.

**Exit criterion:** the prototype supports at least three stacked levels and
round-trips through stairs while preserving dungeon state.

### Phase 4 — Persistence and content boundary

1. Serialize domain models and navigation/view state to versioned JSON save
files.
2. Load a campaign with regional, local, and stacked dungeon maps.
3. Define content-loading interfaces so JSON-authored and generated maps enter
through the same domain model.
4. Add a migration/version field before saves are shared externally.

**Exit criterion:** a saved campaign restores the active map, active dungeon
level, selection, discovered state, and links correctly.

### Phase 5 — Steam-ready packaging

1. Export a clean Windows build and verify it on a machine without the editor.
2. Add Steamworks behind a small platform-services interface only when there
is a concrete need (launch detection, achievements, cloud saves, etc.).
3. Keep Steam-specific code out of `Domain` and `Application`.
4. Configure Steam depots, store assets, and release branches after the
vertical slice is reliable.

**Exit criterion:** a non-Steam Windows build still runs; enabling Steam is a
platform adapter change, not a map-system rewrite.

## Verification checklist

- Hex-to-screen-to-hex conversion returns the expected cell at grid edges.
- Regional/local/dungeon views use the same coordinate and rendering contract.
- Every map link resolves to an existing destination map and cell.
- Dungeon stair links resolve to valid level depths and reciprocal links where
  the design requires them.
- Loading malformed or old save data fails safely with a useful error.
- Pan, zoom, selection, and scale/depth changes remain usable at the smallest
  supported desktop resolution.
- Exported Windows build starts, saves, and reloads outside the Godot editor.

## Deliberately deferred

- Combat, AI, inventory, quests, and time simulation.
- Procedural world/dungeon generation beyond deterministic sample fixtures.
- Multiplayer and Steam networking.
- Full fog-of-war rendering and adjacent-level overlays.
- Final art, animation, audio, accessibility pass, and controller support.

## Immediate next task

Create the Godot C# project and implement Phase 1 using a single sample map.
Do not start procedural generation or Steam integration until the renderer,
map navigation contract, and stacked-dungeon model have been validated.
