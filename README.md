# Spectrum R.E.P.O. Mod

Spectrum's R.E.P.O. integration mod.

**Version 0.6.0 adds a dedicated Tools pool for approved shop upgrades, drones and C.A.R.T.s.** The F8 panel supports Twitch pairing, the Spectrum Game Mods gateway, run/lobby/host reporting, local test spawns, and live catalogue publication.

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

## 0.6.0 test scope

- Valuables are discovered from all currently loaded vanilla valuable presets and the active level.
- Monsters are discovered from loaded vanilla EnemySetup assets and the active difficulty registries.
- Tools are discovered only from the exact vanilla prefab names for the 12 listed upgrades, 5 listed drones and 2 C.A.R.T.s.
- Upgrade, drone and cart entries are checked against their expected vanilla item components before being marked eligible.
- Tools use the existing Control Center **Shop utility items** category toggle.
- Only the current R.E.P.O. host executes shared spawns.
- Requests are deduplicated locally before execution.
- The F8 panel provides local test buttons for Valuables, Monsters and Tools.
- Twitch reward delivery is not enabled in this test build.

## Validation required

- Confirm all 19 Tools entries appear in Control Center.
- Test at least one upgrade, drone and each C.A.R.T. type in single-player.
- Test Tools in a private multiplayer lobby and confirm every member sees one spawn.
- Verify the Shop utility items toggle blocks Tools requests when disabled.
- Confirm existing valuable and monster spawning still works.
- Verify reconnect and host-migration behaviour.

This is a hidden testing release, not a public feature-complete release.
