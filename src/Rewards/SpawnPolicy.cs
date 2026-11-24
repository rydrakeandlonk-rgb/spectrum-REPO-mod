using System;
using System.Collections.Generic;
using System.Linq;

namespace SpectrumRepoMod.Rewards;

/// <summary>
/// Validated runtime view of Control Center category settings. Explicit item
/// exclusions are owned only by SpawnWeaponsBlocker in the game mod.
/// </summary>
public sealed class SpawnPolicy
{
    public bool MonstersEnabled { get; }
    public bool UtilityItemsEnabled { get; }
    public bool ValuablesEnabled { get; }
    public RewardCategoryWeights Weights { get; }

    public static SpawnPolicy Default { get; } = new(false, 1d, false, 1d, true, 1d);

    public SpawnPolicy(
        bool monstersEnabled, double monsterWeight,
        bool utilityItemsEnabled, double utilityItemWeight,
        bool valuablesEnabled, double valuableWeight)
    {
        MonstersEnabled = monstersEnabled;
        UtilityItemsEnabled = utilityItemsEnabled;
        ValuablesEnabled = valuablesEnabled;

        ValidateWeight(monsterWeight, nameof(monsterWeight));
        ValidateWeight(utilityItemWeight, nameof(utilityItemWeight));
        ValidateWeight(valuableWeight, nameof(valuableWeight));

        var effectiveMonster = monstersEnabled ? monsterWeight : 0d;
        var effectiveUtility = utilityItemsEnabled ? utilityItemWeight : 0d;
        var effectiveValuable = valuablesEnabled ? valuableWeight : 0d;
        Weights = new RewardCategoryWeights(effectiveMonster, effectiveUtility, effectiveValuable);
    }

    public bool IsCategoryEnabled(RewardCategory category) => category switch
    {
        RewardCategory.Monster => MonstersEnabled,
        RewardCategory.UtilityItem => UtilityItemsEnabled,
        RewardCategory.Valuable => ValuablesEnabled,
        _ => false
    };

    public bool TryValidateSpawn<T>(RewardCandidate<T>? candidate, out string reason)
    {
        if (candidate == null)
        {
            reason = "Missing candidate";
            return false;
        }
        if (!IsCategoryEnabled(candidate.Category))
        {
            reason = "This reward category is disabled in Control Center";
            return false;
        }
        return SpawnSafetyGuard.TryValidate(candidate, out reason);
    }

    public IReadOnlyList<RewardCandidate<T>> GetEligibleCandidates<T>(
        IEnumerable<RewardCandidate<T>> candidates) =>
        candidates.Where(candidate => TryValidateSpawn(candidate, out _)).ToArray();

    private static void ValidateWeight(double value, string name)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d || value > 1000d)
            throw new ArgumentOutOfRangeException(name, "Weights must be finite and between 0 and 1000.");
    }
}
