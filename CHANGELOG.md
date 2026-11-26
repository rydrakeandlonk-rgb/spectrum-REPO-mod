# Changelog

## 0.7.0
- Add a public registration API for modded valuables, enemy setups and non-combat utility items that are not exposed through the game's normal discovery registries.
- Discover modded drones and trackers by their runtime behaviour components rather than requiring vanilla asset names.
- Correct tracker classification so both valuable and extraction trackers can pass the utility catalogue validator.
- Add a conservative validation profile for explicitly registered non-combat physical items; reject known combat component families.
- Refresh the catalogue every 30 seconds to pick up assets and registrations loaded after the initial scan.
- Preserve the existing Control Center category IDs, host-only execution, prefab-reference network spawning and shared spawn executor.
- Document mod author registration and compatibility limits.

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
- Ship only the root-level SpectrumRepoMod.dll in the Nexus/Vortex install archive so Vortex routes it to BepInEx/plugins.
- Keep manifest.json in the repository as build metadata but exclude it from the install archive.
- Exclude README, changelog, and icon from the install archive as well.
- Apply the DLL-only package rule to the local PowerShell build and GitHub Actions release pipeline.
- Keep the plugin inert; only update its version identity.

## 0.3.2
- Reduce the mod to its BepInEx plugin identity metadata only.
- Remove custom startup functionality while retaining package validation.

## 0.3.1
- Package the BepInEx plugin DLL as a standalone archive-root file.
- Stop shipping a BepInEx/plugins directory tree that can cause Vortex to manage the shared plugins directory as a deployment target.

## 0.3.0
- Replace the previous startup implementation with a minimal BepInEx loader-validation plugin.
- Remove game-specific and networking code from the Stage 1 compiled assembly.
- Keep legacy implementation files excluded from that build.

## 0.2.0
- Reworked valuable discovery and spawning around vanilla valuable definitions.
- Removed REPOLib as an installation dependency.
