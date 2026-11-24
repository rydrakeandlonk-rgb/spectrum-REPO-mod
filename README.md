# Spectrum R.E.P.O. Mod

A BepInEx plugin that connects R.E.P.O. multiplayer sessions to Spectrum's existing Game Mods infrastructure.

## Installation

Install through Thunderstore Mod Manager (or another compatible manager). The package declares its runtime dependencies so the manager can install them:

- **BepInExPack** — the standard R.E.P.O. BepInEx loader. Spectrum does not bundle or recreate it.
- **REPOLib** — used for supported networked enemy, item and valuable discovery/spawning APIs.

Do not copy BepInEx files into this repository or package them inside the Spectrum mod.

## Architecture

Spectrum's existing Cloudflare services remain responsible for Twitch/EventSub authentication, member access, reward configuration, broadcaster-scoped host sessions, delivery, retries and redemption deduplication.

This repository owns only the game plugin:

1. Each participating player's mod connects to that player's broadcaster-scoped Spectrum Game Mods session.
2. A redemption is delivered to the corresponding player's connected mod.
3. If that player is not the R.E.P.O. lobby host, the mod forwards the typed request through the game's multiplayer networking to the current Master Client.
4. The Master Client validates the request, resolves the requested content from the local game catalogue and performs the spawn once.
5. The game networking layer replicates the spawned entity/item to the lobby.

The Spectrum connection identity and the R.E.P.O. lobby identity are deliberately separate. Multiple Spectrum streamers can participate in one R.E.P.O. lobby without sharing credentials or collapsing their broadcaster-scoped sessions.

## Planned redemption types

- Enemy spawn
- Shop / utility item spawn
- Valuable spawn

The initial implementation will use the game's host-authoritative networking path. No client may independently spawn the same redemption. Every request carries a stable command ID so the host can reject duplicates and the gateway can receive an acknowledgement only after the game action completes.

## Development

This project targets the BepInEx 5 / Mono plugin environment used by R.E.P.O.

Set the `REPOGamePath` MSBuild property to your local R.E.P.O. installation directory, or set the `REPO_GAME_PATH` environment variable. The game assemblies are referenced from:

```
$(REPOGamePath)/REPO_Data/Managed
```

Do not commit game DLLs, BepInEx DLLs, host tokens, service secrets or local profile data.

The plugin is not ready for public distribution until it has been built against the current game assemblies and tested in both host and client roles.

## Related repository

The shared gateway, Event Hub routing and Control Center integration live in [spectrum-cloudflare](https://github.com/rydrakeandlonk-rgb/spectrum-cloudflare). This repository must not implement a second Twitch gateway or duplicate the Control Center's member/authentication logic.
