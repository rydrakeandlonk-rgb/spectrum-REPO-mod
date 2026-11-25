using System;
using System.Collections.Generic;
using System.Linq;

namespace SpectrumRepoMod.Runtime;

/// <summary>Category policy for supported, non-weapon spawn types.</summary>
internal sealed class RepoRuntimePolicy
{
    public bool ValuablesEnabled { get; }
    public bool MonstersEnabled { get; }

    public RepoRuntimePolicy(bool valuablesEnabled = true, bool monstersEnabled = true)
    {
        ValuablesEnabled = valuablesEnabled;
        MonstersEnabled = monstersEnabled;
    }

    public bool TryValidate(RepoCatalogRow? candidate, out string reason)
    {
        if (candidate == null)
        {
            reason = "The requested catalogue entry is missing.";
            return false;
        }

        if (string.Equals(candidate.Category, "valuable", StringComparison.Ordinal))
        {
            if (!ValuablesEnabled)
            {
                reason = "Valuable spawning is disabled in Control Center.";
                return false;
            }
            if (candidate.Target.Valuable == null || !candidate.Target.Valuable.IsValid())
            {
                reason = "The valuable is no longer available.";
                return false;
            }
        }
        else if (string.Equals(candidate.Category, "monster", StringComparison.Ordinal))
        {
            if (!MonstersEnabled)
            {
                reason = "Monster spawning is disabled in Control Center.";
                return false;
            }
            var setup = candidate.Target.Monster;
            if (setup == null || setup.spawnObjects == null || setup.spawnObjects.Count == 0)
            {
                reason = "The monster setup has no spawnable prefab references.";
                return false;
            }
            if (setup.spawnObjects.Exists(prefab => prefab == null || !prefab.IsValid()))
            {
                reason = "The monster setup contains an invalid prefab reference.";
                return false;
            }
        }
        else
        {
            reason = "This build supports valuables and monsters only.";
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

    public static RepoCatalogSnapshot Empty { get; } = new(Array.Empty<RepoCatalogRow>());
}
