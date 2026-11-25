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
- `category` identifies the runtime type. The shared tag limiter can classify `valuable`, `monster`, and `shop-utility` entries independently. A category is not spawn-enabled merely because it appears in the review catalogue; its runtime discovery and spawn handler must be validated separately.
- `tags` are editable per-entry classifications. `blocked` always takes precedence over `allowed`.
- When enforcement is enabled, an entry must be explicitly listed and have the `allowed` tag. Unlisted entries are blocked.

## Safe rollout

`enforceEntries` is currently `false`. This deliberately preserves the working 0.4.0 valuable behaviour while the verified vanilla IDs are collected. The file is already loaded by the mod, and setting `enforceEntries` to `true` activates its per-entry tag decisions.

The initial `entries` array is intentionally empty. No vanilla names or identifiers are guessed or fabricated. Populate it only with IDs verified by the read-only in-game extractor and manual review. The catalogue is a review and policy data file: it does not itself add discovery or spawning support for a category. The working runtime currently discovers valuables from the active level; monster and shop-utility execution must be separately validated before those categories can be enabled in the live mod.
