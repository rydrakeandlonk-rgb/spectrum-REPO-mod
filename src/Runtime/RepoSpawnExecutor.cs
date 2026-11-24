using REPOLib.Modules;
using SpectrumRepoMod.Rewards;
using UnityEngine;

namespace SpectrumRepoMod.Runtime;

/// <summary>
/// Executes a selected catalogue entry only on the authoritative game host.
/// The caller supplies a validated world position and rotation.
/// </summary>
public sealed class RepoSpawnExecutor
{
    public bool TrySpawn(
        RewardCandidate<RepoSpawnTarget> candidate,
        SpawnPolicy policy,
        Vector3 position,
        Quaternion rotation,
        out string result)
    {
        if (!SemiFunc.IsMasterClientOrSingleplayer())
        {
            result = "Only the current game host can execute a shared spawn.";
            return false;
        }

        if (RunManager.instance == null || LevelGenerator.Instance == null)
        {
            result = "There is no active R.E.P.O. run.";
            return false;
        }

        if (policy == null)
        {
            result = "The spawn policy is unavailable.";
            return false;
        }

        if (!policy.TryValidateSpawn(candidate, out result))
            return false;

        var target = candidate.Value;
        switch (candidate.Category)
        {
            case RewardCategory.Monster:
                if (target.Enemy == null)
                {
                    result = "The selected monster is no longer in the live catalogue.";
                    return false;
                }
                var enemies = Enemies.SpawnEnemy(target.Enemy, position, rotation);
                if (enemies == null || enemies.Count == 0)
                {
                    result = "The game did not confirm a successful monster spawn.";
                    return false;
                }
                result = "Monster spawn completed.";
                return true;

            case RewardCategory.UtilityItem:
                if (target.ShopItem == null)
                {
                    result = "The selected shop item is no longer in the live catalogue.";
                    return false;
                }
                if (Items.SpawnItem(target.ShopItem, position, rotation) == null)
                {
                    result = "The game did not confirm a successful shop-item spawn.";
                    return false;
                }
                result = "Shop-item spawn completed.";
                return true;

            case RewardCategory.Valuable:
                if (target.Valuable == null)
                {
                    result = "The selected valuable is no longer in the live catalogue.";
                    return false;
                }
                if (Valuables.SpawnValuable(target.Valuable, position, rotation) == null)
                {
                    result = "The game did not confirm a successful valuable spawn.";
                    return false;
                }
                result = "Valuable spawn completed.";
                return true;

            default:
                result = "Unsupported reward category.";
                return false;
        }
    }
}
