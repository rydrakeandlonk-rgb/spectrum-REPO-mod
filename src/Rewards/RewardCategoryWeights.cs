using System;

namespace SpectrumRepoMod.Rewards;

public sealed class RewardCategoryWeights
{
    public double Monster { get; }
    public double UtilityItem { get; }
    public double Valuable { get; }

    public RewardCategoryWeights(double monster, double utilityItem, double valuable)
    {
        ValidateWeight(monster, nameof(monster));
        ValidateWeight(utilityItem, nameof(utilityItem));
        ValidateWeight(valuable, nameof(valuable));

        if (monster + utilityItem + valuable <= 0d)
            throw new ArgumentException("At least one reward category must have a positive weight.");

        Monster = monster;
        UtilityItem = utilityItem;
        Valuable = valuable;
    }

    public static RewardCategoryWeights Default { get; } = new(1d, 1d, 1d);

    public double For(RewardCategory category) => category switch
    {
        RewardCategory.Monster => Monster,
        RewardCategory.UtilityItem => UtilityItem,
        RewardCategory.Valuable => Valuable,
        _ => 0d
    };

    private static void ValidateWeight(double value, string name)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
            throw new ArgumentOutOfRangeException(name, "Category weights must be finite and non-negative.");
    }
}
