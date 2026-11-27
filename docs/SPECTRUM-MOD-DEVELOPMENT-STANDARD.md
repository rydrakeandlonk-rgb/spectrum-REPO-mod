# Spectrum Game Mod Development Standard

**Purpose:** A reusable engineering standard for building clean, efficient, maintainable Spectrum integrations for games.  
**Reference implementation:** Spectrum R.E.P.O. Mod.  
**Last updated:** 2026-11-27.

## 1. Core principles

1. **Integrate; do not reinvent.** Use the game's own APIs, object lifecycle, authority model, replication and asset registries wherever possible.
2. **Keep responsibilities in the correct system.** Game-specific code belongs in the game mod. Twitch identity and approval belong in Control Center. EventSub belongs in Event Hub. Shared session routing belongs in Spectrum Game Mods.
3. **Build the smallest thing that proves the full path.** Start with connection and one safe test action. Add features only after the end-to-end path is reliable.
4. **Prefer explicit contracts over implicit assumptions.** Define message types, IDs, ownership, retry rules, failure states and version compatibility.
5. **Treat live game state as dynamic.** Players, host authority, lobby membership, scenes, assets and targets can change between snapshots and actions.
6. **Diagnostics explain; they do not automatically decide.** A diagnostic snapshot should only become an eligibility gate if it is authoritative and required.
7. **One authoritative execution path.** For a shared action, choose the game's existing authority and execute once. Do not let host and clients independently perform the same action.
8. **Minimise dependencies and persistent writes.** Prefer built-in game APIs and existing Spectrum services. Do not add a library, worker, database table or background poller without a concrete need.

## 2. Spectrum platform ownership

| Concern | Correct owner |
|---|---|
| Twitch OAuth, broadcaster identity and membership approval | Control Center |
| Channel Point reward configuration and operator controls | Control Center |
| Twitch EventSub connection and ingress | Event Hub |
| Shared game-session registry, connected mod sessions and routing | Spectrum Game Mods backend |
| Game process, game objects, game APIs, local assets and spawn logic | Game mod |
| Native multiplayer, ownership and replication | The game and its networking framework |
| Charity/fundraiser data and donation processing | Charity app/worker |
| Game-specific connection settings shown to operators | Dedicated game page in Control Center |

Never:
- Put Twitch client secrets, refresh tokens, Cloudflare service secrets or broad Spectrum credentials in a distributed mod.
- Add a second EventSub listener inside each game mod.
- Let a game mod decide Spectrum membership eligibility.
- Let a Cloudflare worker instantiate or manipulate game objects.
- Create a second copy of a service that already owns the required responsibility.

A mod should receive only the minimum scoped credential needed to authenticate its own game connection. Keep credentials out of logs and screenshots.

## 3. Recommended architecture

### Game mod

The mod should contain a small number of clear layers:

- **Plugin/bootstrap:** loader registration, lifecycle hooks, configuration, startup/shutdown.
- **Game adapter:** reads game state, lobby ID, local actor/player identity, current authority and live player/avatar objects.
- **Spectrum pairing client:** short-lived pairing flow; no Twitch login implementation.
- **Gateway client:** authenticated WebSocket or documented transport, message parsing, reconnect and bounded sends.
- **Main-thread dispatcher:** moves incoming network actions onto the Unity/game thread.
- **Feature handlers:** translate a validated Spectrum command into a game-specific action.
- **Execution adapter:** calls the game's native API and returns a clear result.
- **Diagnostics:** concise structured logs and optional snapshots; avoid high-frequency or expensive telemetry.

Keep transport parsing separate from game logic. A gateway message should become a typed internal command before it reaches the game adapter.

### Spectrum backend

The backend should:
- Authenticate each mod connection independently.
- Group connections by the game's real lobby/session identifier, not by Twitch broadcaster identity.
- Track which connected participant is the current game authority.
- Map a Twitch redemption to the broadcaster who received it.
- Resolve that broadcaster to their connected participant and game-specific player identifier.
- Route shared actions to the current authority.
- Track queue, delivery, acknowledgement, expiry and retry state.
- Avoid storing transient game state in D1/KV unless persistence is genuinely required.

