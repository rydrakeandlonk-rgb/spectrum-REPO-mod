using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SpectrumRepoMod.Runtime;

/// <summary>
/// Builds the valuable and monster catalogues, plus an explicit allowlist of
/// verified vanilla upgrades, drones, C.A.R.T.s and utility tools from loaded Item assets.
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

    // Known shop aliases are used for stable display names. Tool entries are
    // additionally gated by exact vanilla behaviour components below.
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

        new("HealthSmall", "Small Health Pack", "upgrade"),
        new("HealthMedium", "Medium Health Pack", "upgrade"),
        new("HealthLarge", "Large Health Pack", "upgrade"),

        

        new("EnergyCrystal", "Energy Crystal", "drone"),
        new("ZeroGravityOrb", "Zero Gravity Orb", "drone"),
        new("Chargingorb", "Charging Orb", "drone"),
        new("Duck Bucket", "Duck Bucket", "drone"),
        new("Phase Bridge R.E.P.O.", "Phase Bridge", "drone"),
        new("Semibot walkiee", "Semibot Walkies", "drone"),
        new("Defibro transparent", "Defibro", "drone"),
        new("TrackerValuable", "Valuable Tracker", "tracker"),
        new("TrackerExtraction", "Extraction Tracker", "tracker"),

        new("CartSmall", "Pocket C.A.R.T.", "cart"),
        new("CartBig", "C.A.R.T.", "cart"),
        new("Scout", "Scout", "cart"),
        new("Hauler", "Hauler", "cart"),

        new("WalkieBox", "Walkie Talkie Box", "walkie_box"),
        new("WalkieTalkie", "Walkie Talkie", "walkie_talkie"),
        new("LeafBlower", "Leaf Blower", "leaf_blower"),
    };

    public static RepoCatalogSnapshot Build(RepoRuntimePolicy? policy = null)
    {
        policy ??= new RepoRuntimePolicy();
        var rows = new List<RepoCatalogRow>();
        AddValuables(rows, policy);
        AddMonsters(rows, policy);
        AddUtilityItems(rows, policy);
        RepoSpawnRegistry.AppendRows(rows, policy);
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
        // The game does not consistently expose shop prefab identifiers through
        // PrefabName. Discover only the approved non-combat families by their
        // runtime components, then use known item-name aliases as a fallback.
        if (RunManager.instance == null)
        {
            Plugin.Log.LogInfo("Shop item catalogue: waiting for an active RunManager.");
            return;
        }

        var loadedItems = Resources.FindObjectsOfTypeAll<Item>();
        var matched = 0;
        var eligible = 0;
        var upgradeMatches = 0;
        var droneMatches = 0;
        var cartMatches = 0;
        var trackerMatches = 0;
        var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in loadedItems)
        {
            if (item == null || item.disabled || item.prefab == null || !item.prefab.IsValid())
                continue;

            var prefab = item.prefab.Prefab;
            if (prefab == null)
                continue;

            var itemName = item.name ?? string.Empty;
            var prefabName = item.prefab.PrefabName ?? string.Empty;
            var resourcePath = item.prefab.ResourcePath ?? string.Empty;
            var itemDisplayName = item.itemName ?? string.Empty;
            var identity = NormalizeIdentity(itemName + " " + itemDisplayName + " " +
                prefabName + " " + resourcePath);

            // Upgrades are identified by their dedicated game component. This
            // avoids relying on prefab display names, which vary between builds.
            var isUpgrade = prefab.GetComponentInChildren<ItemUpgrade>(true) != null;

            // Keep drones on the existing approved list; identify them by any
            // of the item's exposed safe identifiers rather than PrefabName alone.
            var isApprovedDrone = prefab.GetComponentInChildren<ItemDrone>(true) != null ||
                UtilityItemDefinitions.Any(definition =>
                    definition.Kind == "drone" &&
                    IdentityMatches(identity, NormalizeIdentity(definition.PrefabName),
                        NormalizeIdentity(definition.DisplayName)));

            // Trackers are safe utility tools and are discovered by behaviour,
            // not by their particular vanilla asset names.
            var isTracker = prefab.GetComponentInChildren<ItemTracker>(true) != null;

            // C.A.R.T.s must be physical items and explicitly identify as a cart.
            var isCart = item.physicalItem &&
                (identity.Contains("cart") || identity.Contains("scout") || identity.Contains("hauler"));

            // These tools are admitted only by their exact vanilla behaviour
            // components. Names alone never make an otherwise unknown item safe.
            var toolKind = GetApprovedToolKind(prefab);

            if (!isUpgrade && !isApprovedDrone && !isCart && !isTracker && string.IsNullOrEmpty(toolKind))
                continue;

            var kind = isUpgrade ? "upgrade" : isApprovedDrone ? "drone" :
                isCart ? "cart" : isTracker ? "tracker" : toolKind;
            var definition = UtilityItemDefinitions.FirstOrDefault(candidate =>
                candidate.Kind == kind &&
                IdentityMatches(identity, NormalizeIdentity(candidate.PrefabName),
                    NormalizeIdentity(candidate.DisplayName)));

            var path = string.IsNullOrWhiteSpace(resourcePath) ? prefabName : resourcePath;
            if (string.IsNullOrWhiteSpace(path))
                path = itemName;
            if (string.IsNullOrWhiteSpace(path) || !seenPaths.Add(path))
                continue;

            var displayName = definition?.DisplayName;
            if (string.IsNullOrWhiteSpace(displayName))
                displayName = !string.IsNullOrWhiteSpace(itemDisplayName) ? itemDisplayName :
                    !string.IsNullOrWhiteSpace(itemName) ? itemName : prefabName;
            if (string.IsNullOrWhiteSpace(displayName))
                displayName = kind == "upgrade" ? "Shop upgrade" :
                    kind == "drone" ? "Approved drone" :
                    kind == "cart" ? "C.A.R.T." :
                    kind == "tracker" ? "Tracker" :
                    kind == "walkie_box" ? "Walkie Talkie Box" :
                    kind == "walkie_talkie" ? "Walkie Talkie" : "Leaf Blower";

            if (kind == "upgrade") upgradeMatches++;
            else if (kind == "drone") droneMatches++;
            else if (kind == "cart") cartMatches++;
            else if (kind == "tracker") trackerMatches++;

            var id = "utility_item:" + path;
            var target = RepoSpawnTarget.ForUtilityItem(item, kind);
            var provisional = new RepoCatalogRow("utility_item", id, displayName,
                target, true, string.Empty);
            var allowed = policy.TryValidate(provisional, out var reason);
            if (allowed) eligible++;
            else Plugin.Log.LogWarning("Shop item catalogue excluded " + displayName + ": " + reason);
            matched++;
            rows.Add(new RepoCatalogRow("utility_item", id, displayName, target, allowed, reason));
        }

        var toolMatches = matched - upgradeMatches - droneMatches - cartMatches - trackerMatches;
        Plugin.Log.LogInfo("Shop item catalogue diagnostic: loaded Item assets=" + loadedItems.Length +
            ", matched approved non-combat entries=" + matched +
            " (upgrades=" + upgradeMatches + ", drones=" + droneMatches +
            ", carts=" + cartMatches + ", trackers=" + trackerMatches +
            ", utility tools=" + toolMatches + "), eligible=" + eligible +
            ", approved definitions=" + UtilityItemDefinitions.Length + ".");

        static string NormalizeIdentity(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            var chars = value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray();
            return new string(chars);
        }

        static bool IdentityMatches(string identity, string prefabAlias, string displayAlias)
        {
            if (string.IsNullOrWhiteSpace(identity)) return false;
            return (!string.IsNullOrWhiteSpace(prefabAlias) && identity.Contains(prefabAlias)) ||
                (!string.IsNullOrWhiteSpace(displayAlias) && identity.Contains(displayAlias));
        }

        static string GetApprovedToolKind(GameObject prefab)
        {
            if (prefab.GetComponentInChildren<ItemWalkieBox>(true) != null)
                return "walkie_box";
            if (prefab.GetComponentInChildren<ItemWalkieTalkie>(true) != null)
                return "walkie_talkie";
            if (prefab.GetComponentInChildren<ItemLeafBlower>(true) != null)
                return "leaf_blower";
            return string.Empty;
        }
    }
}
