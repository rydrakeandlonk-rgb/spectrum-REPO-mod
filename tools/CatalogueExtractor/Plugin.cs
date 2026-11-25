using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using UnityEngine;

namespace SpectrumRepoCatalogueExtractor;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class Plugin : BaseUnityPlugin
{
    private const string PluginGuid = "spectrum.repo.catalogue.extractor";
    private const string PluginName = "Spectrum R.E.P.O. Catalogue Extractor";
    private const string PluginVersion = "0.1.0";

    private static readonly string[] ValuableGroups =
    {
        "tiny", "small", "medium", "big", "wide", "tall", "veryTall"
    };

    // Only known non-weapon utility categories are exported from the shop registry.
    // Unknown item types are deliberately omitted rather than guessed.
    private static readonly string[] SafeShopUtilityTypes =
    {
        "Drone", "Upgrade", "Health", "HealthPack"
    };

    private void Awake()
    {
        Logger.LogInfo("Read-only catalogue extractor loaded. Press F9 in-game to export the currently loaded vanilla registries.");
        Logger.LogInfo("This tool does not spawn, modify, or block game objects.");
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F9))
            ExportCatalogue();
    }

    private void ExportCatalogue()
    {
        try
        {
            var document = new ExtractionDocument();
            var entries = new List<ExtractionEntry>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var gameAssembly = FindGameAssembly();

            if (gameAssembly == null)
            {
                Logger.LogError("Could not find Assembly-CSharp. No catalogue was exported.");
                return;
            }

            CollectValuables(gameAssembly, entries, seen);
            CollectMonsters(gameAssembly, entries, seen);
            CollectShopUtilities(gameAssembly, entries, seen);

            entries.Sort((a, b) =>
            {
                var category = string.Compare(a.category, b.category, StringComparison.OrdinalIgnoreCase);
                return category != 0 ? category : string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase);
            });

            document.extractedAtUtc = DateTime.UtcNow.ToString("O");
            document.entries = entries.ToArray();
            document.entryCount = document.entries.Length;

            var directory = Path.Combine(Paths.ConfigPath, "SpectrumRepoMod");
            Directory.CreateDirectory(directory);
            var fileName = "catalogue-extraction-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + ".json";
            var path = Path.Combine(directory, fileName);
            File.WriteAllText(path, JsonUtility.ToJson(document, true));

            Logger.LogInfo("Catalogue export completed: " + document.entryCount + " entries.");
            Logger.LogInfo("Valuables: " + CountCategory(entries, "valuable") +
                           "; monsters: " + CountCategory(entries, "monster") +
                           "; shop utilities: " + CountCategory(entries, "shop-utility"));
            Logger.LogInfo("Export saved to: " + path);
        }
        catch (Exception exception)
        {
            Logger.LogError("Catalogue export failed: " + exception);
        }
    }

    private static Assembly? FindGameAssembly()
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (string.Equals(assembly.GetName().Name, "Assembly-CSharp", StringComparison.Ordinal))
                return assembly;
        }

        try { return Assembly.Load("Assembly-CSharp"); }
        catch { return null; }
    }

    private static void CollectValuables(Assembly gameAssembly, List<ExtractionEntry> entries, HashSet<string> seen)
    {
        var presetType = gameAssembly.GetType("LevelValuables");
        if (presetType == null)
            return;

        foreach (var preset in Resources.FindObjectsOfTypeAll(presetType))
        {
            if (preset == null)
                continue;

            var presetName = UnityName(preset);
            foreach (var group in ValuableGroups)
            {
                AddPrefabReferences(GetMember(preset, group), "valuable", "valuable:", presetName, group, entries, seen);
            }
        }

        // Include presets referenced by the active level even if Unity did not return them in the asset scan.
        var generatorType = gameAssembly.GetType("LevelGenerator");
        var generator = GetStaticMember(generatorType, "Instance");
        var level = GetMember(generator, "Level");
        AddPresetCollection(GetMember(level, "ValuablePresets"), entries, seen);
    }

    private static void AddPresetCollection(object? presets, List<ExtractionEntry> entries, HashSet<string> seen)
    {
        foreach (var preset in AsObjects(presets))
        {
            var presetName = UnityName(preset);
            foreach (var group in ValuableGroups)
                AddPrefabReferences(GetMember(preset, group), "valuable", "valuable:", presetName, group, entries, seen);
        }
    }

    private static void CollectMonsters(Assembly gameAssembly, List<ExtractionEntry> entries, HashSet<string> seen)
    {
        var setupType = gameAssembly.GetType("EnemySetup");
        if (setupType == null)
            return;

        var setups = new List<object>();
        var setupNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var setup in Resources.FindObjectsOfTypeAll(setupType))
            AddSetup(setup, setups, setupNames);

        var directorType = gameAssembly.GetType("EnemyDirector");
        var director = GetStaticMember(directorType, "instance");
        foreach (var memberName in new[] { "enemyList", "enemiesDifficulty1", "enemiesDifficulty2", "enemiesDifficulty3" })
        {
            foreach (var setup in AsObjects(GetMember(director, memberName)))
                AddSetup(setup, setups, setupNames);
        }

        foreach (var setup in setups)
        {
            var setupName = UnityName(setup);
            var prefabPaths = new List<string>();
            var prefabNames = new List<string>();

            foreach (var prefabRef in AsObjects(GetMember(setup, "spawnObjects")))
            {
                var path = ReadString(prefabRef, "ResourcePath");
                if (string.IsNullOrWhiteSpace(path))
                    continue;

                if (!prefabPaths.Contains(path, StringComparer.OrdinalIgnoreCase))
                    prefabPaths.Add(path);

                var prefabName = ReadString(prefabRef, "PrefabName");
                if (!string.IsNullOrWhiteSpace(prefabName) &&
                    !prefabNames.Contains(prefabName, StringComparer.OrdinalIgnoreCase))
                    prefabNames.Add(prefabName);
            }

            if (string.IsNullOrWhiteSpace(setupName) || prefabPaths.Count == 0)
                continue;

            var id = "monster:" + setupName;
            if (!seen.Add(id))
                continue;

            entries.Add(new ExtractionEntry
            {
                id = id,
                name = setupName,
                category = "monster",
                tags = new[] { "vanilla", "needs-review" },
                source = "EnemySetup",
                sourceAsset = setupName,
                prefabPaths = prefabPaths.ToArray(),
                prefabNames = prefabNames.ToArray()
            });
        }
    }

    private static void AddSetup(object? setup, List<object> setups, HashSet<string> names)
    {
        if (setup == null)
            return;

        var name = UnityName(setup);
        if (string.IsNullOrWhiteSpace(name) || !names.Add(name))
            return;

        setups.Add(setup);
    }

    private static void CollectShopUtilities(Assembly gameAssembly, List<ExtractionEntry> entries, HashSet<string> seen)
    {
        var statsType = gameAssembly.GetType("StatsManager");
        var stats = GetStaticMember(statsType, "instance");
        var dictionary = GetMember(stats, "itemDictionary");

        foreach (var item in DictionaryValues(dictionary))
        {
            if (item == null)
                continue;

            var itemType = GetMember(item, "itemType")?.ToString() ?? string.Empty;
            if (!IsSafeShopUtilityType(itemType))
                continue;

            var internalName = UnityName(item);
            if (string.IsNullOrWhiteSpace(internalName))
                continue;

            var displayName = ReadString(item, "itemName");
            if (string.IsNullOrWhiteSpace(displayName))
                displayName = internalName;

            var prefabRef = GetMember(item, "prefab");
            var prefabPath = ReadString(prefabRef, "ResourcePath");
            var networkPath = "Items/" + internalName;
            var id = "shop-utility:" + networkPath;
            if (!seen.Add(id))
                continue;

            entries.Add(new ExtractionEntry
            {
                id = id,
                name = displayName,
                category = "shop-utility",
                tags = new[] { "vanilla", "needs-review" },
                source = "StatsManager.itemDictionary",
                sourceAsset = internalName,
                itemType = itemType,
                prefabPaths = string.IsNullOrWhiteSpace(prefabPath)
                    ? new[] { networkPath }
                    : new[] { prefabPath, networkPath }
            });
        }
    }

    private static bool IsSafeShopUtilityType(string itemType)
    {
        foreach (var allowedType in SafeShopUtilityTypes)
        {
            if (string.Equals(itemType, allowedType, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static void AddPrefabReferences(
        object? collection,
        string category,
        string idPrefix,
        string sourceAsset,
        string group,
        List<ExtractionEntry> entries,
        HashSet<string> seen)
    {
        foreach (var prefabRef in AsObjects(collection))
        {
            var path = ReadString(prefabRef, "ResourcePath");
            if (string.IsNullOrWhiteSpace(path))
                continue;

            var id = idPrefix + path;
            if (!seen.Add(id))
                continue;

            var name = ReadString(prefabRef, "PrefabName");
            if (string.IsNullOrWhiteSpace(name))
                name = path;

            entries.Add(new ExtractionEntry
            {
                id = id,
                name = name,
                category = category,
                tags = new[] { "vanilla", "needs-review" },
                source = "LevelValuables",
                sourceAsset = sourceAsset,
                group = group,
                prefabPaths = new[] { path }
            });
        }
    }

    private static object? GetStaticMember(Type? type, string name)
    {
        if (type == null)
            return null;

        const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        try
        {
            var property = type.GetProperty(name, flags);
            if (property != null)
                return property.GetValue(null, null);

            var field = type.GetField(name, flags);
            return field?.GetValue(null);
        }
        catch
        {
            return null;
        }
    }

    private static object? GetMember(object? target, string name)
    {
        if (target == null)
            return null;

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        try
        {
            var property = target.GetType().GetProperty(name, flags);
            if (property != null)
                return property.GetValue(target, null);

            var field = target.GetType().GetField(name, flags);
            return field?.GetValue(target);
        }
        catch
        {
            return null;
        }
    }

    private static string ReadString(object? target, string member)
    {
        return GetMember(target, member) as string ?? string.Empty;
    }

    private static string UnityName(object? target)
    {
        if (target is UnityEngine.Object unityObject)
            return unityObject.name;

        return ReadString(target, "name");
    }

    private static IEnumerable<object> AsObjects(object? value)
    {
        if (value is not IEnumerable enumerable)
            yield break;

        foreach (var item in enumerable)
        {
            if (item != null)
                yield return item;
        }
    }

    private static IEnumerable<object> DictionaryValues(object? value)
    {
        if (value is IDictionary dictionary)
        {
            foreach (var item in dictionary.Values)
            {
                if (item != null)
                    yield return item;
            }

            yield break;
        }

        foreach (var pair in AsObjects(value))
        {
            var item = GetMember(pair, "Value");
            if (item != null)
                yield return item;
        }
    }

    private static int CountCategory(List<ExtractionEntry> entries, string category)
    {
        var count = 0;
        foreach (var entry in entries)
        {
            if (string.Equals(entry.category, category, StringComparison.OrdinalIgnoreCase))
                count++;
        }

        return count;
    }

    [Serializable]
    private sealed class ExtractionDocument
    {
        public int schemaVersion = 1;
        public string extractedAtUtc = string.Empty;
        public int entryCount;
        public ExtractionEntry[] entries = Array.Empty<ExtractionEntry>();
    }

    [Serializable]
    private sealed class ExtractionEntry
    {
        public string id = string.Empty;
        public string name = string.Empty;
        public string category = string.Empty;
        public string[] tags = Array.Empty<string>();
        public string source = string.Empty;
        public string sourceAsset = string.Empty;
        public string group = string.Empty;
        public string itemType = string.Empty;
        public string[] prefabPaths = Array.Empty<string>();
        public string[] prefabNames = Array.Empty<string>();
    }
}
