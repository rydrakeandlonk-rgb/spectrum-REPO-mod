using System;
using System.Collections.Generic;
using System.Linq;

namespace SpectrumRepoMod.Rewards;

/// <summary>
/// Shared deny list for monsters, shop utility items, and valuables.
/// Category-scoped IDs prevent an exclusion in one catalogue affecting another.
/// </summary>
public sealed class SpawnExclusionList
{
    private readonly Dictionary<RewardCategory, Dictionary<string, string>> _entries;
    private readonly HashSet<string> _globalTags;

    public static SpawnExclusionList Default { get; } = new(
        Array.Empty<SpawnExclusionEntry>(),
        new[] { "weapon" });

    public SpawnExclusionList(
        IEnumerable<SpawnExclusionEntry>? entries,
        IEnumerable<string>? globalExcludedTags = null)
    {
        _entries = new Dictionary<RewardCategory, Dictionary<string, string>>();
        foreach (var entry in entries ?? Array.Empty<SpawnExclusionEntry>())
        {
            if (entry == null) continue;
            if (!_entries.TryGetValue(entry.Category, out var categoryEntries))
            {
                categoryEntries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                _entries.Add(entry.Category, categoryEntries);
            }
            categoryEntries[Normalize(entry.Id)] = entry.Reason;
        }

        _globalTags = new HashSet<string>(
            (globalExcludedTags ?? Array.Empty<string>())
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Select(Normalize),
            StringComparer.OrdinalIgnoreCase);
    }

    public bool IsExcluded(RewardCategory category, string id, IEnumerable<string>? tags = null) =>
        TryGetExclusionReason(category, id, tags, out _);

    public bool TryGetExclusionReason(
        RewardCategory category,
        string id,
        IEnumerable<string>? tags,
        out string reason)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            reason = "Missing stable catalogue ID";
            return true;
        }

        if (_entries.TryGetValue(category, out var categoryEntries) &&
            categoryEntries.TryGetValue(Normalize(id), out reason!))
            return true;

        var matchingTag = (tags ?? Array.Empty<string>())
            .FirstOrDefault(tag => !string.IsNullOrWhiteSpace(tag) && _globalTags.Contains(Normalize(tag)));
        if (matchingTag != null)
        {
            reason = "Excluded by global tag: " + Normalize(matchingTag);
            return true;
        }

        reason = string.Empty;
        return false;
    }

    public IReadOnlyCollection<SpawnExclusionEntry> Entries =>
        _entries.SelectMany(category => category.Value.Select(item =>
            new SpawnExclusionEntry(category.Key, item.Key, item.Value))).ToArray();

    public IReadOnlyCollection<string> GlobalExcludedTags => _globalTags.ToArray();

    private static string Normalize(string value) => value.Trim();
}
