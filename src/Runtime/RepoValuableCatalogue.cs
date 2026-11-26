using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SpectrumRepoMod.Runtime;

/// <summary>Discovers and validates valuable prefab references.</summary>
internal static class RepoValuableCatalogue
{
public static void AddRowsList<RepoCatalogRow> rows, RepoRuntimePolicy policy)
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
}
