using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using REPOLib.Extensions;
using REPOLib.Modules;
using REPOLib.Objects;
using SpectrumRepoMod.Rewards;

namespace SpectrumRepoMod.Runtime;

/// <summary>
/// Reads the live REPOLib registries. Only explicitly recognized non-combat
/// shop categories enter the utility pool; everything else fails closed.
/// </summary>
public static class RepoCatalogBuilder
{
    private static readonly HashSet<string> SupportedUtilityTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "drone", "orb", "tracker", "vehicle", "tool", "itemupgrade",
        "playerupgrade", "healthpack", "powercrystals", "cart", "pocketcart"
    };

    public static SpawnCatalogSnapshot<RepoSpawnTarget> Build()
    {
        var candidates = new List<RewardCandidate<RepoSpawnTarget>>();

        foreach (var enemy in Enemies.AllEnemies)
        {
            if (enemy == null || string.IsNullOrWhiteSpace(enemy.name)) continue;
            var id = "enemy:" + enemy.name.Trim();
            candidates.Add(new RewardCandidate<RepoSpawnTarget>(
                RewardCategory.Monster, id, enemy.name, RepoSpawnTarget.ForEnemy(enemy)));
        }

        foreach (var item in Items.AllItems)
        {
            if (item == null || item.prefab == null || !item.prefab.IsValid()) continue;
            var id = "item:" + item.prefab.ResourcePath;
            var classification = ClassifyShopItem(item);
            candidates.Add(new RewardCandidate<RepoSpawnTarget>(
                RewardCategory.UtilityItem,
                id,
                string.IsNullOrWhiteSpace(item.itemName) ? item.prefab.PrefabName : item.itemName,
                RepoSpawnTarget.ForShopItem(item),
                classification));
        }

        foreach (var valuable in Valuables.AllValuables)
        {
            if (valuable == null || !valuable.IsValid()) continue;
            var id = "valuable:" + valuable.ResourcePath;
            candidates.Add(new RewardCandidate<RepoSpawnTarget>(
                RewardCategory.Valuable,
                id,
                valuable.PrefabName,
                RepoSpawnTarget.ForValuable(valuable),
                ShopItemClassification.Valuable));
        }

        return SpawnCatalogSnapshot<RepoSpawnTarget>.Build(candidates, SpawnExclusionList.Default);
    }

    private static ShopItemClassification ClassifyShopItem(Item item)
    {
        var type = item.GetType();
        var field = type.GetField("itemType", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var property = type.GetProperty("itemType", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        object? value = field != null ? field.GetValue(item) : property?.GetValue(item);
        var normalized = Convert.ToString(value)?.Replace("_", "").Replace(" ", "").Trim();
        if (string.IsNullOrWhiteSpace(normalized)) return ShopItemClassification.Unknown;
        return SupportedUtilityTypes.Contains(normalized)
            ? ShopItemClassification.Utility
            : ShopItemClassification.Excluded;
    }
}
