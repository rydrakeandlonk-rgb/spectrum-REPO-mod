using Photon.Pun;
using UnityEngine;

namespace SpectrumRepoMod.Runtime;

/// <summary>
/// Executes a selected valuable only on the current host. Multiplayer uses
/// Photon room-object instantiation so every lobby member receives one object.
/// </summary>
internal sealed class RepoSpawnExecutor
{
    public bool TrySpawn(
        RepoCatalogRow candidate,
        RepoRuntimePolicy policy,
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

        if (policy == null || !policy.TryValidate(candidate, out result))
            return false;

        if (!string.Equals(candidate.Category, "valuable", System.StringComparison.Ordinal))
        {
            result = "This test build only supports valuable collectibles.";
            return false;
        }

        var prefab = candidate.Target.Valuable;
        if (prefab == null || !prefab.IsValid() || string.IsNullOrWhiteSpace(prefab.ResourcePath))
        {
            result = "The selected valuable is no longer valid in the current level.";
            return false;
        }

        var loadedPrefab = prefab.Prefab;
        if (loadedPrefab == null || loadedPrefab.GetComponent<ValuableObject>() == null)
        {
            result = "The selected prefab is not a valid valuable collectible.";
            return false;
        }

        GameObject spawned;
        if (PhotonNetwork.CurrentRoom != null)
        {
            spawned = PhotonNetwork.InstantiateRoomObject(prefab.ResourcePath, position, rotation, 0, null);
        }
        else
        {
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
            result = "The spawned object did not contain a ValuableObject component.";
            return false;
        }

        valuable.DollarValueSetLogic();
        result = "Valuable spawn completed.";
        return true;
    }
}
