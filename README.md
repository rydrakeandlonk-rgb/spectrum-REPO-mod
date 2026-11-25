# Spectrum R.E.P.O. Mod

Spectrum's R.E.P.O. integration mod.

**Version 0.3.2 is an inert deployment-diagnostic build.** It contains only the BepInEx plugin identity (GUID, name, and version). It has no custom startup method, logging, game access, configuration, networking, commands, event handlers, or gameplay functionality.

## Requirements

- R.E.P.O.
- BepInEx 5

## Install with Vortex

1. Install BepInEx 5 through Vortex.
2. Install Spectrum R.E.P.O. Mod through Vortex from the [Spectrum R.E.P.O. Mod Nexus page](https://www.nexusmods.com/games/repo/mods/401).
3. Enable Spectrum R.E.P.O. Mod and deploy it in Vortex.
4. The archive contains the standalone plugin DLL at its root, not a BepInEx directory tree.
5. Launch R.E.P.O.

## What this build tests

This build exists to test whether deploying a practically empty BepInEx plugin still causes the existing plugins directory or other installed plugins to be displaced. It intentionally does not write a startup log message. Check BepInEx's plugin discovery list and the deployed DLL itself to establish whether it is present; do not expect a Spectrum startup message in LogOutput.log.

The plugin class must still be discovered by BepInEx for this test to be meaningful. It does not perform any custom work after discovery.

