# Spectrum R.E.P.O. Mod

Spectrum's R.E.P.O. integration mod.

**Version 0.4.1 is a version-only republish of the connected 0.4.0 integration test build.** No functionality changed. It adds an in-game F8 panel, Twitch device-code pairing, the Spectrum Game Mods gateway connection, live lobby/run/host reporting, a current-level valuable catalogue, and host-authoritative valuable test spawning.

## Requirements

- R.E.P.O.
- BepInEx 5
- An active Spectrum member account with R.E.P.O. host access in Control Center

## Install with Vortex

1. Install BepInEx 5 through Vortex.
2. Install the Spectrum R.E.P.O. Mod test package.
3. Enable the mod and deploy it in Vortex.
4. Launch R.E.P.O. and press **F8** to open the Spectrum panel.

The install archive contains exactly one file: `SpectrumRepoMod.dll` at the archive root. It does not include a manifest, documentation, icon, or a BepInEx directory.

## Connect Twitch

1. Open the F8 panel.
2. Select **Connect Twitch**.
3. The Control Center page opens with a short approval code.
4. Sign in to the Twitch account that should own this game session and approve the pairing.
5. Return to the game. The mod saves its private host credential in the local BepInEx configuration and connects to the gateway.

The host credential is private. Do not post or share the contents of the mod configuration file.

## 0.4.0 test scope

- Valuable collectibles are the only supported spawn category.
- Monster and utility-item requests are rejected locally.
- The catalogue is built from the valuable presets selected for the current level.
- Only the current R.E.P.O. host executes a shared spawn.
- Multiplayer spawns use Photon room-object instantiation.
- Requests are deduplicated locally before execution.
- The F8 panel includes a local test button for the current host.
- Control Center test requests are routed through the connected host session.
- Twitch reward delivery is not enabled in this test build.

## Validation still required

- Confirm the F8 panel loads in the user's installed game.
- Pair Rydrake and Lonk accounts and verify both appear in Control Center.
- Confirm run, lobby ID and host status update while entering/leaving sessions.
- Test local single-player spawning.
- Test a Control Center valuable request in a private multiplayer lobby.
- Confirm all lobby members see one spawned valuable.
- Test host migration and reconnect behavior before expanding functionality.

This is a testing build, not a public feature-complete release.
