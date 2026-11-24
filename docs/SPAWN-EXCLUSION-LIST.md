# Spawn exclusion list

The R.E.P.O. reward catalogue uses a central deny list instead of hardcoding item restrictions throughout selection and spawn code.

## Default policy

- The `weapon` tag is excluded globally.
- Entries explicitly classified as `Weapon` or `Excluded` are rejected even if their tags are incomplete.
- Unknown classifications are not eligible.
- Specific item IDs can be added to the deny list for softlocks, broken interactions, or unsafe spawn behaviour.

## Runtime contract

1. The catalogue adapter supplies a stable item ID, classification, and normalized content tags.
2. The exclusion list filters candidates before random selection.
3. The host-side spawn executor checks the same exclusion list again immediately before execution.
4. Excluded items are reported in diagnostics with their stable ID and exclusion reason; they are never silently re-added by a category refresh.
5. The list is configuration/data, not a series of scattered conditionals.

## Extending the list

Add exact stable item IDs to the configured `excludedItemIds` collection, or add a tag to `excludedTags`. IDs are case-insensitive. The initial default excludes the `weapon` tag. The first runtime catalogue audit should enumerate the game's shop IDs and tag/classification metadata so known softlock-prone entries can be added by their actual stable IDs.
