# Changelog

## 0.6.0
- Add a separate Tools catalogue for the 12 listed upgrades, 5 listed drones and 2 C.A.R.T.s.
- Discover only the approved shop entries by their exact vanilla prefab names.
- Validate upgrade, drone and physical-item components before allowing a Tools entry to spawn.
- Apply the existing Control Center Shop utility items enable/disable setting.
- Add the Tools catalogue count and local F8 test button.
- Preserve the existing valuable and monster catalogue and spawn paths.
- Retain host-only execution, request deduplication, catalogue publication and DLL-only Vortex packaging.

## 0.5.0
- Expand valuable discovery to all currently loaded vanilla presets, not only the active level.
- Discover loaded vanilla monster setups and active difficulty registries.
- Add host-authoritative monster spawning through the game's public LevelGenerator.EnemySpawn entry point.
- Apply separate Control Center enable/disable settings for valuable and monster categories.
- Add local F8 test controls and catalogue counts for both supported categories.
- Retain DLL-only packaging, host checks, request deduplication, and acknowledgement handling.

## 0.4.1
- Republish the existing 0.4.0 integration test build under a new version number for Vortex/Nexus update detection.
- No gameplay, networking, catalogue, or spawning behaviour changed.

## 0.4.0
- Add an F8 in-game Spectrum connection panel.
- Add Twitch device-code pairing through Spectrum Control Center.
- Save the approved broadcaster identity and private host credential in the local BepInEx configuration.
- Connect to the Spectrum Game Mods WebSocket gateway.
- Report active run, lobby ID and host authority to the backend.
- Publish the current level's vanilla valuable catalogue.
- Add host-authoritative valuable spawning for Control Center test requests.
- Add a local request-deduplication guard.
- Keep the test scope restricted to valuable collectibles; other categories are rejected.
- Keep Twitch reward delivery disabled.
- Build against the uploaded R.E.P.O. Managed assemblies and preserve DLL-only Vortex packaging.
- Test-branch builds produce artifacts without automatically publishing to Nexus Mods.

## 0.3.3
- Ship only the root-level SpectrumRepoMod.dll in the Nexus/Vortex install archive.
- Keep manifest.json in the repository as build metadata but exclude it from the install archive.
- Exclude README, changelog, and icon from the install archive as well.
- Apply the DLL-only package rule to the local PowerShell build and GitHub Actions release pipeline.
- Keep the plugin inert; only update its version identity.

## 0.3.2
- Reduce the plugin to its BepInEx identity metadata only.
- Remove the custom Awake method and startup log message.
- Keep all game, networking, configuration, command, and gameplay functionality absent.
- Retain standalone root-level DLL packaging for Vortex deployment diagnosis.

## 0.3.1
- Package the BepInEx plugin DLL as a standalone archive-root file so Vortex can route it to BepInEx/plugins.
- Stop shipping a BepInEx/plugins directory tree that can cause Vortex to manage the shared plugins directory as a deployment target.
- Add package checks that reject a BepInEx directory in the install archive.
- Keep the loader-only plugin behavior unchanged.

## 0.3.0
- Replace the previous startup implementation with a minimal BepInEx loader-validation plugin.
- Remove game-specific and networking code from the Stage 1 compiled assembly.
- Compile only the minimal plugin entry point; legacy implementation files are retained but excluded from this build.
- Keep the package limited to the Spectrum plugin and standard install metadata.

## 0.2.0
- Reworked valuable discovery and spawning around vanilla valuable definitions.
- Removed REPOLib as an installation dependency.
