# Spectrum R.E.P.O. Mod

Spectrum's R.E.P.O. integration mod.

**Version 0.3.3 is an inert deployment-diagnostic build.** It contains only the BepInEx plugin identity (GUID, name, and version). It has no custom startup method, logging, game access, configuration, networking, commands, event handlers, or gameplay functionality.

## Requirements

- R.E.P.O.
- BepInEx 5

## Install with Vortex

1. Install BepInEx 5 through Vortex.
2. Install Spectrum R.E.P.O. Mod through Vortex from the [Spectrum R.E.P.O. Mod Nexus page](https://www.nexusmods.com/games/repo/mods/401).
3. Enable Spectrum R.E.P.O. Mod and deploy it in Vortex.
4. The 0.3.3 install archive contains exactly one file: `SpectrumRepoMod.dll` at the archive root. It does not include the Thunderstore manifest, documentation, icon, or a BepInEx directory tree.
5. Launch R.E.P.O.

## What this build tests

This build isolates Vortex deployment to a single standalone plugin DLL. The repository manifest remains available for build/version metadata but is deliberately excluded from the install archive.

The plugin class must still be discovered by BepInEx for this test to be meaningful. It does not perform any custom work after discovery, and no Spectrum startup message is expected in LogOutput.log.
