# Multiplayer integration post-mortem — R.E.P.O. 0.10.0

**Date:** 2026-11-27  
**Status:** Resolved; R.E.P.O. multiplayer Twitch rewards confirmed working again.  
**Scope:** Spectrum R.E.P.O. mod, Spectrum Game Mods worker, lobby coordination and host/client participant eligibility.

## Executive summary

The R.E.P.O. integration already had the correct core multiplayer model: each connected Spectrum user maintained their own authenticated mod connection, the backend grouped connections by the shared Photon lobby, Twitch redemptions were matched to the broadcaster who received them, and the current game host executed shared spawns.

The regression was in an additional backend eligibility check. The lobby coordinator required the host's periodic Photon player/avatar diagnostic snapshot to contain every connected remote participant before that participant could become reward-ready. In practice, the host snapshot could be empty or incomplete even while the game and mod could resolve the target avatar when a request arrived. This caused a false negative: a connected client such as Cutler was shown as ignored even though their own mod connection reported a valid Photon actor ID and their redemption reached the backend.

We removed the host snapshot as an eligibility gate. The coordinator now validates the connected participant's own reported actor ID and uses the host snapshot for diagnostics only. The host mod remains responsible for resolving the target avatar at execution time and safely rejecting a request if the target is no longer available.

The fix was deployed to the Spectrum Game Mods worker. No R.E.P.O. mod rebuild, Event Hub change, Control Center change, or new multiplayer protocol was required. The live multiplayer reward flow was subsequently confirmed working again.

## System architecture

### Ownership boundaries

| Component | Owns | Must not own |
|---|---|---|
| R.E.P.O. BepInEx mod | Game state, Photon lobby/actor identity, game asset catalogue, target avatar lookup, game-native spawn execution, local diagnostics and acknowledgements | Twitch login, Twitch API credentials, EventSub subscriptions, Spectrum membership approval, Cloudflare service secrets |
| Spectrum Control Center | Twitch account approval, Spectrum membership/host eligibility, pairing UX, session management UI, reward configuration and operator controls | Direct Unity/game object manipulation |
| Spectrum Game Mods worker | Authenticated mod sessions, lobby grouping, host selection, redemption-to-participant routing, queue/delivery coordination, request/ack state | Game-specific object creation or recreating Photon |
| Event Hub | Central Twitch EventSub ingress and event distribution to the appropriate application | R.E.P.O. game logic or a second game-specific Twitch connection |
| Photon / R.E.P.O. | Native lobby membership, actor ownership, host/master authority and replication of shared network objects | Spectrum account and Twitch identity |

**Rule:** Keep each responsibility in the system that already owns it. Do not move Twitch credentials into a game mod or rebuild game networking in a Cloudflare worker.

### Successful request flow

1. Each Spectrum member pairs their own R.E.P.O. mod through Control Center.
2. The mod receives a private host credential and connects to the Spectrum Game Mods gateway using its broadcaster identity.
3. Each mod reports its current run state, Photon lobby ID, host/master status and local Photon actor number.
4. Game Mods groups active connections by the actual shared lobby ID and identifies the current host connection.
5. EventSub delivers a Twitch redemption through Event Hub. The backend identifies the Twitch broadcaster/channel that received it.
6. The lobby coordinator maps that broadcaster to their connected participant record and obtains that participant's reported Photon actor number.
7. The backend queues and dispatches the request to the current host mod, including the target Photon actor number and stable request ID.
8. The host mod rechecks that it is still the game authority, resolves the target player's current game-managed avatar, validates the requested category and candidate, then invokes the existing game-native spawn path.
9. For shared objects, the host creates the network object using the game's Photon room-object mechanism. Photon replicates it to the other players.
10. The host acknowledges the request. Stable request IDs prevent retries from intentionally creating a second spawn.

The target player's mod does not need to create a duplicate object locally. The host creates the shared object at or near the target participant's avatar; the game's existing multiplayer system distributes it.

## Incident timeline and diagnosis

### Symptoms

- Rydrake hosting a multiplayer lobby could redeem rewards successfully.
- Cutler joined the same lobby and connected their Twitch account through the mod.
- The dashboard showed two mod-connected participants, but only the host was ready.
- Cutler was marked ignored with the reason: **"The host's Photon player list does not contain actor 2."**
- Cutler's Twitch redemption reached the Game Mods endpoint and received an HTTP 200 response, but the outcome was `participant_ignored`.
- The host's periodic diagnostic snapshot showed zero Photon players and zero avatar mappings, even around a time when a host redemption had successfully completed end-to-end.

