using System;
using System.Collections.Generic;
using System.Linq;

namespace SpectrumRepoMod.Runtime;

/// <summary>Category policy for supported, non-weapon spawn types.</summary>
internal sealed class RepoRuntimePolicy
{
    public bool ValuablesEnabled { get; }
    public bool MonstersEnabled { get; }
    public bool UtilityItemsEnabled { get; }

    public RepoRuntimePolicy(bool valuablesEnabled = true, bool monstersEnabled = true,
        bool utilityItemsEnabled = true)
    {
        ValuablesEnabled = valuablesEnabled;
        MonstersEnabled = monstersEnabled;
        UtilityItemsEnabled = utilityItemsEnabled;
    }

    public bool TryValidate(RepoCatalogRow? candidate, out string reason)
    {
        if (candidate == null)
        {
            reason = "The requested catalogue entry is missing.";
            return false;
        }

        if (string.Equals(candidate.Category, "valuable", StringComparison.Ordinal))
        {
            if (!ValuablesEnabled)
            {
                reason = "Valuable spawning is disabled in Control Center.";
                return false;
            }
            if (candidate.Target.Valuable == null || !candidate.Target.Valuable.IsValid())
            {
                reason = "The valuable is no longer available.";
                return false;
            }
        }
        else if (string.Equals(candidate.Category, "monster", StringComparison.Ordinal))
        {
            if (!MonstersEnabled)
            {
                reason = "Monster spawning is disabled in Control Center.";
                return false;
            }
            var setup = candidate.Target.Monster;
            if (setup == null || setup.spawnObjects == null || setup.spawnObjects.Count == 0)
            {
                reason = "The monster setup has no spawnable prefab references.";
                return false;
            }
            if (setup.spawnObjects.Exists(prefab => prefab == null || !prefab.IsValid()))
            {
                reason = "The monster setup contains an invalid prefab reference.";
                return false;
            }
        }
        else if (string.Equals(candidate.Category, "utility_item", StringComparison.Ordinal))
        {
            if (!UtilityItemsEnabled)
            {
                reason = "Shop item spawning is disabled in Control Center.";
                return false;
            }

            var item = candidate.Target.UtilityItem;
            if (item == null || item.disabled || item.prefab == null || !item.prefab.IsValid())
            {
                reason = "The shop item is disabled or its prefab is unavailable.";
                return false;
            }

            var prefab = item.prefab.Prefab;
            if (prefab == null)
            {
                reason = "The shop item prefab could not be loaded.";
                return false;
            }

            var attributes = prefab.GetComponent<ItemAttributes>() ??
                prefab.GetComponentInChildren<ItemAttributes>(true);
            if (attributes == null || attributes.item != item)
            {
                reason = "The prefab is not linked to the expected vanilla item data.";
                return false;
            }

            if (candidate.Target.UtilityItemKind == "upgrade" &&
                prefab.GetComponentInChildren<ItemUpgrade>(true) == null)
            {
                reason = "The upgrade prefab is missing its vanilla upgrade component.";
                return false;
            }

            if (candidate.Target.UtilityItemKind == "drone" &&
                prefab.GetComponentInChildren<ItemDrone>(true) == null)
            {
                reason = "The drone prefab is missing its vanilla drone component.";
                return false;
            }

            if (candidate.Target.UtilityItemKind == "cart" && !item.physicalItem)
            {
                reason = "The cart item is not marked as a physical item by the game.";
                return false;
            }

            if (candidate.Target.UtilityItemKind != "upgrade" &&
                candidate.Target.UtilityItemKind != "drone" &&
                candidate.Target.UtilityItemKind != "cart")
            {
                reason = "The shop item is outside the approved Tools catalogue.";
                return false;
            }
        }
        else
        {
            reason = "This build supports valuables, monsters and approved shop items only.";
            return false;
        }

        reason = string.Empty;
        return true;
    }
}

internal sealed class RepoCatalogRow
{
    public string Category { get; }
    public string Id { get; }
    public string DisplayName { get; }
    public RepoSpawnTarget Target { get; }
    public bool Allowed { get; }
    public string ExclusionReason { get; }

    public RepoCatalogRow(string category, string id, string displayName, RepoSpawnTarget target,
        bool allowed, string exclusionReason)
    {
        Category = category;
        Id = id;
        DisplayName = displayName;
        Target = target;
        Allowed = allowed;
        ExclusionReason = exclusionReason;
    }
}

internal sealed class RepoCatalogSnapshot
{
    public IReadOnlyList<RepoCatalogRow> Rows { get; }
    public IReadOnlyList<RepoCatalogRow> AllowedCandidates { get; }

    public RepoCatalogSnapshot(IEnumerable<RepoCatalogRow> rows)
    {
        Rows = rows.ToArray();
        AllowedCandidates = Rows.Where(row => row.Allowed).ToArray();
    }

    public static RepoCatalogSnapshot Empty { get; } = new(Array.Empty<RepoCatalogRow>());
}
