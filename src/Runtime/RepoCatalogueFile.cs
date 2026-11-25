using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using UnityEngine;

namespace SpectrumRepoMod.Runtime;

/// <summary>
/// Loads the editable per-entry catalogue from the BepInEx config directory.
/// The embedded JSON is only the first-run template; user edits are preserved.
/// </summary>
internal sealed class RepoCatalogueFile
{
    private const string ResourceName = "SpectrumRepoMod.Catalogue.vanilla-catalogue.json";
    private const string FileName = "vanilla-catalogue.json";

    private readonly RepoCatalogueDocument _document;

    private RepoCatalogueFile(RepoCatalogueDocument document)
    {
        _document = document;
    }

    public static void EnsureInstalled()
    {
        var path = GetConfigFilePath();
        if (File.Exists(path))
            return;

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        using var resource = typeof(RepoCatalogueFile).GetTypeInfo().Assembly.GetManifestResourceStream(ResourceName);
        if (resource == null)
            throw new InvalidOperationException("The embedded vanilla catalogue template is missing.");

        using var output = File.Create(path);
        resource.CopyTo(output);
    }

    public static RepoCatalogueFile Load()
    {
        EnsureInstalled();
        var json = File.ReadAllText(GetConfigFilePath());
        var document = JsonUtility.FromJson<RepoCatalogueDocument>(json);
        if (document == null || document.schemaVersion != 1 || document.entries == null)
            throw new InvalidDataException("The vanilla catalogue file is invalid or uses an unsupported schema version.");

        return new RepoCatalogueFile(document);
    }

    public bool IsAllowed(string category, string id, out string reason)
    {
        if (!_document.enforceEntries)
        {
            reason = string.Empty;
            return true;
        }

        var entry = _document.entries.FirstOrDefault(candidate =>
            candidate != null &&
            string.Equals(candidate.category, category, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(candidate.id, id, StringComparison.OrdinalIgnoreCase));

        if (entry == null)
        {
            reason = "This entry is not listed in the editable vanilla catalogue.";
            return false;
        }

        var tags = entry.tags ?? Array.Empty<string>();
        if (tags.Any(tag => string.Equals(tag, "blocked", StringComparison.OrdinalIgnoreCase)))
        {
            reason = "This entry is explicitly blocked in the editable vanilla catalogue.";
            return false;
        }

        if (!tags.Any(tag => string.Equals(tag, "allowed", StringComparison.OrdinalIgnoreCase)))
        {
            reason = "This entry does not have the allowed tag in the editable vanilla catalogue.";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    private static string GetConfigFilePath()
    {
        return Path.Combine(Paths.ConfigPath, "SpectrumRepoMod", FileName);
    }
}

[Serializable]
internal sealed class RepoCatalogueDocument
{
    public int schemaVersion = 1;
    public bool enforceEntries;
    public string description = string.Empty;
    public RepoCatalogueEntry[] entries = Array.Empty<RepoCatalogueEntry>();
}

[Serializable]
internal sealed class RepoCatalogueEntry
{
    public string id = string.Empty;
    public string name = string.Empty;
    public string category = string.Empty;
    public string[] tags = Array.Empty<string>();
}