### What the evidence established

1. **Twitch ingress was working.** The client redemption reached the Game Mods endpoint.
2. **The participant connection existed.** Cutler appeared as a connected lobby participant and had reported a Photon actor ID.
3. **The backend was rejecting the participant before delivery.** The outcome was `participant_ignored`, with the host-snapshot membership check as the reason.
4. **The host snapshot was not a reliable source of participant eligibility.** It could be empty or incomplete while the host mod's live target-resolution path could still resolve a player for a request.
5. **The spawn architecture itself was not the problem.** R.E.P.O. already had host-authoritative shared spawning and Photon replication.

This distinction mattered: a diagnostic snapshot describes what the host reported at snapshot time. It is not automatically authoritative proof that a connected participant cannot be targeted when the game handles a spawn request.

## Root cause

The Game Mods lobby coordinator's session activation path had accumulated a second participant eligibility system. It required all of the following before a remote participant could become ready:

- A fresh host snapshot.
- A valid host-local actor identity.
- A host-local avatar.
- The remote participant's actor number in the host's Photon player list.
- The remote participant's actor number in the host's avatar-owner list.

These checks made backend readiness depend on a periodically reported diagnostic view of the game, rather than on the participant's own authenticated connection and the host mod's live execution-time lookup.

The check was too strict and could reject valid participants. It also risked pausing reward activation for an entire lobby because of an incomplete snapshot.

## Fix applied

The fix was made in the `spectrum-cloudflare` repository, in:

- `workers/spectrum-game-mods/src/repo-lobby-coordinator.js`
- `workers/spectrum-game-mods/src/repo-lobby-coordinator.test.js`

The coordinator now:

- Requires a valid current host/master connection.
- Validates that each connected participant reports a positive integer Photon actor number.
- Uses the participant's own reported actor number as the routing target.
- Keeps host Photon player/avatar snapshots as diagnostic information only.
- Does not reject remote participants merely because their actor is absent from a host snapshot.
- Does not pause the entire lobby's reward setup because the host snapshot is empty or incomplete.
- Leaves the authoritative live target-avatar lookup to the host mod when a spawn request is executed.

Regression tests were added for both an incomplete host snapshot and a missing host snapshot. The tests assert that otherwise-valid connected clients remain eligible.

### Deployment record

The fix was committed directly to `main`:

- `7bb4eff` — Remove host snapshot as a participant eligibility gate.
- `d1805c3` — Test participant eligibility without host player snapshots.
- `6b16a66` — Fix service credentials in no-snapshot regression test.

The first CI attempt failed because the new test fixture omitted required service credentials. The fixture was corrected; the deployment workflow then passed the Game Mods test step, worker dry-run, deployment and deployed health check. The Game Mods suite reported 70 tests with zero failures. A separate full-repository validation workflow was still running at the time of the deployment report; do not treat that workflow as completed based on the deployment result alone.

Only the Spectrum Game Mods worker was deployed. The R.E.P.O. mod remained version 0.10.0. Event Hub and Control Center were not changed.

## Other important R.E.P.O. engineering lessons

### 1. Photon ActorNumber and PhotonView ViewID are different identifiers

A Photon actor number identifies a player in the room. A PhotonView ID identifies a networked view/object. They are not interchangeable.

An earlier targeting issue was fixed in mod 0.8.4 by matching the requested actor number against each game-managed PlayerAvatar's PhotonView owner actor number. Do not pass a player ActorNumber to APIs that expect a PhotonView ID.

When debugging identity, log the names of the identifier fields explicitly:
- `ActorNumber`
- `OwnerActorNr`
- `ViewID`

Never label all of these simply as "Photon ID".

### 2. Use the game's player/avatar registry

The mod's target-resolution path uses the game-managed player avatar list and matches each avatar's PhotonView owner actor number to the target participant. Prefer game-owned player objects and supported game APIs over maintaining a parallel player registry.

The lookup must happen when the host handles the request, not only when a periodic dashboard snapshot is generated. Players can join, leave, reconnect or change roles during a session.

### 3. Preserve native multiplayer authority

R.E.P.O. shared spawns are host-authoritative. The host uses the existing spawn executor and Photon room-object instantiation for shared network objects. Clients receive the object through Photon.

Do not add client-side duplicate spawning or a new client-to-client RPC layer unless investigation proves the game's native path cannot satisfy the requirement.

