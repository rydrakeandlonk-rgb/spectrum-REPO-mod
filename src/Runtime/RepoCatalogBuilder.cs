using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SpectrumRepoMod.Runtime;

/// <summary>
/// Builds the valuable and monster catalogues, plus a strict allowlist of
/// verified vanilla upgrades, drones and C.A.R.T.s from loaded Item assets.
/// </summary>
internal static class RepoCatalogBuilder
{
    private sealed class UtilityItemDefinition
    {
        public string PrefabName { get; }
        public string DisplayName { get; }
        public string Kind { get; }

        public UtilityItemDefinition(string prefabName, string displayName, string kind)
        {
            PrefabName = prefabName;
            DisplayName = displayName;
            Kind = kind;
        }
    }

    // Names are taken from the Purchased Items page's Appearance column.
    // This intentionally excludes all other shop categories and the unobtainable
    // "Throw" upgrade.
    private static readonly UtilityItemDefinition[] UtilityItemDefinitions =
    {
        new("Strength Upgrade", "Strength Upgrade", "upgrade"),
        new("Range Upgrade", "Range Upgrade", "upgrade"),
        new("Stamina Upgrade", "Stamina Upgrade", "upgrade"),
        new("Sprint Speed Upgrade", "Sprint Speed Upgrade", "upgrade"),
        new("Crouch Rest Upgrade", "Crouch Rest Upgrade", "upgrade"),
        new("Upgrade Health Albedo", "Health Upgrade", "upgrade"),
        new("Player Count Upgrade", "Map Player Count Upgrade", "upgrade"),
        new("Tumble Launch Upgrade", "Tumble Launch Upgrade", "upgrade"),
        new("Tumble Wings Upgrade", "Tumble Wings Upgrade", "upgrade"),
        new("Extra Jump Upgrade", "Extra Jump Upgrade", "upgrade"),
        new("Tumble Climb Upgrade", "Tumble Climb Upgrade", "upgrade"),
        new("Death Head Battery Upgrade", "Death Head Battery Upgrade", "upgrade"),

        new("DroneRecharge", "Recharge Drone", "drone"),
        new("DroneIndestructible", "Indestructible Drone", "drone"),
        new("DroneRoll", "Roll Drone", "drone"),
        new("DroneFeather", "Feather Drone", "drone"),
        new("DroneZeroGravity", "Zero Gravity Drone", "drone"),

        new("CartSmall", "Pocket C.A.R.T.", "cart"),
        new("CartBig", "C.A.R.T.", "cart")
    };

    private static readonly Dictionary<string, UtilityItemDefinition> UtilityItemsByPrefab =
        UtilityItemDefinitions.ToDictionary(definition => definition.PrefabName,
            StringComparer.OrdinalIgnoreCase);

    public static RepoCatalogSnapshot Build(RepoRuntimePolicy? policy = null)
    {
        policy ??= new RepoRuntimePolicy();
        var rows = new List<RepoCatalogRow>();
        AddValuables(rows, policy);
        AddMonsters(rows, policy);
        AddUtilityItems(rows, policy);
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

    private static void AddUtilityItems(List<RepoCatalogRow> rows, RepoRuntimePolicy policy)
    {
        // PrefabRef.Prefab resolves through RunManager's active resource pools.
        if (RunManager.instance == null) return;

        var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in Resources.FindObjectsOfTypeAll<Item>())
        {
            if (item == null || item.prefab == null || !item.prefab.IsValid())
                continue;

            var prefabName = item.prefab.PrefabName;
            if (string.IsNullOrWhiteSpace(prefabName) ||
                !UtilityItemsByPrefab.TryGetValue(prefabName, out var definition))
                continue;

            var path = item.prefab.ResourcePath;
            if (string.IsNullOrWhiteSpace(path) || !seenPaths.Add(path))
                continue;

            var id = "utility_item:" + path;
            var target = RepoSpawnTarget.ForUtilityItem(item, definition.Kind);
            var provisional = new RepoCatalogRow("utility_item", id, definition.DisplayName,
                target, true, string.Empty);
            var allowed = policy.TryValidate(provisional, out var reason);
            rows.Add(new RepoCatalogRow("utility_item", id, definition.DisplayName,
                target, allowed, reason));
        }
    }
}
