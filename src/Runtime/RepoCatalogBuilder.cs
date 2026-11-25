using System;
using System.Collections.Generic;

namespace SpectrumRepoMod.Runtime;

/// <summary>
/// Builds the catalogue only from the valuable presets selected by the
/// currently loaded vanilla level. No unrelated game registries are scanned.
/// </summary>
internal static class RepoCatalogBuilder
{
    public static IReadOnlyList<PrefabRef> GetLevelPrefabRefs()
    {
        var level = LevelGenerator.Instance?.Level;
        if (level == null || level.ValuablePresets == null)
            return Array.Empty<PrefabRef>();

        var refs = new List<PrefabRef>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var preset in level.ValuablePresets)
        {
            if (preset == null) continue;
            Add(preset.tiny);
            Add(preset.small);
            Add(preset.medium);
            Add(preset.big);
            Add(preset.wide);
            Add(preset.tall);
            Add(preset.veryTall);
        }

        return refs;

        void Add(IEnumerable<PrefabRef>? source)
        {
            if (source == null) return;
            foreach (var prefab in source)
            {
                if (prefab == null || !prefab.IsValid()) continue;
                var resourcePath = prefab.ResourcePath;
                if (string.IsNullOrWhiteSpace(resourcePath) || !seen.Add(resourcePath)) continue;
                refs.Add(prefab);
            }
        }
    }

    public static RepoCatalogSnapshot Build(RepoRuntimePolicy? policy = null)
    {
        policy ??= new RepoRuntimePolicy();
        var rows = new List<RepoCatalogRow>();
        foreach (var prefab in GetLevelPrefabRefs())
        {
            var displayName = string.IsNullOrWhiteSpace(prefab.PrefabName)
                ? prefab.ResourcePath
                : prefab.PrefabName;
            var id = "valuable:" + prefab.ResourcePath;
            var target = RepoSpawnTarget.ForValuable(prefab);
            var provisional = new RepoCatalogRow("valuable", id, displayName, target, true, string.Empty);
            var allowed = policy.TryValidate(provisional, out var reason);
            rows.Add(new RepoCatalogRow("valuable", id, displayName, target, allowed, reason));
        }
        return new RepoCatalogSnapshot(rows);
    }
}
