using System;
using System.Collections.Generic;
using System.Linq;
using SpectrumRepoMod.Rewards;

namespace SpectrumRepoMod.Runtime;

/// <summary>
/// Builds the live catalogue from the valuable presets selected by the
/// current vanilla level. It deliberately does not scan unrelated registries.
/// </summary>
public static class RepoCatalogBuilder
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
            if (preset == null)
                continue;

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
            if (source == null)
                return;

            foreach (var prefab in source)
            {
                if (prefab == null || !prefab.IsValid())
                    continue;

                var resourcePath = prefab.ResourcePath;
                if (string.IsNullOrWhiteSpace(resourcePath) || !seen.Add(resourcePath))
                    continue;

                refs.Add(prefab);
            }
        }
    }

    public static SpawnCatalogSnapshot<RepoSpawnTarget> Build(SpawnPolicy? policy = null)
    {
        var candidates = new List<RewardCandidate<RepoSpawnTarget>>();

        foreach (var prefab in GetLevelPrefabRefs())
        {
            var displayName = string.IsNullOrWhiteSpace(prefab.PrefabName)
                ? prefab.ResourcePath
                : prefab.PrefabName;
            var id = "valuable:" + prefab.ResourcePath;

            candidates.Add(new RewardCandidate<RepoSpawnTarget>(
                RewardCategory.Valuable,
                id,
                displayName,
                RepoSpawnTarget.ForValuable(prefab)));
        }

        return policy == null
            ? SpawnCatalogSnapshot<RepoSpawnTarget>.Build(candidates)
            : SpawnCatalogSnapshot<RepoSpawnTarget>.Build(candidates, policy);
    }
}
