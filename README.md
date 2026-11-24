# Spectrum R.E.P.O. Mod

Spectrum's host-side R.E.P.O. integration. It connects a broadcaster's game instance to the Spectrum Game Mods gateway, publishes the live REPOLib catalogue, receives Control Center test requests, and executes them on the current lobby host.

**This is an early testing build.** Twitch Channel Point redemptions and non-host multiplayer forwarding are not enabled yet. Manual tests are sent from the admin-only R.E.P.O. page in Spectrum Control Center.

## Requirements

- R.E.P.O.
- BepInEx 5
- REPOLib (installed automatically by Thunderstore Mod Manager)

## Install

Install **Spectrum R.E.P.O. Mod** through Thunderstore Mod Manager and launch R.E.P.O.

## Connect to Twitch

The first time the mod loads without a saved connection, its Spectrum connection panel opens automatically. You can also open or close it with **F8**.

1. Select **Connect to Twitch** in the in-game Spectrum panel.
2. The mod opens the Spectrum Control Center in your browser and displays a short approval code.
3. Sign in with the Twitch account that owns the Spectrum channel and approve the R.E.P.O. connection.
4. Return to the game. The mod receives and saves its private host credential automatically, then connects to Spectrum.

You do not need to generate a credential in the dashboard or edit the BepInEx config file. The credential is stored locally by the mod and is never displayed in the browser. To move the connection to another Twitch account, revoke the existing host access in Control Center and pair again.

## Test the connection and catalogue

1. Start a run as the R.E.P.O. lobby host.
2. Open Control Center → Games → R.E.P.O. and select **Refresh**.
3. Confirm your channel shows **Connected**, **Run active**, **Lobby host**, and catalogue counts.
4. Use **Test monster**, **Test utility item**, and **Test valuable** one at a time.
5. Check the game and the session's recent command result in Control Center.

The initial catalogue test is intentionally unrestricted for shop items. Every valid REPOLib item is included, regardless of its previous item-type classification. The hard-coded SpawnWeaponsBlocker currently contains no IDs, so no items are blocked by it. This is deliberate: first verify ordinary items are discoverable and spawn correctly, then add specific exclusions after testing.

The category enable/weight settings remain in Control Center. They are separate from item exclusions.

## Current limitations

- Twitch reward delivery is deliberately disabled pending end-to-end validation.
- Non-host participant forwarding to the lobby Master Client is not implemented in this test build.
- Modded multiplayer catalogues depend on the content being installed on the authoritative host.
- The unrestricted catalogue is a test mode. Do not treat the current item pool as a final curated release pool.

## Troubleshooting

- **Connection panel not visible:** press **F8**.
- **Pairing code expired:** select **Connect to Twitch** again to request a fresh code.
- **Saved connection is offline:** reopen the panel and check status; the mod automatically retries its saved connection.
- **Connected but no catalogue:** start a run and refresh Control Center. The plugin waits for the game's run registries before publishing.
- **Test rejected:** confirm the connected broadcaster is the active lobby host and that the selected category has eligible entries.

## Development build

On a Windows machine with R.E.P.O. installed:

~~~powershell
.\build-thunderstore.ps1 -GamePath "C:\Program Files (x86)\Steam\steamapps\common\REPO"
~~~

The script compiles against the local game-managed assemblies and creates a Thunderstore package. Game assemblies are not included in the package or source repository.
