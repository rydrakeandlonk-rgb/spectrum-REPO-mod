using System;
using System.Collections.Generic;
using System.Linq;

namespace SpectrumRepoMod.Rewards;

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
        exclusions ??= SpawnExclusionList.Default;
        return BuildInternal(candidates, candidate =>
        {
            var allowed = SpawnSafetyGuard.TryValidate(candidate, exclusions, out var reason);
            return (allowed, reason);
        });
    }

    public static SpawnCatalogSnapshot<T> Build(
        IEnumerable<RewardCandidate<T>> candidates,
        SpawnPolicy policy)
    {
        if (policy == null) throw new ArgumentNullException(nameof(policy));
        return BuildInternal(candidates, candidate =>
        {
            var allowed = policy.TryValidateSpawn(candidate, out var reason);
            return (allowed, reason);
        });
    }

    public bool CanSpawn(RewardCandidate<T>? candidate, SpawnExclusionList? exclusions, out string reason) =>
        SpawnSafetyGuard.TryValidate(candidate, exclusions, out reason);

    public bool CanSpawn(RewardCandidate<T>? candidate, SpawnPolicy policy, out string reason)
    {
        if (policy == null)
        {
            reason = "Missing spawn policy";
            return false;
        }
        return policy.TryValidateSpawn(candidate, out reason);
    }

    private static SpawnCatalogSnapshot<T> BuildInternal(
        IEnumerable<RewardCandidate<T>> candidates,
        Func<RewardCandidate<T>, (bool Allowed, string Reason)> validate)
    {
        if (candidates == null) throw new ArgumentNullException(nameof(candidates));
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

            var validation = validate(candidate);
            rows.Add(new SpawnCatalogRow<T>(candidate, validation.Allowed, validation.Reason));
        }

        return new SpawnCatalogSnapshot<T>(rows);
    }
}
