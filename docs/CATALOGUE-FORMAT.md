# Vanilla catalogue format

The editable vanilla catalogue lives at `src/Catalogue/vanilla-catalogue.json`.

## Entry format

Each vanilla object gets its own entry:

```json
{
  "id": "valuable:example/resource/path",
  "name": "Example display name",
  "category": "valuable",
  "tags": ["vanilla", "allowed"]
}
```

- `id` is the stable identifier used by the mod to match a runtime-discovered object.
- `name` is a human-readable label for maintainers.
- `category` identifies the runtime type (initially `valuable`; future categories will be added only after their spawn handlers are validated).
- `tags` are editable per-entry classifications. The planned policy tags are `allowed` and `blocked`; `blocked` always takes precedence. Entries without an explicit `allowed` tag are not eligible once catalogue enforcement is enabled.

## Important

The file is intentionally an empty schema foundation for now. No vanilla names or identifiers are guessed or fabricated. The current 0.4.0 runtime catalogue and valuable-only behaviour remain unchanged while the actual identifiers are extracted from the uploaded game assemblies and runtime catalogue. The next implementation step is to populate this file with verified entries and connect its tag policy to the runtime and Control Center.
