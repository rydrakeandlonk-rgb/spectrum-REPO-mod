using System;
using System.Collections.Generic;
using System.Linq;

namespace SpectrumRepoMod.Rewards;

public static class CombinedRewardSelector
{
    public static RewardCandidate<T> Select<T>(
        IEnumerable<RewardCandidate<T>> source,
        SpawnPolicy policy,
        Random random)
    {
        if (policy == null) throw new ArgumentNullException(nameof(policy));
        return Select(source, policy.Weights, random);
    }

    public static RewardCandidate<T> Select<T>(
        IEnumerable<RewardCandidate<T>> source,
        RewardCategoryWeights weights,
        Random random)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (weights == null) throw new ArgumentNullException(nameof(weights));
        if (random == null) throw new ArgumentNullException(nameof(random));

        var eligible = source
            .Where(candidate => SpawnSafetyGuard.TryValidate(candidate, out _))
            .GroupBy(candidate => candidate.Category)
            .ToDictionary(group => group.Key, group => group.ToArray());

        var available = Enum.GetValues(typeof(RewardCategory))
            .Cast<RewardCategory>()
            .Select(category => new
            {
                Category = category,
                Weight = weights.For(category),
                Candidates = eligible.TryGetValue(category, out var entries)
                    ? entries
                    : Array.Empty<RewardCandidate<T>>()
            })
            .Where(entry => entry.Weight > 0d && entry.Candidates.Length > 0)
            .ToArray();

        if (available.Length == 0)
            throw new InvalidOperationException("No eligible reward candidates are currently available.");

        var totalWeight = available.Sum(entry => entry.Weight);
        var roll = random.NextDouble() * totalWeight;
        var selectedCategory = available[available.Length - 1];
        foreach (var entry in available)
        {
            if (roll < entry.Weight)
            {
                selectedCategory = entry;
                break;
            }
            roll -= entry.Weight;
        }

        return selectedCategory.Candidates[random.Next(selectedCategory.Candidates.Length)];
    }
}
