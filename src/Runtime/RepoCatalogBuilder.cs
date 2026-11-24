using System;
using System.Collections.Generic;
using System.Linq;
using REPOLib.Extensions;
using REPOLib.Modules;
using REPOLib.Objects;
using SpectrumRepoMod.Rewards;

namespace SpectrumRepoMod.Runtime;

/// <summary>
/// Reads the live REPOLib registries. All valid shop items enter the initial
/// utility catalogue; only exact IDs in SpawnWeaponsBlocker can be blocked.
/// </summary>
public static class RepoCatalogBuilder
{
    public static SpawnCatalogSnapshot<RepoSpawnTarget> Build(SpawnPolicy? policy = null)
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
            candidates.Add(new RewardCandidate<RepoSpawnTarget>(
                RewardCategory.UtilityItem,
                id,
                string.IsNullOrWhiteSpace(item.itemName) ? item.prefab.PrefabName : item.itemName,
                RepoSpawnTarget.ForShopItem(item),
                ShopItemClassification.Unknown));
        }

        foreach (var valuable in Valuables.AllValuables)
        {
            if (valuable == null || !valuable.IsValid()) continue;
            var id = "valuable:" + valuable.ResourcePath;
            candidates.Add(new RewardCandidate<RepoSpawnTarget>(
                RewardCategory.Valuable, id, valuable.PrefabName,
                RepoSpawnTarget.ForValuable(valuable), ShopItemClassification.Valuable));
        }

        return policy == null
            ? SpawnCatalogSnapshot<RepoSpawnTarget>.Build(candidates)
            : SpawnCatalogSnapshot<RepoSpawnTarget>.Build(candidates, policy);
    }
}
