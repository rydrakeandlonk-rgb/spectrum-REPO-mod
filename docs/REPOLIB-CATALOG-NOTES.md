# Vanilla valuable API notes

This document records the decompiled vanilla APIs used by the Spectrum R.E.P.O. Mod valuable-only test build.

## Catalogue source

- `LevelGenerator.Instance.Level.ValuablePresets` provides the presets selected for the current level.
- Each `LevelValuables` preset exposes seven lists: `tiny`, `small`, `medium`, `big`, `wide`, `tall`, and `veryTall`.
- Each list contains vanilla `PrefabRef` values.
- `PrefabRef.IsValid()`, `PrefabName`, `ResourcePath`, `IsFinalized`, and `Prefab` expose the metadata and loading interface needed by the catalogue.
- The game prepares these references through `LevelGenerator.PrefabsPrepare` before its own spawn process.

## Spawn behavior

The vanilla `ValuableDirector` uses the prefab resource path with `PhotonNetwork.InstantiateRoomObject` in multiplayer and instantiates `PrefabRef.Prefab` in single-player. After instantiation it obtains the `ValuableObject` component and calls `DollarValueSetLogic()`.

The Spectrum test executor follows those same network-mode-specific creation paths and calls the same value-initialisation method.

## Runtime constraints

- The catalogue is specific to the current level; it is not a global scan of every valuable in the game.
- Prefab references are prepared before publication so multiplayer resource loading follows the game's expected path.
- The catalogue is rebuilt when the current level changes.
- Only valid prefab references are included, and duplicate resource paths are collapsed.
- All test requests are host-authoritative and restricted to the valuable category.
