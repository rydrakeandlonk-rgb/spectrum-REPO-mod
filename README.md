# Spectrum R.E.P.O. Mod

Spectrum's host-side R.E.P.O. integration test plugin. It connects one Spectrum broadcaster's game instance to the Spectrum Game Mods gateway, publishes the live REPOLib catalogue, receives Control Center test requests, and executes approved category tests on the current lobby host.

**This is an early testing build. Twitch Channel Point redemptions and non-host multiplayer forwarding are not enabled.** Manual tests are sent from the admin-only R.E.P.O. page in Spectrum Control Center.

## Requirements

- R.E.P.O.
- BepInEx 5 (installed automatically by Thunderstore Mod Manager)
- REPOLib (installed automatically by Thunderstore Mod Manager)
- A Spectrum streamer account with an administrator-generated R.E.P.O. host credential

## Install

Install **Spectrum R.E.P.O. Mod** through Thunderstore Mod Manager. Launch R.E.P.O. once so BepInEx creates the plugin configuration file, then close the game.

## Connect your Spectrum channel

1. Open Spectrum Control Center and go to **Games → R.E.P.O.**
2. Find your broadcaster row and select **Generate host credential**.
3. Copy the gateway URL and one-time host token shown in the page.
4. Open `BepInEx/config/com.spectrum.repomod.cfg`.
5. Under `[Spectrum Gateway]`, set `GatewayUrl` and `HostToken` to the values from Control Center.
6. Save the file and launch R.E.P.O. as the lobby host.

The token is private. Do not post it in chat, screenshots, logs or public support requests. If it is lost, an administrator can generate a replacement; the previous token is invalidated.

## Test in game

1. Start a run as the R.E.P.O. lobby host.
2. In Control Center → Games → R.E.P.O., refresh the page.
3. Confirm your channel shows **Connected**, **Run active**, **Lobby host**, and catalogue counts.
4. Use **Test monster**, **Test utility item**, and **Test valuable** one at a time.
5. Check the game and the session's recent command result in Control Center.

The current test build only executes a manual test on the authenticated mod connection when that instance is the active lobby host. If no run is active, the host is not authoritative, or a category has no eligible entries, the request is rejected and reported.

## Safety and catalogue rules

- The utility pool is allow-list based: only explicitly recognized non-weapon utility classifications are eligible.
- Unknown shop-item types fail closed.
- The global weapon exclusion is enforced in the game-side policy and cannot be removed through configuration.
- Category exclusions and category enable/weight settings are managed in Control Center.
- The final policy is checked again immediately before a spawn.

## Current limitations

- Twitch reward delivery is deliberately disabled pending end-to-end validation.
- Non-host participant forwarding to the lobby Master Client is not implemented in this test build.
- The live game adapter must be compiled and verified against the current local R.E.P.O. assemblies before the first package is considered validated.
- Modded multiplayer catalogues depend on the content being installed on the authoritative host.

## Troubleshooting

- **No config file:** launch the game once with BepInEx installed, then close it.
- **Gateway stays offline:** check the URL/token values, confirm the credential has not been rotated, and inspect `BepInEx/LogOutput.log`.
- **Connected but no catalogue:** start a run and refresh Control Center. The plugin waits for the game's run registries before publishing.
- **Test rejected:** confirm the active run is hosted by the connected broadcaster and that the selected category has eligible entries.

## Development build

On a Windows machine with R.E.P.O. installed:

```powershell
.\build-thunderstore.ps1 -GamePath "C:\Program Files (x86)\Steam\steamapps\common\REPO"
```

The script compiles against the local game-managed assemblies and creates `dist/Spectrum_REPO_Mod-0.1.0.zip`. Game assemblies are not included in the package or source repository.
