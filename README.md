# Spectrum R.E.P.O. Mod

Spectrum's R.E.P.O. integration mod.

**Version 0.3.1 is a Stage 1 loader-validation build.** This deliberately minimal build only verifies that BepInEx can discover and initialise the Spectrum plugin. It does not connect to Spectrum, access game systems, inspect valuables, spawn anything, or display an in-game UI.

## Requirements

- R.E.P.O.
- BepInEx 5

## Install with Vortex

1. Install BepInEx 5 through Vortex.
2. Install Spectrum R.E.P.O. Mod through Vortex from the [Spectrum R.E.P.O. Mod Nexus page](https://www.nexusmods.com/games/repo/mods/401).
3. Enable Spectrum R.E.P.O. Mod and deploy it in Vortex.
4. Vortex should deploy the standalone plugin DLL into `BepInEx/plugins`. The package intentionally does not include a `BepInEx` directory, so Vortex manages the plugin file rather than replacing the shared plugins directory.
5. Launch R.E.P.O.

## Stage 1 verification

After launching, check `BepInEx/LogOutput.log` for:

```
Spectrum R.E.P.O. Mod 0.3.1 loaded. Stage 1 loader proof.
```

The game should reach its normal menu. If it does not, retain the complete log and keep the mod disabled until the failure is investigated.

## Rebuild plan

The mod will be reintroduced in isolated stages: game-ready detection, vanilla valuable catalogue discovery, valuable-only spawning, Spectrum gateway and Twitch pairing, then the in-game connection UI. Each stage must pass an in-game startup test before the next is added.
