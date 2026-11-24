# Implementation plan

## Repository boundaries

- This repository owns the R.E.P.O. BepInEx plugin, local gateway client, R.E.P.O. multiplayer forwarding, spawn catalog/execution and Thunderstore package.
- `spectrum-cloudflare` remains the only owner of Twitch/EventSub, member identity/access, Control Center configuration, broadcaster-scoped Game Mods sessions, delivery queues, retries and gateway acknowledgements.
- Do not add Twitch credentials, Cloudflare secrets, a second EventSub subscription manager or another general-purpose gateway here.

## Verified package contract

- BepInExPack is an external Thunderstore dependency. Do not bundle or recreate it.
- REPOLib is an external Thunderstore dependency and a hard BepInEx dependency.
- The Thunderstore package uses the published REPOLib version pinned in `manifest.json`.
- The BepInEx plugin GUID is `REPOLib` (the Thunderstore namespace `Zehs` is not the runtime plugin GUID).

## Multiplayer requirements

1. Each participating Spectrum broadcaster retains an independent authenticated gateway session.
2. A redemption is delivered to the mod connection belonging to the broadcaster that redeemed it.
3. The receiving client forwards the request to the current R.E.P.O. Master Client through the game's multiplayer transport.
4. Only the current Master Client may execute a shared spawn.
5. The host validates request shape, category allowlist, active run state, request origin/authorization, limits and deduplication before execution.
6. The host returns a result to the originating client; that client acknowledges its own gateway delivery only after receiving the host result.
7. A retry with the same command ID must return the prior result rather than create a second spawn.
8. Host migration, disconnects and lobby/run transitions invalidate stale routing state.

The Spectrum broadcaster identity and R.E.P.O. lobby identity remain separate. Multiple broadcasters can participate in one lobby without sharing credentials or merging their gateway sessions.

## Security/design gate before implementation

A PUN message from a lobby client is not, by itself, proof that the message represents a valid Twitch redemption. Before enabling shared spawns, choose and test a host-verifiable authorization mechanism for forwarded commands. The options to evaluate are a short-lived gateway-signed command envelope or a server-side validation exchange through the existing gateway. Do not trust a client-supplied broadcaster ID, host flag or redemption ID without validation.

The game networking event transport also needs a compatibility test. REPOLib exposes networked-event helpers, but event-code allocation and registration order must be checked across the same mod profile on every participant before we rely on it.

## Spawn scope

- Enemy catalogue entries, sourced from REPOLib's current enemy registry.
- Eligible shop utility items and valuables, sourced from REPOLib's current registries and filtered through one central spawn exclusion list.
- The exclusion list is deny-by-ID and deny-by-tag. The default list excludes the weapon tag globally; specific unstable or softlock-prone item IDs can be added without changing selection code.
- Apply exclusions both when building the candidate catalogue and immediately before host-side spawn execution. Unknown classifications remain ineligible.

The adapter should use the current REPOLib registry properties (`Enemies.AllEnemies`, `Items.AllItems`, and `Valuables.AllValuables`) rather than maintaining a static catalogue of ordinary vanilla content. Build the candidate snapshot only after the run and content registries are ready. The utility-item eligibility classifier must be conservative: only known supported utility entries enter the pool; unsupported or ambiguous entries are excluded and surfaced in diagnostics.

Do not expose unrestricted arbitrary prefab names. Use explicit allowlists and bounded quantities/cooldowns. The initial scope excludes weapons.

## Milestones

### M0 — Bootstrap
- BepInEx 5 / Mono plugin project.
- REPOLib hard dependency.
- Thunderstore manifest depends on BepInExPack and REPOLib; neither is copied into the package.
- Plugin load and dependency logging.

### M1 — R.E.P.O. runtime validation
- [x] Verify the REPOLib catalogue/spawn surface: `Enemies.AllEnemies`, `Items.AllItems`, `Valuables.AllValuables`, and host-only item/valuable spawn methods are available in the library source. The game-side item classification still needs local runtime validation.
- [x] Add a pure combined-reward selector with weighted category selection, category fallback when a pool is empty, and fail-closed handling for unclassified shop entries.
- [x] Add a central spawn exclusion list with a default weapon-tag exclusion, explicit item-ID exclusions, and tag-based exclusions. Apply it before selection; the runtime executor must recheck it before spawning.
- [x] Add isolated automated tests for selection, category fallback, category weights and shop-entry classification.
- Build against the installed game's managed assemblies.
- Confirm current R.E.P.O. version and supported lobby/run lifecycle hooks.
- Confirm current Master Client detection and host migration behavior.
- Validate the multiplayer request transport across two clients.

### M2 — Local host-authoritative prototype
- Typed request envelope and strict size/schema validation.
- Non-host-to-host forwarding.
- Host-only execution guard.
- Duplicate command suppression and result return.
- No gateway integration yet; test locally in a private multiplayer lobby.

### M3 — Gateway adapter
- Audit the exact existing Game Mods WebSocket protocol and pairing flow.
- Add R.E.P.O. as a separate game adapter in the existing `spectrum-game-mods` Worker.
- Add a separate Event Hub readiness gate for R.E.P.O.; do not alter Stardew readiness.
- Preserve broadcaster-scoped sessions and existing Stardew behavior.
- Implement and test host-verifiable authorization before accepting forwarded requests.

### M4 — Spawn catalog and Control Center
- Enemy, non-weapon utility-item and valuable allowlists.
- Configurable category toggles, limits and cooldowns.
- R.E.P.O.-specific access/configuration in the existing Mods Manager only where the audit proves it is needed.

### M5 — Release
- Build/package validation.
- Two- and multi-broadcaster multiplayer test matrix.
- Host migration, duplicate delivery, reconnect and failure-path tests.
- Thunderstore release notes and package validation.

## Combined random reward contract

The Twitch integration uses one combined R.E.P.O. reward. Each valid redemption selects exactly one category, then one eligible entry from that category:

- Monster
- Non-weapon utility item
- Valuable

Initial category selection is uniform (one-third probability per category). Selection within a category is uniform across its currently eligible allowlist entries. A category with no eligible entries is excluded from that selection; if all categories are unavailable, the command fails with a clear result and does not acknowledge success.

Control Center owns the category probability settings, with equal probabilities as defaults. The gateway forwards the resolved configuration snapshot with each command so retries use the same selection rules. The game host chooses and records the selected category and entry against the command ID; a retry must return the recorded result rather than rerolling.

There is one Twitch reward mapping and one Event Hub readiness gate for R.E.P.O. Manual Control Center tests use the same typed command and host execution path but are marked as administrator-originated test actions, bypassing Twitch redemption intake without bypassing host authority, category allowlists, or deduplication.

Control Center's R.E.P.O. view must show live connected broadcaster sessions and their game/lobby relationship, and expose manual tests for the combined random reward. The test result must report the selected category, selected entry label, and host execution outcome.

Category probability changes are configuration updates, not additional Twitch rewards. They must be validated as finite non-negative weights with a positive total and applied from a versioned configuration snapshot. The initial default is equal weighting.

