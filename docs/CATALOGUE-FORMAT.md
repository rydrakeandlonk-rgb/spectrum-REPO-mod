# Vanilla catalogue format

The editable vanilla catalogue source lives at `src/Catalogue/vanilla-catalogue.json`. On first launch, the mod copies this embedded template to:

`BepInEx/config/SpectrumRepoMod/vanilla-catalogue.json`

The mod reads the config copy when rebuilding its runtime catalogue. Existing user edits are never overwritten by updates.

## Entry format

Each vanilla object gets its own entry:

```json
{
  "id": "valuable:verified/resource/path",
  "name": "Verified display name",
  "category": "valuable",
  "tags": ["vanilla", "allowed"]
}
```

- `id` is the stable identifier used by the mod to match a runtime-discovered object.
- `name` is a human-readable label for maintainers.
- `category` identifies the runtime type (initially `valuable`; future categories will be added only after their spawn handlers are validated).
- `tags` are editable per-entry classifications. `blocked` always takes precedence over `allowed`.
- When enforcement is enabled, an entry must be explicitly listed and have the `allowed` tag. Unlisted entries are blocked.

## Safe rollout

`enforceEntries` is currently `false`. This deliberately preserves the working 0.4.0 valuable behaviour while the verified vanilla IDs are collected. The file is already loaded by the mod, and setting `enforceEntries` to `true` activates its per-entry tag decisions.

The initial `entries` array is intentionally empty. No vanilla names or identifiers are guessed or fabricated. Populate it only with IDs verified from the uploaded game assemblies or runtime catalogue. The mod currently discovers valuables from the active level; expanding discovery to the full vanilla catalogue is a separate step from enabling tag enforcement.
