using System;
using System.Collections.Generic;
using SpectrumRepoMod.Runtime;

namespace SpectrumRepoMod.API
{
    /// <summary>Supported non-combat shop-item registration profiles.</summary>
    public enum RepoUtilityItemKind
    {
        Upgrade,
        Drone,
        Cart,
        Tracker,
        NonCombatPhysical
    }

    /// <summary>
    /// Allows compatible mods to expose content that is not present in the
    /// game's normal discovery registries. Register after assets have loaded.
    /// Spectrum revalidates every entry before publishing or spawning it.
    /// </summary>
    public static class RepoSpawnRegistry
    {
        private static readonly object Sync = new();
        private static readonly Dictionary<string, RepoCatalogRow> Entries =
            new(StringComparer.OrdinalIgnoreCase);

        public static bool RegisterValuable(string stableId, string displayName, PrefabRef prefab)
        {
            if (prefab == null || !prefab.IsValid()) return false;
            return Register("valuable", stableId, displayName, RepoSpawnTarget.ForValuable(prefab));
        }

        public static bool RegisterMonster(string stableId, string displayName, EnemySetup setup)
        {
            if (setup == null) return false;
            return Register("monster", stableId, displayName, RepoSpawnTarget.ForMonster(setup));
        }

        public static bool RegisterUtilityItem(string stableId, string displayName, Item item,
            RepoUtilityItemKind kind)
        {
            if (item == null) return false;
            var kindId = kind switch
            {
                RepoUtilityItemKind.Upgrade => "upgrade",
                RepoUtilityItemKind.Drone => "drone",
                RepoUtilityItemKind.Cart => "cart",
                RepoUtilityItemKind.Tracker => "tracker",
                RepoUtilityItemKind.NonCombatPhysical => "noncombat_physical",
                _ => string.Empty
            };
            if (string.IsNullOrEmpty(kindId)) return false;
            return Register("utility_item", stableId, displayName,
                RepoSpawnTarget.ForUtilityItem(item, kindId));
        }

        /// <summary>Remove a registration by category and stable ID.</summary>
        public static bool Unregister(string category, string stableId)
        {
            if (!IsValidText(category, 32) || !IsValidText(stableId, 160)) return false;
            lock (Sync) return Entries.Remove(category.Trim() + ":" + stableId.Trim());
        }

        internal static void AppendRows(List<RepoCatalogRow> rows, RepoRuntimePolicy policy)
        {
            RepoCatalogRow[] snapshot;
            lock (Sync) snapshot = new List<RepoCatalogRow>(Entries.Values).ToArray();

            foreach (var registered in snapshot)
            {
                var allowed = policy.TryValidate(registered, out var reason);
                rows.Add(new RepoCatalogRow(registered.Category, registered.Id,
                    registered.DisplayName, registered.Target, allowed, reason));
            }
        }

        private static bool Register(string category, string stableId, string displayName,
            RepoSpawnTarget target)
        {
            if (!IsValidText(stableId, 160) || !IsValidText(displayName, 128)) return false;
            var id = category + ":" + stableId.Trim();
            var row = new RepoCatalogRow(category, id, displayName.Trim(), target, true, string.Empty);
            lock (Sync) Entries[id] = row;
            return true;
        }

        private static bool IsValidText(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > maxLength) return false;
            foreach (var character in value)
                if (char.IsControl(character)) return false;
            return true;
        }
    }
}
