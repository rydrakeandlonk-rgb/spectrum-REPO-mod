using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SpectrumRepoMod.Runtime;

/// <summary>Discovers and validates loaded enemy setup definitions.</summary>
internal static class RepoMonsterCatalogue
{
public static void AddRows(List<RepoCatalogRow> rows, RepoRuntimePolicy policy)
    {
        var setups = new List<EnemySetup>();
        var seenObjects = new HashSet<int>();

        void Add(IEnumerable<EnemySetup>? source)
        {
            if (source == null) return;
            foreach (var setup in source)
            {
                if (setup == null) continue;
                var instanceId = setup.GetInstanceID();
                if (seenObjects.Add(instanceId))
                    setups.Add(setup);
            }
        }

        Add(Resources.FindObjectsOfTypeAll<EnemySetup>());
        var director = EnemyDirector.instance;
        if (director != null)
        {
            Add(director.enemiesDifficulty1);
            Add(director.enemiesDifficulty2);
            Add(director.enemiesDifficulty3);
        }

        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var setup in setups)
        {
            if (setup.spawnObjects == null || setup.spawnObjects.Count == 0) continue;
            var paths = setup.spawnObjects
                .Where(prefab => prefab != null && prefab.IsValid())
                .Select(prefab => prefab.ResourcePath)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            if (paths.Length == 0) continue;

            var name = string.IsNullOrWhiteSpace(setup.name) ? "Unnamed enemy setup" : setup.name;
            var id = "monster:" + Uri.EscapeDataString(name) + ":" +
                string.Join("|", paths.Select(Uri.EscapeDataString));
            if (!seenIds.Add(id)) continue;

            var provisional = new RepoCatalogRow("monster", id, name,
                RepoSpawnTarget.ForMonster(setup), true, string.Empty);
            var allowed = policy.TryValidate(provisional, out var reason);
            rows.Add(new RepoCatalogRow("monster", id, name, provisional.Target, allowed, reason));
        }
    }
}
