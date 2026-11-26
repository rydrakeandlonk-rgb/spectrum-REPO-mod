using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

            var kind = candidate.Target.UtilityItemKind;
            if (!item.physicalItem && kind != "upgrade")
            {
                reason = "The utility item is not marked as a physical item by the game.";
                return false;
            }

            if (kind == "upgrade")
            {
                if (prefab.GetComponentInChildren<ItemUpgrade>(true) == null)
                {
                    reason = "The upgrade prefab is missing its vanilla upgrade component.";
                    return false;
                }
            }
            else if (kind == "drone")
            {
                if (prefab.GetComponentInChildren<ItemDrone>(true) == null)
                {
                    reason = "The drone prefab is missing its vanilla drone component.";
                    return false;
                }
            }
            else if (kind == "cart")
            {
                // Physical-item status was checked above. Cart discovery also
                // requires an explicit cart identity in the catalogue builder.
            }
            else if (kind == "tracker")
            {
                if (prefab.GetComponentInChildren<ItemTracker>(true) == null)
                {
                    reason = "The tracker prefab is missing its vanilla tracker behaviour component.";
                    return false;
                }
            }
            else if (kind == "noncombat_physical")
            {
                if (!item.physicalItem)
                {
                    reason = "The registered non-combat item is not marked as a physical item.";
                    return false;
                }

                var components = prefab.GetComponentsInChildren<MonoBehaviour>(true);
                var combatComponent = components.FirstOrDefault(component =>
                    component != null && IsCombatComponentName(component.GetType().Name));
                if (combatComponent != null)
                {
                    reason = "The registered item contains a known combat component (" +
                        combatComponent.GetType().Name + ").";
                    return false;
                }
            }
            else if (kind == "walkie_box")
            {
                var box = prefab.GetComponentInChildren<ItemWalkieBox>(true);
                if (box == null || box.walkieBoxItem != item || box.walkieTalkieItem == item)
                {
                    reason = "The walkie box is missing its vanilla box behaviour or item reference.";
                    return false;
                }

                var talkieItem = box.walkieTalkieItem;
                if (talkieItem == null || talkieItem.disabled ||
                    talkieItem.prefab == null || !talkieItem.prefab.IsValid())
                {
                    reason = "The walkie box does not reference a valid vanilla walkie-talkie item.";
                    return false;
                }

                var talkiePrefab = talkieItem.prefab.Prefab;
                var talkieAttributes = talkiePrefab == null ? null :
                    (talkiePrefab.GetComponent<ItemAttributes>() ??
                     talkiePrefab.GetComponentInChildren<ItemAttributes>(true));
                if (talkiePrefab == null ||
                    talkiePrefab.GetComponentInChildren<ItemWalkieTalkie>(true) == null ||
                    talkieAttributes == null || talkieAttributes.item != talkieItem)
                {
                    reason = "The walkie box's referenced walkie-talkie prefab is not correctly linked.";
                    return false;
                }
            }
            else if (kind == "walkie_talkie")
            {
                if (prefab.GetComponentInChildren<ItemWalkieTalkie>(true) == null ||
                    prefab.GetComponentInChildren<ItemEquippable>(true) == null ||
                    prefab.GetComponentInChildren<ItemToggle>(true) == null)
                {
                    reason = "The walkie-talkie prefab is missing one or more required vanilla behaviour components.";
                    return false;
                }
            }
            else if (kind == "leaf_blower")
            {
                if (prefab.GetComponentInChildren<ItemLeafBlower>(true) == null ||
                    prefab.GetComponentInChildren<ItemBattery>(true) == null ||
                    prefab.GetComponentInChildren<ItemToggle>(true) == null)
                {
                    reason = "The leaf blower is missing one or more required vanilla behaviour components.";
                    return false;
                }
            }
            else
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

    private static bool IsCombatComponentName(string name)
    {
        return name.IndexOf("gun", StringComparison.OrdinalIgnoreCase) >= 0 ||
            name.IndexOf("grenade", StringComparison.OrdinalIgnoreCase) >= 0 ||
            name.IndexOf("mine", StringComparison.OrdinalIgnoreCase) >= 0 ||
            name.IndexOf("bomb", StringComparison.OrdinalIgnoreCase) >= 0 ||
            name.IndexOf("cannon", StringComparison.OrdinalIgnoreCase) >= 0 ||
            name.IndexOf("weapon", StringComparison.OrdinalIgnoreCase) >= 0 ||
            name.IndexOf("rocket", StringComparison.OrdinalIgnoreCase) >= 0 ||
            name.IndexOf("explosive", StringComparison.OrdinalIgnoreCase) >= 0 ||
            name.IndexOf("projectile", StringComparison.OrdinalIgnoreCase) >= 0;
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
