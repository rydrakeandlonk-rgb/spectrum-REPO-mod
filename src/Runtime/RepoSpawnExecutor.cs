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

        if (policy == null)
        {
            result = "The spawn policy is unavailable.";
            return false;
        }
        if (!policy.TryValidate(candidate, out result))
            return false;

        if (string.Equals(candidate.Category, "valuable", StringComparison.Ordinal))
            return TrySpawnValuable(candidate, position, rotation, out result);

        if (string.Equals(candidate.Category, "monster", StringComparison.Ordinal))
            return TrySpawnMonster(candidate, position, out result);

        if (string.Equals(candidate.Category, "utility_item", StringComparison.Ordinal))
            return TrySpawnUtilityItem(candidate, position, rotation, out result);

        result = "This build supports valuables, monsters and approved shop items only.";
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
            spawned = UnityEngine.Object.Instantiate(loadedPrefab, position, rotation);

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

        var before = director.enemiesSpawned.Count;
        generator.EnemySpawn(setup, position);

        // EnemySpawn hands placement and relocation to the game's enemy system.
        // Some enemy types are repositioned immediately, and the director's
        // bookkeeping can update after this call returns. Neither the monster's
        // final position nor an immediate list-count change is a reliable failure
        // signal. A thrown exception remains the failure signal for this API call.
        var after = director.enemiesSpawned.Count;
        if (after <= before)
            Plugin.Log.LogInfo("EnemySpawn returned without an exception; enemy director tracking has not updated yet. Treating the game spawn request as accepted.");

        result = after > before
            ? "Monster spawn accepted and registered by the enemy director."
            : "Monster spawn request accepted by the game; final placement is handled by the enemy director.";
        return true;
    }

    private static bool TrySpawnUtilityItem(RepoCatalogRow candidate, Vector3 position,
        Quaternion rotation, out string result)
    {
        var item = candidate.Target.UtilityItem;
        if (item == null || item.prefab == null || !item.prefab.IsValid())
        {
            result = "The selected shop item is no longer available.";
            return false;
        }

        var prefabRef = item.prefab;
        var loadedPrefab = prefabRef.Prefab;
        if (loadedPrefab == null)
        {
            result = "The selected shop item prefab could not be loaded.";
            return false;
        }

        var expectedAttributes = loadedPrefab.GetComponent<ItemAttributes>() ??
            loadedPrefab.GetComponentInChildren<ItemAttributes>(true);
        if (expectedAttributes == null || expectedAttributes.item != item)
        {
            result = "The selected prefab is not linked to its vanilla item data.";
            return false;
        }

        var spawnRotation = rotation * item.spawnRotationOffset;
        GameObject spawned;
        if (PhotonNetwork.CurrentRoom != null)
            spawned = PhotonNetwork.InstantiateRoomObject(prefabRef.ResourcePath,
                position, spawnRotation, 0, null);
        else
            spawned = UnityEngine.Object.Instantiate(loadedPrefab, position, spawnRotation);

        if (spawned == null)
        {
            result = "The game did not confirm a successful shop item spawn.";
            return false;
        }

        var spawnedAttributes = spawned.GetComponent<ItemAttributes>() ??
            spawned.GetComponentInChildren<ItemAttributes>(true);
        if (spawnedAttributes == null || spawnedAttributes.item != item)
        {
            if (PhotonNetwork.CurrentRoom != null)
                PhotonNetwork.Destroy(spawned);
            else
                UnityEngine.Object.Destroy(spawned);

            result = "The spawned object did not initialize as the selected vanilla shop item.";
            return false;
        }

        result = "Shop item spawn completed.";
        return true;
    }
}