The backend should not duplicate game-side target resolution or asset validation when the host mod can authoritatively perform those checks at execution time.

## 4. Multiplayer design rules

Before writing multiplayer code, answer these questions in a short design note:

1. What is the game's lobby/session identifier?
2. What identifier represents a player? Is it stable only within a lobby?
3. Which client is authoritative for each action?
4. Does the game already replicate the object/action?
5. What native API creates the shared object or performs the action?
6. How does the game handle host migration, disconnects and reconnects?
7. What happens if the target leaves between redemption and execution?
8. What does success mean: request accepted, object created, or object replicated?

Then follow these rules:

- Each broadcaster/mod connection identifies itself independently.
- Group participants using the actual shared lobby identifier.
- Re-evaluate authority when the game reports a role change.
- Route a shared action to one authority only.
- Include a target participant identifier when the action is player-specific.
- Resolve the target against live game state at execution time.
- Use native game networking to replicate shared effects.
- Do not create a custom network protocol if the game already provides the required behaviour.
- If a client-local effect is required, make it a distinct presentation event, not a second shared action.
- Handle stale targets and host migration explicitly.
- Never assume a periodic snapshot is a perfect representation of current live state.

### Identifier discipline

Games often have multiple identifiers that look interchangeable but are not. Give each one an explicit name and type:
- Twitch user/broadcaster ID.
- Spectrum participant/account ID.
- Mod connection/session ID.
- Game lobby/session ID.
- Game player ID / actor number.
- Network object ID / view ID.

Do not use a generic `id` field when the distinction matters. Never pass one identifier type into an API expecting another.

## 5. Command delivery contract

Every remote action should have a stable envelope containing, as applicable:

- `type`: versioned message type.
- `request_id`: unique, stable ID for deduplication.
- `session_id` and `lobby_id`: destination context.
- `target_player_id`: explicit game-native player identifier.
- `category` / `action`: allow-listed action.
- `payload`: minimal validated parameters.
- `created_at` / expiry: prevent stale actions from executing indefinitely.

Recommended lifecycle:

1. Validate ingress and deduplicate the source event.
2. Resolve the correct participant and active lobby.
3. Select the current authority.
4. Queue and dispatch with a stable request ID.
5. Validate again inside the mod.
6. Resolve the live target and game objects.
7. Execute through the native game API.
8. Acknowledge success or a specific failure.
9. Retry only according to an explicit policy; the same request ID must never create a second action.

Distinguish:
- HTTP accepted by an endpoint.
- Event accepted by the backend.
- Request queued.
- Request dispatched.
- Mod received request.
- Game API accepted the action.
- Action replicated/observed.
- Final acknowledgement received.

Never report a game action as successful based only on an HTTP 200 response.

## 6. Game integration research before implementation

Do not start by writing a large mod. First inspect the game and its modding ecosystem.

### Research checklist

- Game engine and runtime version.
- Mod loader and compatible version.
- Required framework dependencies; identify which are truly required versus optional.
- Existing game APIs and public modding APIs.
- Game assemblies and type definitions needed for integration.
- Player, lobby and authority model.
- Native spawn/action lifecycle.
- Asset/prefab discovery and network registration rules.
- Main-thread restrictions.
- Save/configuration conventions.
- Mod packaging and install-manager expectations.
- Game updates and compatibility risks.

Use the smallest dependency set that supports the feature. A library should be added only if it provides functionality that cannot be implemented cleanly with the game's existing APIs or the mod loader.

### Inspect before reflecting

Prefer public game methods and stable mod APIs. If reflection is unavoidable:
- Isolate it in one adapter.
- Check that the expected member exists and has the expected type.
- Fail safely if the game update changes it.
- Log a clear compatibility error.
- Add a focused test or in-game validation step.
- Never scatter private field names throughout the codebase.

