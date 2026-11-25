# R.E.P.O. Mod implementation plan

## Ownership

- This repository owns the R.E.P.O. BepInEx plugin, vanilla valuable catalogue, local pairing client, gateway client, spawn execution, and Nexus/Vortex package.
- spectrum-cloudflare owns Twitch identity/access, Control Center approval, Game Mods sessions, lobby coordination, test-request delivery, and acknowledgements.
- Do not add Twitch passwords, Cloudflare secrets, or a second EventSub manager to this mod.

## 0.4.0 connected test build

The first connected implementation is now compiled and packaged on the `testing/0.4.0-connection` branch.

Implemented in source:

- BepInEx 5 plugin with an F8 connection/status panel.
- Short-lived device-code pairing through Spectrum Control Center.
- Local storage of the approved broadcaster ID and private host credential in the BepInEx configuration.
- WebSocket connection to the Spectrum Game Mods gateway.
- Session reporting for active run, Photon lobby ID, and current host authority.
- Periodic session refresh so the lobby coordinator can expire disconnected clients.
- Current-level valuable catalogue built from the selected vanilla `LevelValuables` presets.
- Host-only execution of valuable test requests.
- Multiplayer instantiation through Photon room objects; local instantiation in single-player.
- Request-ID deduplication before spawning and an acknowledgement for success or failure.
- A local F8-panel test button for the current host.
- Explicit valuable-only runtime policy. Other spawn categories are rejected.
- Test-branch builds create a DLL-only Vortex artifact and do not publish automatically to Nexus Mods.

## Initial test-build restrictions

- Valuable collectibles are the only supported spawn category.
- Monster and utility-item requests are rejected locally.
- Twitch reward delivery is not enabled.
- The Control Center test action is the only remote spawn request source in this build.
- The first test build is not considered multiplayer-validated until two clients have confirmed that a single host-created object appears to both.

## Connection flow

1. The mod requests a random private device code and a short human-readable approval code from Game Mods.
2. The mod opens the Control Center URL carrying the approval code.
3. The signed-in Twitch identity is checked against the active Spectrum member roster and the R.E.P.O. host eligibility rules.
4. The Control Center authorises pairing through a service-authenticated request.
5. Game Mods provisions a broadcaster-scoped R.E.P.O. host session and stores only a hash of the private host token.
6. The mod polls using the private device code, receives the one-time host token directly, saves it locally, and connects over the WebSocket gateway.
7. The mod reports run/lobby/host state and publishes the current valuable catalogue.
8. The dashboard reads connection and session status from the R.E.P.O. host session.

The browser never receives the host token. The R.E.P.O. pairing namespace remains separate from Stardew pairing.

## Validation steps

1. Confirm the 0.4.0 GitHub Actions build and DLL-only package checks pass.
2. Install the resulting test package through Vortex and confirm the F8 panel appears without REPOLib installed.
3. Pair Rydrake and Lonk separately and confirm both appear in the Control Center R.E.P.O. account list.
4. Confirm Control Center reports each connection.
5. Start a private multiplayer run and confirm run state, lobby ID, and host authority are correct for both clients.
6. Test the local single-player valuable button.
7. Trigger one Control Center valuable test request and confirm the host executes it once.
8. Confirm both players see the same spawned valuable.
9. Change levels and confirm the catalogue refreshes.
10. Test disconnect/reconnect and host migration before enabling Twitch reward delivery.

## Multiplayer contract

Each broadcaster retains an independent authenticated gateway session even when multiple broadcasters share one lobby. The lobby coordinator correlates players by Photon lobby ID, not broadcaster ID. A Control Center test request resolves the active lobby host and delivers the request to that host's authenticated session. The host rechecks current authority and executes the shared spawn once. Retries must be deduplicated by stable request ID and return the original result rather than spawning twice.
