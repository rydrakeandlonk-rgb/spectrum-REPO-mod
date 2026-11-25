using System.Collections.Generic;
using System.Linq;

namespace SpectrumRepoMod.Runtime;

/// <summary>
/// Narrow first-test policy. This version intentionally recognises only
/// valuable collectibles; every other category is rejected by default.
/// </summary>
internal sealed class RepoRuntimePolicy
{
    public bool ValuablesEnabled { get; }

    public RepoRuntimePolicy(bool valuablesEnabled = true)
    {
        ValuablesEnabled = valuablesEnabled;
    }

    public bool TryValidate(RepoCatalogRow? candidate, out string reason)
    {
        if (candidate == null)
        {
            reason = "The requested catalogue entry is missing.";
            return false;
        }

        if (!string.Equals(candidate.Category, "valuable", System.StringComparison.Ordinal))
        {
            reason = "This test build supports valuable collectibles only.";
            return false;
        }

        if (!ValuablesEnabled)
        {
            reason = "Valuable spawning is disabled in Control Center.";
            return false;
        }

        if (candidate.Target.Valuable == null || !candidate.Target.Valuable.IsValid())
        {
            reason = "The valuable is no longer available in the current level.";
            return false;
        }

        reason = string.Empty;
        return true;
    }
}

internal sealed class RepoCatalogRow
{
    public string Category { get; }
    public string Id { get; }
    public string DisplayName { get; }
    public RepoSpawnTarget Target { get; }
    public bool Allowed { get; }
    public string ExclusionReason { get; }

    public RepoCatalogRow(string category, string id, string displayName, RepoSpawnTarget target,
        bool allowed, string exclusionReason)
    {
        Category = category;
        Id = id;
        DisplayName = displayName;
        Target = target;
        Allowed = allowed;
        ExclusionReason = exclusionReason;
    }
}

internal sealed class RepoCatalogSnapshot
{
    public IReadOnlyList<RepoCatalogRow> Rows { get; }
    public IReadOnlyList<RepoCatalogRow> AllowedCandidates { get; }

    public RepoCatalogSnapshot(IEnumerable<RepoCatalogRow> rows)
    {
        Rows = rows.ToArray();
        AllowedCandidates = Rows.Where(row => row.Allowed).ToArray();
    }

    public static RepoCatalogSnapshot Empty { get; } = new(System.Array.Empty<RepoCatalogRow>());
}