## 7. Game-thread and network-thread separation

For Unity/BepInEx integrations:

- Network receive and retry work may run asynchronously.
- Do not touch Unity objects, Photon game state or scene objects from arbitrary background continuations.
- Parse incoming data off-thread if useful, then enqueue a typed action.
- Drain the action queue from the plugin's main `Update` loop.
- Catch exceptions per action so one bad message does not stop the queue.
- Bound message size, queue growth, retry frequency and reconnect delays.
- Cancel background work and close sockets during plugin shutdown.

Keep network handlers small. They should validate the message envelope and enqueue work, not execute game logic directly.

## 8. Asset catalogues and spawn safety

For games that expose spawnable assets:

- Discover from the game's actual loaded registries or supported mod APIs.
- Validate the required component/type and prefab/resource reference.
- Verify multiplayer network registration before allowing shared network spawns.
- Use stable content IDs, not display names, as identity.
- Sort catalogue entries deterministically.
- Keep unsupported or ambiguous content out of the eligible catalogue.
- Revalidate the candidate immediately before execution; assets can unload or become invalid.
- Refresh on scene/level changes and at a bounded interval if mods load assets late.
- Keep catalogue data transient unless there is a real product requirement to persist it.
- Expose precise exclusion reasons for operator diagnostics.

Do not automatically allow unknown items based only on their names. Use conservative type/component validation and explicit registration for third-party content when needed.

## 9. Performance and operational efficiency

- Avoid per-frame reflection, asset scans, network requests and database writes.
- Read game state at lifecycle events or at a modest bounded interval.
- Send session heartbeats only as often as the backend needs to detect stale connections.
- Publish catalogues when they change, on level transitions, or on an explicit request—not every frame.
- Rate-limit diagnostic snapshots.
- Keep routine logs concise; include request/session identifiers on errors.
- Avoid writing transient player lists, catalogue snapshots or chat events to persistent storage without a clear requirement.
- Keep backend invocations, D1 writes, KV writes and queue operations proportional to actual user actions.
- Make retry/backoff bounded and cancellable.
- Prefer one source of truth for each piece of state.

Efficiency means reducing unnecessary work and duplicated responsibility, not compressing all logic into one giant class.

## 10. Security and trust boundaries

- Pair through Control Center using short-lived approval codes.
- Give each broadcaster/mod instance a scoped private credential.
- Store only credential hashes on the server where practical.
- Never expose private host credentials to browser JavaScript or dashboard payloads.
- Do not log tokens, authorization headers, pairing secrets or private user data.
- Validate every incoming message, category, action and payload on both backend and mod.
- Treat the backend as a router, not as proof that a game action is safe.
- Re-check host authority and target validity immediately before execution.
- Keep charity/fundraiser actions within the charity service boundary.
- Never let a user-supplied display name or catalogue label become executable code or a resource path.

## 11. Testing strategy

Build tests in layers so failures can be located quickly.

### Unit tests
- Message parsing and validation.
- Identity mapping.
- Lobby grouping.
- Host selection and host migration.
- Eligibility and readiness states.
- Request deduplication.
- Queue/retry/expiry rules.
- Catalogue filtering and deterministic selection.
- Failure handling.

### Backend integration tests
- Authenticated mod connection.
- Two or more participants in one lobby.
- Twitch event routed to the correct broadcaster.
- Client request dispatched to host.
- Host acknowledgement updates queue.
- Disconnected host and host migration.
- Incomplete/missing diagnostic snapshot does not incorrectly reject valid participants.

### In-game tests
- Single-player local test action.
- Host-only multiplayer action.
- Client-originated redemption targeting the client.
- Object visible to host and remote clients.
- Target disconnects before execution.
- Host leaves or migrates.
- Duplicate delivery.
- Level transition and late-loaded assets.
- Mod restart and reconnect.

### Release gates

