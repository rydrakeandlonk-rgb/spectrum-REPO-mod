using System;

namespace SpectrumRepoMod.Rewards;

/// <summary>A category-scoped exclusion identified by a stable catalogue ID.</summary>
public sealed class SpawnExclusionEntry
{
    public RewardCategory Category { get; }
    public string Id { get; }
    public string Reason { get; }

    public SpawnExclusionEntry(RewardCategory category, string id, string reason)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("A stable catalogue ID is required.", nameof(id));
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("An exclusion reason is required.", nameof(reason));
        Category = category;
        Id = id.Trim();
        Reason = reason.Trim();
    }
}
