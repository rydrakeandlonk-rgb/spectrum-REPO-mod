# Spectrum R.E.P.O. Mod

Spectrum's host-side R.E.P.O. integration. It connects a broadcaster's game instance to the Spectrum Game Mods gateway, publishes the live REPOLib catalogue, receives Control Center test requests, and executes them on the current lobby host.

**This is an early testing build (0.1.3).** Twitch Channel Point redemptions and non-host multiplayer forwarding are not enabled. Manual tests are sent from the admin-only R.E.P.O. page in Spectrum Control Center.

## Requirements

- R.E.P.O.
- BepInEx 5
- REPOLib

Make sure BepInEx 5 and REPOLib are installed for R.E.P.O. Then install **Spectrum R.E.P.O. Mod** through Vortex from the [Spectrum R.E.P.O. Mod Nexus page](https://www.nexusmods.com/games/repo/mods/401).

## Install with Vortex

1. Open the Spectrum R.E.P.O. Mod page on Nexus Mods.
2. Use the page's Vortex download option.
3. Allow Vortex to install the mod and its listed dependencies.
4. Deploy the mod in Vortex, then launch R.E.P.O.

## Connect to Twitch

The first time the mod loads without a saved connection, its Spectrum connection panel opens automatically. You can also open or close it with **F8**.

1. Select **Connect to Twitch** in the in-game Spectrum panel.
2. The mod opens the Spectrum Control Center in your browser and displays a short approval code.
3. Sign in with the Twitch account that owns the Spectrum channel and approve the R.E.P.O. connection.
4. Return to the game. The mod receives and saves its private host credential automatically, then connects to Spectrum.

You do not need to generate a credential in the dashboard or edit the BepInEx config file. The credential is stored locally by the mod and is never displayed in the browser. To move the connection to another Twitch account, revoke the existing host access in Control Center and pair again.

## Initial test-build restrictions

This first Vortex test build deliberately allows **valuables only**.

- Monster requests are disabled.
- All shop-item requests are disabled while the catalogue classification remains unverified.
- These two restrictions are enforced by the mod even if older category settings in Control Center are still enabled.
- Valuables remain available for controlled end-to-end testing.

The live catalogue can still be inspected in Control Center. Disabled entries should be reported as ineligible, and requests for disabled categories should be rejected by the mod.

## Test the connection and catalogue

1. Start a run as the R.E.P.O. lobby host.
2. Open Control Center → Games → R.E.P.O. and select **Refresh**.
3. Confirm your channel shows **Connected**, **Run active**, **Lobby host**, and catalogue counts.
4. Confirm the disabled categories are marked ineligible.
5. If you choose to test a spawn, use only the enabled valuable category.
6. Check the session's recent command result in Control Center.

## Current limitations

- Twitch reward delivery is deliberately disabled pending end-to-end validation.
- Non-host participant forwarding to the lobby Master Client is not implemented in this test build.
- Modded multiplayer catalogues depend on the content being installed on the authoritative host.
- This is a test build, not a final release configuration.

## Troubleshooting

- **Connection panel not visible:** press **F8**.
- **Pairing code expired:** select **Connect to Twitch** again to request a fresh code.
- **Saved connection is offline:** reopen the panel and check status; the mod automatically retries its saved connection.
- **Connected but no catalogue:** start a run and refresh Control Center. The plugin waits for the game's run registries before publishing.
- **Test rejected:** confirm the connected broadcaster is the active lobby host and that the requested category is enabled in this build.
- **Vortex does not show the mod:** confirm the Nexus page is open in Vortex and that the deployment step completed.

## Development build

On a machine with .NET 8 installed:

~~~powershell
.\build-repo-mod.ps1
~~~

The script compiles the plugin, runs the automated tests, and creates an installable ZIP under `dist`. Game assemblies and local host credentials are not included in the package.
