Include ..\AGENTS.md

# The Beaver Retriever — Mod-Specific Agent Instructions

## Identity
- **Assembly:** `thebeaverretriever`
- **Namespace:** `Calloatti.TheBeaverRetriever`
- **Framework:** Harmony
- **Publicizer:** includes `Timberborn.Wandering`
- **ModId:** `Calloatti.TheBeaverRetriever`
- **Min Game Version:** 1.0.0.0 — uses `timberborn-decompiled-1.0.*`

## What This Mod Does

When a beaver's path to its district is cut off (flooding, demolished walkways, etc.), the beaver becomes stranded and wanders until it dies. This mod rescues them:

1. **Tracks each beaver's last assigned district** via `UnstuckHelpers._previousDistricts` (static `Dictionary<Citizen, DistrictCenter>`)
2. **Detects stranded beavers** by patching `StrandedRootBehavior.Decide`
3. **Skips rescue** if the beaver is already on globally reachable ground (handles false positives)
4. **Teleports** the beaver toward its previous district (if available) or the nearest district center
5. **Cleans up** the dict entry when the beaver dies via `Character.KillCharacter` patch
6. **Resets the entire dict** on game unload via `RetrieverGameState.Dispose()`

## Source Architecture (`Version-1.0/Source/`)

| File | Role |
|---|---|
| `ModStarter.cs` | Entry point — `IModStarter`, calls `Harmony.PatchAll()` |
| `TBRPatches.cs` | Four Harmony patches (tracking + rescue + death cleanup) |
| `TBRModule.cs` | DI wiring, game lifecycle, and all helper logic |

### TBRPatches.cs — Harmony Patches

Patches are ordered in beaver lifecycle order:

| Class | Patched Method | Type | What it does |
|---|---|---|---|
| `CitizenAssignPatch` | `Citizen.AssignDistrict` | Postfix | Saves the newly assigned district as the "previous" district |
| `CitizenUnassignPatch` | `Citizen.UnassignDistrictIfCutOff` | Prefix | Before the district is removed, saves the current district (so we know where they came from) |
| `StrandedRootBehaviorPatch` | `StrandedRootBehavior.Decide` | Prefix | If citizen is stranded and unassigned: skip if on reachable ground, then try to teleport to previous or nearest district |
| `CharacterKillPatch` | `Character.KillCharacter` | Postfix | Removes the beaver's entry from the district tracking dict on death |

### TBRModule.cs — Helpers & Wiring

| Class | Role |
|---|---|
| `RetrieverConfigurator` (`[Context("Game")]`) | Binds `RetrieverGameState` as a singleton via Bindito DI |
| `RetrieverGameState : IDisposable` | Clears the entire `_previousDistricts` dict when the game context unloads |
| `UnstuckHelpers` (static) | Manages the `Dictionary<Citizen, DistrictCenter>` and contains the teleport/rescue algorithm |

### UnstuckHelpers Detail

- **Dict methods:** `SetPreviousDistrict`, `TryGetPreviousDistrict`, `HasPreviousDistrict`, `ClearPreviousDistrict`, `ClearAll`
- **`TryFindReachableTowardDistrict`**: Tries preferred district first, then falls back to nearest by `DistanceToCitizen`
- **`TryFindReachableTowardSingleDistrict`**: Steps along the direction vector from beaver toward district center, checking z-levels for a globally reachable tile. Falls back to the district center's own position.
- **`TeleportAndAssignCitizen`**: Sets transform + CharacterModel position, stops walker, and calls `AssignDistrict`
- **`MaxZ = 32`**: Upper bound for vertical search

## Key Behaviors

- Dict entry is **never cleared on district unassign** — we keep it so we know where to send them back
- Dict entry **is cleared on death** (prevents stale references)
- Dict is **fully cleared on game unload** via `IDisposable`
- Rescue is **skipped** if the beaver is already on reachable ground for any district
- Preferred (previous) district is checked first; nearest district is the fallback
- No debug logging in release code

## Build & Deploy

- Build: `dotnet build "The Beaver Retriever.csproj"`
- Output deploys automatically to `C:\Users\calloatti\Documents\Timberborn\Mods\The Beaver Retriever\Version-1.0\`
- Runtime logs: `C:\Users\calloatti\AppData\LocalLow\Mechanistry\Timberborn\Player.log`

## Hard Rule
DO NOT EVER TOUCH THE DEPLOY FOLDER.

BUILD DOES EVERYTHING, NEVER EVER MESS WITH THE DEPLOY PROCESS.
