# VastDark map generator

VastDark is a Godot 4 / C# hex-map prototype. It generates a travel-scale
regional map, creates detailed local maps on demand, and persists the campaign
between game sessions.

## Generation rules

- Regional map: 10 x 8 flat-topped hexes; one regional hex represents 6 miles.
- On creation, exactly 8 d6 are assigned to 8 distinct regional hexes.
  - `1` = Wastes
  - `2-4` = Ruins
  - `5-6` = Pillars
- Local map: 1-mile subhexes within one selected regional hex.
  - A d6 determines local density: `1-3` = 6 dice, `4-5` = 12 dice, `6` = 32 dice.
  - Ruins and Wastes use their own local terrain tables; Pillars remain entirely
    Pillar structures.

The full rules and pseudocode are in [vast-generation.md](vast-generation.md).

## Run

1. Install Godot 4 with .NET support and the .NET 8 SDK specified in
   [global.json](global.json).
2. Import [project.godot](project.godot) in Godot.
3. Run the project.

Use `Regional`, `Local`, and `Dungeon` to change map scales. Click a hex to
inspect it; middle-drag pans and the mouse wheel zooms. Use
`Campaign -> New regional map` to replace the current generated campaign.

## Campaign saves

At startup, the game loads `user://vastdark-campaign.json`. If it does not
exist, a regional map is generated and saved immediately. Local maps are
generated and persisted when first opened. Godot resolves `user://` to the
current user's application-data directory, outside this repository.

## Verify

```powershell
dotnet run --project tests/VastDark.Domain.Tests/VastDark.Domain.Tests.csproj
dotnet build VastDark.csproj
```

## Repository layout

- `src/VastDark.Domain/` - map generation, map state, persistence, and navigation.
- `src/VastDark.Presentation/` - Godot UI and map rendering.
- `tests/VastDark.Domain.Tests/` - executable domain checks.
- `scenes/` - Godot scenes.
