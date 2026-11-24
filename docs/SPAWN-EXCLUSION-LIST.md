# Universal No Spawn List

The No Spawn List is a category-scoped deny list shared by all R.E.P.O. spawn paths.

## Categories

- **Monsters:** block individual enemy catalogue IDs.
- **Shop items:** block individual eligible utility-item IDs.
- **Valuables:** block individual valuable catalogue IDs.

Every entry stores its category, stable catalogue ID, and human-readable reason. An entry only affects the matching category, even if another catalogue happens to use the same ID.

## Global rules

- The `weapon` tag is excluded globally by default.
- Items classified as `Weapon` or `Excluded` are rejected independently of list configuration.
- Unknown shop-item classifications remain ineligible.
- Global tags can be added for rules that should apply across all spawn categories.

## Enforcement

1. Apply the list while constructing the current catalogue snapshot.
2. Apply it again immediately before host-side spawn execution.
3. Return the exclusion reason in diagnostics and Control Center catalogue inspection.
4. Refreshing the catalogue must never bypass the list.

## Tuning

Add a category-specific entry with a stable game catalogue ID and a reason, such as `Balance`, `Softlock risk`, or `Unsupported spawn behaviour`. This lets the team tune monster difficulty, item utility, and valuable rewards independently without changing selection logic.

The runtime catalogue audit must identify actual IDs from the installed game and REPOLib registries. Do not guess IDs or populate the production list with display names.