Do not call a feature complete until:
- Unit and backend tests pass.
- Build and packaging checks pass.
- The exact mod version has been installed in the target game.
- The feature has been tested in the relevant multiplayer configuration.
- Host and client logs show the same request ID and expected result.
- The dashboard reflects the final acknowledged outcome.
- Release notes and known limitations are documented.

A successful CI build proves the code compiled and automated checks passed. It does not prove the game integration works in a live lobby.

## 12. Development workflow

1. **Read existing documentation first.** Read the repository README, implementation plan, changelog, architecture notes and build/release workflow.
2. **Inspect the current source and deployed backend contract.** Do not assume old notes still match current code.
3. **Write a small design plan.** State ownership, messages, IDs, authority, native game API, dependencies and test cases.
4. **Find the simplest native game path.** Verify it with a local in-game test before adding Twitch/backend routing.
5. **Build a vertical slice.** Pair, connect, report lobby state, send one test request, execute once, acknowledge.
6. **Add multiplayer.** Prove host/client routing and replication before adding more content types.
7. **Add features incrementally.** Catalogue, weighted selection, categories, callouts and optional integrations should be separate steps.
8. **Add regression tests with each fix.** Reproduce the observed failure, test the corrected behaviour and preserve the old success cases.
9. **Inspect deployment scope.** Confirm exactly which worker/service will deploy and which workflows will run.
10. **Validate the deployed result.** Check health and live session behaviour; do not equate deployment success with game success.
11. **Document the outcome.** Update README, changelog, architecture notes and incident notes when contracts or lessons change.

For production fixes, prefer a small, targeted change. Do not change Control Center, Event Hub, the game mod and the backend together unless the traced failure crosses those boundaries.

## 13. Repository and packaging standards

- Keep one clear source-of-truth repository per game mod.
- Keep game-specific code out of the shared backend.
- Keep backend-specific code out of the mod.
- Avoid committing secrets, generated build output, local game saves or unnecessary game binaries.
- Pin and document required game/mod-loader/framework versions.
- Keep local build and CI build outputs equivalent.
- Make install archives contain only the files the mod manager expects.
- For the R.E.P.O. mod, the Vortex/Nexus archive is intentionally a single `SpectrumRepoMod.dll` at archive root.
- Normal pushes should build/test only. Publishing to Nexus or another public mod site should require an explicit release or deliberate manual action.
- Use semantic versioning and maintain a changelog with user-visible and compatibility-relevant changes.

## 14. Definition of a clean Spectrum game mod

A mod is ready for wider use when it has:

- A clear ownership boundary.
- Minimal, justified dependencies.
- A documented pairing and credential model.
- A documented lobby/player/authority model.
- A native game execution path.
- A stable command and acknowledgement contract.
- Safe main-thread dispatch.
- Request deduplication.
- Bounded reconnect, retry and diagnostics.
- Unit and backend regression tests.
- A repeatable build and correct package.
- A real multiplayer validation checklist.
- Documentation of unsupported cases and game-version assumptions.

## 15. R.E.P.O. reference lessons

The R.E.P.O. integration demonstrates several standards worth reusing:

- BepInEx plugin with a compact F8 status/pairing panel.
- Device-code pairing through Control Center; no Twitch credentials in the mod.
- Per-broadcaster authenticated gateway connections.
- Lobby grouping by Photon lobby ID.
- Host-authoritative shared spawning through existing Photon room objects.
- Target resolution using the live game-managed avatar list and PhotonView owner actor number.
- Stable request IDs, deduplication and acknowledgements.
- Background WebSocket receive with main-thread action dispatch.
- Catalogue discovery from loaded game assets, with conservative validation.
- Bounded state reporting and 30-second catalogue/diagnostic refresh rather than per-frame work.
- DLL-only Vortex packaging and explicit Nexus release gating.

The most important R.E.P.O. incident lesson: **do not make a participant's eligibility depend on an incomplete diagnostic snapshot when the host can authoritatively resolve the target at execution time.**
