# Spectrum R.E.P.O. Mod

Spectrum's R.E.P.O. integration mod.

**Version 0.7.0 introduces mod-aware catalogue discovery and a public registration API.** The catalogue discovers eligible content exposed through the game's loaded asset registries, validates every candidate, and retains the existing vanilla spawn executors and Control Center category IDs.

## Requirements

- R.E.P.O.
- BepInEx 5
- An active Spectrum member account with R.E.P.O. host access in Control Center

## Install with Vortex

1. Install BepInEx 5 through Vortex.
2. Install the Spectrum R.E.P.O. Mod test package.
3. Enable the mod and deploy it in Vortex.
4. Launch R.E.P.O. and press **F8** to open the Spectrum panel.

The install archive contains exactly one file: `SpectrumRepoMod.dll` at the archive root.

## Catalogue discovery

- **Valuables:** discovers valid prefab references from every loaded `LevelValuables` preset and the active level. Modded valuables included in those presets are eligible when they expose a valid `ValuableObject` prefab and resource path.
- **Monsters:** discovers loaded `EnemySetup` assets and the active difficulty registries. Modded setups are included when they expose valid spawn prefab references.
- **Shop utility items:** discovers loaded `Item` assets by verified game behaviours (upgrades, health packs, drones, trackers and supported tools), plus the existing cart identity rules. Names are used for display aliases, not as the sole safety check.
- **Late-loaded content:** the catalogue is rebuilt at level changes and on a bounded 30-second refresh, so mods that finish loading after the initial scan can be picked up.
- **Control Center compatibility:** existing category IDs remain `valuable`, `monster` and `utility_item`. No backend category migration is required.
- **Host authority:** only the current R.E.P.O. host executes shared spawns. The existing spawn executor remains the single spawning pathway.

## Mod author registration API

A mod can register content that is loaded but not exposed through the game's normal registries. Reference `SpectrumRepoMod.dll` and call the public API after its assets are ready:

```csharp
using SpectrumRepoMod.API;

// A valuable must use a real game PrefabRef with a valid resource path.
RepoSpawnRegistry.RegisterValuable("my-mod:crystal-vase", "Crystal Vase", valuablePrefabRef);

// An EnemySetup must contain valid spawnObjects.
RepoSpawnRegistry.RegisterMonster("my-mod:custom-enemy", "Custom Enemy", enemySetup);

// NonCombatPhysical is an explicit assertion by the registering mod author.
// Spectrum still checks ItemAttributes, physicalItem, and known combat components.
RepoSpawnRegistry.RegisterUtilityItem(
    "my-mod:utility-item",
    "My Utility Item",
    itemAsset,
    RepoUtilityItemKind.NonCombatPhysical);
```

Supported profiles are `Upgrade`, `HealthPack`, `Drone`, `Cart`, `Tracker` and `NonCombatPhysical`. Registration IDs should be stable and unique within their category. Re-registering an ID replaces its previous definition; `Unregister(category, stableId)` removes it. Registrations are revalidated whenever the catalogue is rebuilt.

### Compatibility limits

Automatic discovery can only find assets that a mod has loaded and exposed through Unity/game registries. A mod that keeps an asset private, does not provide a usable `PrefabRef`, or does not register a network prefab cannot be made spawnable through reflection alone. Such mods should use the registration API and ensure their multiplayer prefab is registered with the game's networking system.

Unknown shop item types are deliberately not auto-approved. Mod authors can explicitly register a confirmed non-combat physical item; known combat component families are still rejected.

## Validation required

- Confirm vanilla valuables, monsters, upgrades, drones, trackers, carts and tools still appear.
- Test a modded valuable included in a loaded `LevelValuables` preset.
- Test a registered modded valuable in single-player and a private multiplayer lobby.
- Test a modded `EnemySetup` through both automatic discovery and explicit registration where supported.
- Confirm a registered combat item is rejected by the non-combat validation profile.
- Verify Control Center category toggles still block their respective categories.
- Verify late-loaded assets appear after the catalogue refresh.
- Verify reconnect and host-migration behaviour.

This is a hidden testing release. Ordinary main-branch pushes build an installable artifact but do not publish to Nexus. Nexus publishing requires a published GitHub release or an explicit manual workflow opt-in. A successful source commit or CI build does not by itself confirm an in-game compatibility test.
