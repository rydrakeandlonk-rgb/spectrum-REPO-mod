using System;
using Photon.Pun;
using UnityEngine;

namespace SpectrumRepoMod.Runtime;

/// <summary>Executes supported spawns only on the current host.</summary>
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

        if (RunManager.instance == null || LevelGenerator.Instance == null || GameManager.instance == null)
        {
            result = "There is no active R.E.P.O. run.";
            return false;
        }

        if (policy == null || !policy.TryValidate(candidate, out result))
            return false;

        if (string.Equals(candidate.Category, "valuable", StringComparison.Ordinal))
            return TrySpawnValuable(candidate, position, rotation, out result);

        if (string.Equals(candidate.Category, "monster", StringComparison.Ordinal))
            return TrySpawnMonster(candidate, position, out result);

        result = "This build supports valuables and monsters only.";
        return false;
    }

    private static bool TrySpawnValuable(RepoCatalogRow candidate, Vector3 position,
        Quaternion rotation, out string result)
    {
        if (ValuableDirector.instance == null)
        {
            result = "The valuable director is not ready.";
            return false;
        }

        var prefab = candidate.Target.Valuable;
        if (prefab == null || !prefab.IsValid())
        {
            result = "The selected valuable is no longer valid.";
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
            spawned = PhotonNetwork.InstantiateRoomObject(prefab.ResourcePath, position, rotation, 0, null);
        else
            spawned = Object.Instantiate(loadedPrefab, position, rotation);

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

    private static bool TrySpawnMonster(RepoCatalogRow candidate, Vector3 position, out string result)
    {
        var setup = candidate.Target.Monster;
        if (setup == null || setup.spawnObjects == null || setup.spawnObjects.Count == 0)
        {
            result = "The selected monster setup is no longer available.";
            return false;
        }

        foreach (var prefabRef in setup.spawnObjects)
        {
            if (prefabRef == null || !prefabRef.IsValid() || prefabRef.Prefab == null)
            {
                result = "A monster prefab could not be loaded; no spawn was attempted.";
                return false;
            }
        }

        var generator = LevelGenerator.Instance;
        var director = EnemyDirector.instance;
        if (generator == null || director == null)
        {
            result = "The enemy director is not ready.";
            return false;
        }

        var before = generator.EnemiesSpawnTarget;
        generator.EnemySpawn(setup, position);
        if (generator.EnemiesSpawnTarget <= before)
        {
            result = "The game did not confirm a successful monster spawn.";
            return false;
        }

        result = "Monster spawn completed.";
        return true;
    }
}
