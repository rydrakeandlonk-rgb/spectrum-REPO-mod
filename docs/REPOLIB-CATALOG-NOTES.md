# REPOLib catalogue validation notes

The R.E.P.O. plugin uses REPOLib as its supported catalogue and network-spawn boundary.

## Verified public API surface

- `REPOLib.Modules.Enemies.AllEnemies` exposes the current game's enemy setups.
- `REPOLib.Modules.Items.AllItems` exposes the current `StatsManager` item catalogue.
- `REPOLib.Modules.Valuables.AllValuables` exposes the active run's combined valuable presets.
- `Items.SpawnItem` and `Valuables.SpawnValuable` enforce Master Client / singleplayer execution.
- `Enemies.SpawnEnemy` performs network-prefab spawning; Spectrum must still enforce its own host-authority check before calling it.
- REPOLib also exposes `NetworkedEvent`, but its event code is dynamically allocated from each client's locally registered event list. Do not assume that registration order or event-code assignment is stable across different mod profiles. Validate the actual transport and registration behaviour before depending on it.

## Catalogue constraints

`Items.AllItems` is a game item catalogue, not proof that every returned entry is a shop-available utility item. The adapter must classify each shop entry before it can enter the utility pool. Unknown entries fail closed. The selection core accepts only entries explicitly classified as utility items; entries explicitly excluded or not yet classified are not selected.

`Valuables.AllValuables` depends on an active run (`RunManager.instance`). Catalogue refresh must therefore happen after the run is available and be invalidated when the run/level changes.

`Enemies.AllEnemies` depends on `EnemyDirector.instance`; an empty list before game initialization is not a permanent empty catalogue.

## Remaining runtime verification

- Confirm the installed R.E.P.O. version and REPOLib 4.5.1 API compatibility against the user's local managed assemblies.
- Inspect actual item metadata/components to establish a reliable utility classification for the complete shop catalogue.
- Verify spawn placement and host/client forwarding in a private multiplayer session.
- Verify that the selected catalogue snapshot is consistent for the active run and is refreshed after level transitions.

No game assemblies or local player files are committed to this repository.