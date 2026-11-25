# R.E.P.O. Mod implementation plan

## Ownership

- This repository owns the R.E.P.O. BepInEx plugin, vanilla valuable catalogue, spawn execution, local pairing client, and Nexus/Vortex package.
- spectrum-cloudflare owns Twitch identity/access, Control Center approval, Game Mods sessions, reward delivery, and acknowledgements.
- Do not add Twitch passwords, Cloudflare secrets, or a second EventSub manager to this mod.

## Current implementation

- BepInEx 5 plugin; no REPOLib dependency.
- Valuable catalogue sourced from the current level's vanilla `LevelValuables` presets.
- The seven vanilla size lists are combined and de-duplicated by prefab resource path.
- The plugin prepares the current level's prefab references through `LevelGenerator.PrefabsPrepare`.
- Host-side valuable test requests are validated immediately before execution.
- Single-player and multiplayer spawning follow the vanilla `ValuableDirector` implementation.
- The in-game Spectrum panel opens automatically when no connection is saved; F8 toggles it.
- Connect to Twitch uses short-lived device-code pairing and stores the approved host credential locally.
- The dashboard displays connected broadcaster sessions and their run/lobby status.

## Initial test-build restrictions

- Valuable requests are the only supported category.
- Other categories are rejected locally, even if older Control Center settings enable them.
- Twitch reward delivery and non-host multiplayer forwarding remain disabled pending validation.

## Connection flow

1. The mod requests a random private device code and a short human-readable approval code from Game Mods.
2. The mod opens the Control Center URL carrying only the approval code.
3. The signed-in Twitch identity is checked against the active Spectrum member roster.
4. The Control Center authorises pairing through a service-authenticated request.
5. Game Mods provisions a broadcaster-scoped R.E.P.O. host session and stores only a hash of the private host token.
6. The mod polls using the private device code, receives the one-time host token directly, saves it locally, and connects over the existing WebSocket gateway.
7. The dashboard reads connection/run/lobby state from the R.E.P.O. host session.

The browser never receives the host token. The R.E.P.O. pairing namespace remains separate from Stardew pairing.

## Validation steps

1. Build and test the plugin through GitHub Actions.
2. Install the resulting test package through Vortex and confirm the F8 connection panel appears without REPOLib installed.
3. Pair one Spectrum broadcaster through the in-game Connect to Twitch button.
4. Confirm Control Center reports the host as connected without manual config editing.
5. Start a R.E.P.O. run and confirm the current level's valuable catalogue is published.
6. Test valuable spawning in single-player.
7. Test valuable spawning in a private multiplayer session with the broadcaster as lobby host.
8. Change levels and confirm the catalogue refreshes.
9. Complete multiplayer host-authority and host-migration tests before enabling Twitch reward delivery.

## Future multiplayer contract

Each broadcaster retains an independent authenticated gateway session even when multiple broadcasters share one lobby. A redemption belongs to the broadcaster whose Twitch channel received it, while the active R.E.P.O. Master Client owns shared-world spawn execution. Forwarded requests must be verifiable by the host; a client-supplied broadcaster ID or host flag is not sufficient proof. Retries must be deduplicated by stable command ID and return the original result rather than spawning twice.
