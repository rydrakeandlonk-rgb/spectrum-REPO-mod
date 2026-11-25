using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SpectrumRepoMod.Runtime;

/// <summary>
/// Builds from all currently loaded vanilla valuable presets and enemy setups,
/// plus the active run's registries. It does not invent catalogue IDs.
/// </summary>
internal static class RepoCatalogBuilder
{
    public static RepoCatalogSnapshot Build(RepoRuntimePolicy? policy = null)
    {
        policy ??= new RepoRuntimePolicy();
        var rows = new List<RepoCatalogRow>();
        AddValuables(rows, policy);
        AddMonsters(rows, policy);
        return new RepoCatalogSnapshot(rows);
    }

    private static void AddValuables(List<RepoCatalogRow> rows, RepoRuntimePolicy policy)
    {
        var refs = new List<PrefabRef>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var preset in Resources.FindObjectsOfTypeAll<LevelValuables>())
            AddPreset(preset);
        var level = LevelGenerator.Instance?.Level;
        if (level?.ValuablePresets != null)
            foreach (var preset in level.ValuablePresets)
                AddPreset(preset);

        foreach (var prefab in refs)
        {
            var displayName = string.IsNullOrWhiteSpace(prefab.PrefabName)
                ? prefab.ResourcePath
                : prefab.PrefabName;
            var id = "valuable:" + prefab.ResourcePath;
            var provisional = new RepoCatalogRow("valuable", id, displayName,
                RepoSpawnTarget.ForValuable(prefab), true, string.Empty);
            var allowed = policy.TryValidate(provisional, out var reason);
            rows.Add(new RepoCatalogRow("valuable", id, displayName, provisional.Target, allowed, reason));
        }

        void AddPreset(LevelValuables? preset)
        {
            if (preset == null) return;
            Add(preset.tiny);
            Add(preset.small);
            Add(preset.medium);
            Add(preset.big);
            Add(preset.wide);
            Add(preset.tall);
            Add(preset.veryTall);
        }

        void Add(IEnumerable<PrefabRef>? source)
        {
            if (source == null) return;
            foreach (var prefab in source)
            {
                if (prefab == null || !prefab.IsValid()) continue;
                var path = prefab.ResourcePath;
                if (string.IsNullOrWhiteSpace(path) || !seen.Add(path)) continue;
                refs.Add(prefab);
            }
        }
    }

    private static void AddMonsters(List<RepoCatalogRow> rows, RepoRuntimePolicy policy)
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
