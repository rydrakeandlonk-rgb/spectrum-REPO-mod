# Spectrum R.E.P.O. Mod

Spectrum's host-side R.E.P.O. integration. It connects a broadcaster's game instance to the Spectrum Game Mods gateway, publishes the current level's vanilla valuable catalogue, receives Control Center test requests, and executes valuable spawns on the current lobby host.

**Version 0.2.0 is the vanilla-valuable rework test build.** REPOLib is no longer required. Twitch Channel Point reward delivery and non-host multiplayer forwarding remain disabled.

## Requirements

- R.E.P.O.
- BepInEx 5

Install BepInEx 5 through Vortex, then install **Spectrum R.E.P.O. Mod** through Vortex from the [Spectrum R.E.P.O. Mod Nexus page](https://www.nexusmods.com/games/repo/mods/401). REPOLib is not a dependency.

## Install with Vortex

1. Open the Spectrum R.E.P.O. Mod page on Nexus Mods.
2. Use the page's Vortex download option.
3. Allow Vortex to install the mod.
4. Deploy the mod in Vortex, then launch R.E.P.O.

## Connect to Twitch

The first time the mod loads without a saved connection, its Spectrum connection panel opens automatically. You can also open or close it with **F8**.

1. Select **Connect to Twitch** in the in-game Spectrum panel.
2. The mod opens the Spectrum Control Center in your browser and displays a short approval code.
3. Sign in with the Twitch account that owns the Spectrum channel and approve the R.E.P.O. connection.
4. Return to the game. The mod receives and saves its private host credential automatically, then connects to Spectrum.

You do not need to generate a credential in the dashboard or edit the BepInEx config file. The credential is stored locally by the mod and is never displayed in the browser. To move the connection to another Twitch account, revoke the existing host access in Control Center and pair again.

## Valuable catalogue and spawning

The mod reads the valuable presets selected by the current vanilla level. It collects valid prefab references from the seven vanilla size groups and prepares those references through the game's own `LevelGenerator.PrefabsPrepare` flow.

The initial test build supports **valuable requests only**. Other categories are rejected locally, regardless of older Control Center settings.

Valuable spawning follows the vanilla game's network mode:
- Single-player instantiates the loaded prefab.
- Multiplayer creates a room object using the prefab's resource path.

The host validates the selected catalogue entry immediately before spawning and initialises the spawned valuable's value using the game's own `DollarValueSetLogic` method.

## Test the connection and catalogue

1. Start a run as the R.E.P.O. lobby host.
2. Open Control Center → Games → R.E.P.O. and select **Refresh**.
3. Confirm your channel shows **Connected**, **Run active**, **Lobby host**, and a valuable catalogue count.
4. Send a valuable test request.
5. Check the session's recent command result in Control Center.

## Current limitations

- Twitch reward delivery is deliberately disabled pending end-to-end validation.
- Non-host participant forwarding to the lobby Master Client is not implemented in this test build.
- Modded valuable content must be available to the authoritative host and supported by the game's resource-loading path.
- The catalogue refreshes when the current level changes.
- This is a test build, not a final release configuration.

## Troubleshooting

- **Connection panel not visible:** press **F8**.
- **Pairing code expired:** select **Connect to Twitch** again to request a fresh code.
- **Saved connection is offline:** the mod automatically retries its saved connection.
- **Connected but no catalogue:** start a run and allow the level to finish generating; the plugin waits for the level's valuable presets before publishing.
- **Test rejected:** confirm the connected broadcaster is the active lobby host and that the request is for a valuable.
- **Vortex does not show the mod:** confirm the Nexus page is open in Vortex and that deployment completed.

## Development build

On a machine with .NET 8 installed:

~~~powershell
.\build-repo-mod.ps1
~~~

The script compiles the plugin, runs the automated tests, and creates an installable ZIP under `dist`. Game assemblies and local host credentials are not included in the package.
