using System;
using System.Collections.Generic;
using System.Linq;

namespace SpectrumRepoMod.Rewards;

/// <summary>A catalogue row suitable for the Control Center manager.</summary>
public sealed class SpawnCatalogRow<T>
{
    public RewardCandidate<T> Candidate { get; }
    public bool Allowed { get; }
    public string ExclusionReason { get; }

    internal SpawnCatalogRow(RewardCandidate<T> candidate, bool allowed, string exclusionReason)
    {
        Candidate = candidate;
        Allowed = allowed;
        ExclusionReason = exclusionReason;
    }
}

/// <summary>
/// Immutable, deterministic view of the current game's registered spawn catalogue.
/// It never writes to persistent storage; callers can rebuild it when registries change.
/// </summary>
public sealed class SpawnCatalogSnapshot<T>
{
    public IReadOnlyList<SpawnCatalogRow<T>> Rows { get; }
    public IReadOnlyList<RewardCandidate<T>> AllowedCandidates { get; }
    public int TotalCount => Rows.Count;
    public int AllowedCount => AllowedCandidates.Count;
    public int ExcludedCount => TotalCount - AllowedCount;

    private SpawnCatalogSnapshot(IReadOnlyList<SpawnCatalogRow<T>> rows)
    {
        Rows = rows;
        AllowedCandidates = rows.Where(row => row.Allowed).Select(row => row.Candidate).ToArray();
    }

    public static SpawnCatalogSnapshot<T> Build(
        IEnumerable<RewardCandidate<T>> candidates,
        SpawnExclusionList? exclusions = null)
    {
        if (candidates == null) throw new ArgumentNullException(nameof(candidates));
        exclusions ??= SpawnExclusionList.Default;

        var ordered = candidates
            .Where(candidate => candidate != null)
            .OrderBy(candidate => candidate.Category)
            .ThenBy(candidate => candidate.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var rows = new List<SpawnCatalogRow<T>>(ordered.Length);
        foreach (var candidate in ordered)
        {
            var key = ((int)candidate.Category).ToString() + ":" + candidate.Id;
            if (!seen.Add(key))
                throw new InvalidOperationException("Duplicate catalogue ID in category " +
                    candidate.Category + ": " + candidate.Id);

            var allowed = SpawnSafetyGuard.TryValidate(candidate, exclusions, out var reason);
            rows.Add(new SpawnCatalogRow<T>(candidate, allowed, reason));
        }

        return new SpawnCatalogSnapshot<T>(rows);
    }

    public bool CanSpawn(RewardCandidate<T>? candidate, SpawnExclusionList? exclusions, out string reason) =>
        SpawnSafetyGuard.TryValidate(candidate, exclusions, out reason);
}
