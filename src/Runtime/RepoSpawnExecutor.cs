using Photon.Pun;
using SpectrumRepoMod.Rewards;
using UnityEngine;

namespace SpectrumRepoMod.Runtime;

/// <summary>
/// Executes valuable spawns only on the authoritative game host, following
/// the vanilla game's single-player and multiplayer instantiation paths.
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

        if (RunManager.instance == null || LevelGenerator.Instance == null ||
            GameManager.instance == null || ValuableDirector.instance == null)
        {
            result = "There is no active R.E.P.O. valuable session.";
            return false;
        }

        if (policy == null)
        {
            result = "The spawn policy is unavailable.";
            return false;
        }

        if (!policy.TryValidateSpawn(candidate, out result))
            return false;

        if (candidate.Category != RewardCategory.Valuable)
        {
            result = "This test build only supports valuable requests.";
            return false;
        }

        var prefab = candidate.Value.Valuable;
        if (prefab == null || !prefab.IsValid())
        {
            result = "The selected valuable is no longer valid in the current level.";
            return false;
        }

        GameObject? spawned;
        if (GameManager.instance.gameMode != 0)
        {
            if (string.IsNullOrWhiteSpace(prefab.ResourcePath))
            {
                result = "The valuable has no multiplayer resource path.";
                return false;
            }

            spawned = PhotonNetwork.InstantiateRoomObject(
                prefab.ResourcePath, position, rotation, 0, null);
        }
        else
        {
            var loadedPrefab = prefab.Prefab;
            if (loadedPrefab == null)
            {
                result = "The vanilla game could not load the valuable prefab.";
                return false;
            }

            spawned = Object.Instantiate(loadedPrefab, position, rotation);
        }

        if (spawned == null)
        {
            result = "The game did not confirm a successful valuable spawn.";
            return false;
        }

        var valuable = spawned.GetComponent<ValuableObject>();
        if (valuable == null)
        {
            result = "The selected prefab did not contain a ValuableObject component.";
            return false;
        }

        valuable.DollarValueSetLogic();
        result = "Valuable spawn completed.";
        return true;
    }
}
