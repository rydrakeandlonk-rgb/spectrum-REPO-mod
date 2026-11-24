using System;
using System.Collections.Generic;
using System.Linq;

namespace SpectrumRepoMod.Rewards;

public sealed class SpawnExclusionList
{
    private readonly HashSet<string> _itemIds;
    private readonly HashSet<string> _tags;

    public static SpawnExclusionList Default { get; } = new(
        excludedItemIds: Array.Empty<string>(),
        excludedTags: new[] { "weapon" });

    public SpawnExclusionList(IEnumerable<string>? excludedItemIds, IEnumerable<string>? excludedTags)
    {
        _itemIds = Normalize(excludedItemIds);
        _tags = Normalize(excludedTags);
    }

    public bool IsExcluded(string itemId, IEnumerable<string>? itemTags = null)
    {
        if (string.IsNullOrWhiteSpace(itemId)) return true;
        if (_itemIds.Contains(Normalize(itemId))) return true;
        return itemTags != null && itemTags.Any(tag =>
            !string.IsNullOrWhiteSpace(tag) && _tags.Contains(Normalize(tag)));
    }

    public IReadOnlyCollection<string> ItemIds => _itemIds.ToArray();
    public IReadOnlyCollection<string> Tags => _tags.ToArray();

    private static HashSet<string> Normalize(IEnumerable<string>? values) =>
        new((values ?? Array.Empty<string>()).Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(Normalize), StringComparer.OrdinalIgnoreCase);

    private static string Normalize(string value) => value.Trim();
}
