# R.E.P.O. Mod implementation plan

## Ownership

- This repository owns the R.E.P.O. BepInEx plugin, game catalogue, spawn execution, local pairing client and Thunderstore package.
- spectrum-cloudflare owns Twitch identity/access, Control Center approval, Game Mods sessions, reward delivery and acknowledgements.
- Do not add Twitch passwords, Cloudflare secrets or a second EventSub manager to this mod.

## Current implementation

- BepInEx 5 plugin with REPOLib as a hard dependency.
- Live catalogue snapshot sourced from REPOLib enemies, items and valuables.
- Category toggles and relative weights are received from Control Center.
- Host-side test spawn requests are validated immediately before execution.
- The in-game Spectrum panel opens automatically when no connection is saved; F8 toggles it.
- Connect to Twitch starts a short-lived device-code pairing request, opens Control Center, and receives the private R.E.P.O. host credential after the authenticated Spectrum member approves it.
- The credential is stored locally in the BepInEx config and reconnects automatically.
- The dashboard displays connected broadcaster sessions and their run/lobby status.

## Unrestricted catalogue validation stage

The immediate goal is to verify that ordinary shop items can be discovered and spawned correctly before any exclusions are added.

- All valid entries from REPOLib Items.AllItems are included in the utility-item catalogue.
- Previous item-type allow-lists, weapon classification checks, unknown-item rejection and global weapon tags are disabled.
- SpawnWeaponsBlocker.cs is the only source of item exclusions and currently has an empty ID list.
- Keep category enable/weight settings separate from item exclusions.
- Do not add guessed IDs, display names, tags or category-wide exclusions.
- Record exact stable catalogue IDs and spawn results for any item that proves incompatible. Only then add an explicit entry to SpawnWeaponsBlocker.cs.

## Connection flow

1. The mod requests a random private device code and a short human-readable approval code from Game Mods.
2. The mod opens the Control Center URL carrying only the approval code.
3. The signed-in Twitch identity is checked against the active Spectrum member roster.
4. The Control Center authorises the pairing through a service-authenticated request.
5. Game Mods provisions a broadcaster-scoped R.E.P.O. host session and stores only a hash of the private host token.
6. The mod polls using the private device code, receives the one-time host token directly, saves it locally and connects over the existing WebSocket gateway.
7. The dashboard reads connection/run/lobby state from the R.E.P.O. host session.

The browser never receives the host token. The R.E.P.O. pairing namespace is separate from Stardew pairing and provisions REPO_HOST_SESSION rather than a Stardew session.

## Current limitations

- Twitch Channel Point reward delivery is not enabled for R.E.P.O. yet.
- Non-host multiplayer participant forwarding to the current lobby Master Client is not implemented.
- The unrestricted item pool is a temporary test mode, not a final curated release pool.
- A connected WebSocket is not proof that a run is active or that the broadcaster is the lobby host; those states are reported separately.

## Next validation steps

1. Build and test the plugin through GitHub Actions.
2. Install the resulting test package and confirm the F8 connection panel appears.
3. Pair one Spectrum broadcaster through the in-game Connect to Twitch button.
4. Confirm Control Center reports the host as connected without manual config editing.
5. Start a R.E.P.O. run and confirm catalogue counts are published.
6. Test several ordinary shop items and record exact IDs and outcomes.
7. Test valuables and monsters separately.
8. Only after catalogue/spawn validation, add specific confirmed IDs to SpawnWeaponsBlocker.cs.
9. Complete multiplayer host-authority and host-migration tests before enabling Twitch reward delivery.

## Future multiplayer contract

Each broadcaster retains an independent authenticated gateway session even when multiple broadcasters share one lobby. A redemption belongs to the broadcaster whose Twitch channel received it, while the active R.E.P.O. Master Client owns shared-world spawn execution. Forwarded requests must be verifiable by the host; a client-supplied broadcaster ID or host flag is not sufficient proof. Retries must be deduplicated by stable command ID and return the original result rather than spawning twice.
