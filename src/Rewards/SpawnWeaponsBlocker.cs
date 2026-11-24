using System;
using System.Collections.Generic;

namespace SpectrumRepoMod.Rewards;

/// <summary>
/// The single source of truth for explicitly blocked R.E.P.O. weapon items.
/// Keep this list empty during catalogue validation. Add only confirmed,
/// stable item catalogue IDs (including the "item:" prefix) with a comment
/// identifying the exact in-game item when exclusions are introduced.
/// </summary>
public static class SpawnWeaponsBlocker
{
    private static readonly HashSet<string> BlockedItemIds = new(StringComparer.OrdinalIgnoreCase)
    {
        // Intentionally empty for the unrestricted catalogue test.
        // Add confirmed IDs here one at a time; do not use display names or broad tags.
    };

    public static bool IsBlocked(RewardCategory category, string catalogueId) =>
        category == RewardCategory.UtilityItem &&
        !string.IsNullOrWhiteSpace(catalogueId) &&
        BlockedItemIds.Contains(catalogueId.Trim());
}