### 4. Preserve the game's native spawn lifecycle

Monster spawning required special care. The mod uses the game's `LevelGenerator.EnemySpawn` pathway so the normal setup, relocation and enemy registration lifecycle remain intact. It also corrects the relevant `EnemiesSpawnTarget` bookkeeping issue so spawned enemies can register players. Immediate enemy-list counts are not a reliable success signal because some setup is asynchronous.

For every game object category, use the narrowest supported native game method and understand what the game does after the call returns.

### 5. Network prefab registration is a real compatibility requirement

A prefab being loaded or discoverable does not automatically mean it can be spawned over the network. A multiplayer prefab must be registered with the game's networking system and expose the expected valid prefab/resource reference. Reflection cannot make an unregistered network prefab safe or network-spawnable.

### 6. Diagnostics are not automatically policy

Snapshots, counters and dashboard feeds are useful for diagnosis. Do not silently turn them into hard eligibility gates unless they are authoritative, complete, fresh enough for the decision, and necessary for correctness.

Keep separate concepts separate:
- Connected: authenticated mod connection exists.
- In lobby: mod reports the active lobby.
- Eligible: identity and policy requirements are satisfied.
- Ready: required subscriptions/configuration and delivery path are active.
- Target currently resolvable: host can find the live avatar at execution time.

A participant can be connected and eligible while a transient game object is not currently resolvable. Handle that as an execution-time failure or retry condition, not as proof that the participant should never have been eligible.

### 7. Main-thread and background-thread boundaries matter

Network I/O can run asynchronously, but Unity/game state and spawn operations must be performed on the Unity main thread. The R.E.P.O. gateway queues incoming messages for the plugin's main-thread update loop before invoking game logic.

Do not call Unity APIs from arbitrary WebSocket receive continuations.

### 8. Deduplicate requests and acknowledge outcomes

Every remote spawn request needs a stable request ID. The host should remember processed IDs and acknowledge duplicates without spawning again. The backend should track dispatch and acknowledgement separately. A successful HTTP response from an ingress endpoint is not proof that a game spawn happened.

## Reusable incident debugging sequence

When a future game integration fails, trace one request through the whole path in order:

1. **Event ingress:** Did EventSub/Event Hub receive the event?
2. **Identity:** Which Twitch broadcaster/channel did the event belong to?
3. **Session:** Is that broadcaster connected to the expected game session?
4. **Lobby:** Does the participant report the same real lobby ID as the host?
5. **Role:** Which connected mod currently has game authority?
6. **Eligibility:** What exact condition made the participant ready or ignored?
7. **Dispatch:** Did the backend send the request to the host? Record request ID and target player ID.
8. **Mod receive:** Did the host mod receive and parse the request?
9. **Main thread:** Was game logic invoked on the game thread?
10. **Target lookup:** Could the host resolve the target player's current avatar?
11. **Validation:** Was the category and selected asset still valid?
12. **Game execution:** Did the native game spawn API accept the request?
13. **Replication:** Did the object appear for the host and at least one remote client?
14. **Acknowledgement:** Did the host ack reach the backend and close the queue item?

At each stage, record a structured outcome. Do not infer a later-stage failure from an earlier-stage success.

## Regression tests to retain

- Host-only lobby: host connects, becomes ready, redeems, and spawns once.
- Host + client: both connect to the same lobby; both become eligible.
- Client redemption: backend routes the client's redemption to the host with the client's actor number.
- Incomplete host snapshot: client remains eligible when omitted from the host's diagnostic player list.
- Missing host snapshot: connected valid participants remain eligible.
- Invalid actor number: participant is rejected with a precise reason.
- Target leaves before dispatch: no wrong-player spawn; safe failure/retry outcome.
- Host leaves or migrates: current authority is recalculated; stale host is not used.
- Duplicate request: one spawn maximum, duplicate is acknowledged safely.
- Multiplayer replication: host-created object is visible to other clients.
- Reconnect: participant can recover without restarting the entire lobby.
- Category disabled or invalid catalogue entry: rejected before invoking the game spawn path.

## Current known-good contract

As of 2026-11-27:
- R.E.P.O. mod version: 0.10.0.
- Host-authoritative spawning is retained.
- The host's periodic Photon snapshot is diagnostic only for participant eligibility.
- The participant's own valid actor number is used for routing.
- The host resolves the live avatar at request execution time.
- Shared network objects are created by the host and replicated by the game's Photon system.
- The multiplayer reward flow has been confirmed working again.
