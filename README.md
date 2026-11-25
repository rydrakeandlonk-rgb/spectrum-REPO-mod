# Spectrum R.E.P.O. Mod

Spectrum's R.E.P.O. integration mod.

**Version 0.5.0 adds broader valuable discovery and vanilla monster catalogue/spawning.** The F8 panel supports Twitch pairing, the Spectrum Game Mods gateway, run/lobby/host reporting, local test spawns, and live catalogue publication.

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

## Connect Twitch

1. Open the F8 panel.
2. Select **Connect Twitch**.
3. Approve the displayed code in Control Center.
4. Return to the game. The mod saves its private host credential locally and connects to the gateway.

The host credential is private. Do not post or share the contents of the mod configuration file.

## 0.5.0 test scope

- Valuables are discovered from all currently loaded vanilla valuable presets and the active level.
- Monsters are discovered from loaded vanilla EnemySetup assets and the active difficulty registries.
- Monster spawning uses the game's public EnemySpawn routine so vanilla setup and multiplayer room-object handling are retained.
- Only the current R.E.P.O. host executes shared spawns.
- Control Center category enable/disable settings are applied separately to valuables and monsters.
- Requests are deduplicated locally before execution.
- The F8 panel provides local test buttons for both supported categories.
- Utility-item spawning is not included in this build; its registry and runtime path still need separate verification.
- Twitch reward delivery is not enabled in this test build.

## Validation required

- Confirm the expanded catalogue appears in Control Center.
- Test valuable and monster spawns in single-player.
- Test both categories in a private multiplayer lobby and confirm every member sees one spawn.
- Verify category disable settings are respected.
- Verify reconnect and host-migration behaviour.

This is a hidden testing release, not a public feature-complete release.
