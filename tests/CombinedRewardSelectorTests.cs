using System;
using System.Collections.Generic;
using SpectrumRepoMod.Rewards;
using Xunit;

namespace SpectrumRepoMod.Tests;

public sealed class CombinedRewardSelectorTests
{
    [Fact]
    public void SelectsOneCategoryAndOneCandidate()
    {
        var candidates = new[]
        {
            Candidate(RewardCategory.Monster, "monster-a"),
            Candidate(RewardCategory.UtilityItem, "utility-a", ShopItemClassification.Utility),
            Candidate(RewardCategory.Valuable, "valuable-a")
        };

        var selected = CombinedRewardSelector.Select(candidates, RewardCategoryWeights.Default, new FixedRandom(0.1, 0));

        Assert.Equal(RewardCategory.Monster, selected.Category);
        Assert.Equal("monster-a", selected.Id);
    }

    [Fact]
    public void UnknownAndExcludedShopEntriesNeverEnterUtilityPool()
    {
        var candidates = new[]
        {
            Candidate(RewardCategory.UtilityItem, "unknown-item"),
            Candidate(RewardCategory.UtilityItem, "excluded-item", ShopItemClassification.Excluded),
            Candidate(RewardCategory.UtilityItem, "approved-utility", ShopItemClassification.Utility)
        };

        var selected = CombinedRewardSelector.Select(
            candidates,
            RewardCategoryWeights.Default,
            new FixedRandom(0.2, 0));

        Assert.Equal("approved-utility", selected.Id);
    }

    [Fact]
    public void UnavailableCategoriesAreRemovedBeforeWeightedSelection()
    {
        var candidates = new[]
        {
            Candidate(RewardCategory.Valuable, "valuable-a")
        };

        var selected = CombinedRewardSelector.Select(
            candidates,
            RewardCategoryWeights.Default,
            new FixedRandom(0.99, 0));

        Assert.Equal(RewardCategory.Valuable, selected.Category);
    }

    [Fact]
    public void ZeroWeightCategoryIsNotSelected()
    {
        var candidates = new[]
        {
            Candidate(RewardCategory.Monster, "monster-a"),
            Candidate(RewardCategory.Valuable, "valuable-a")
        };

        var selected = CombinedRewardSelector.Select(
            candidates,
            new RewardCategoryWeights(0d, 1d, 4d),
            new FixedRandom(0.99, 0));

        Assert.Equal(RewardCategory.Valuable, selected.Category);
    }

    [Fact]
    public void FailsClearlyWhenNothingIsEligible()
    {
        var candidates = new[]
        {
            Candidate(RewardCategory.UtilityItem, "unclassified")
        };

        Assert.Throws<InvalidOperationException>(() =>
            CombinedRewardSelector.Select(candidates, RewardCategoryWeights.Default, new Random(1)));
    }

    private static RewardCandidate<string> Candidate(
        RewardCategory category,
        string id,
        ShopItemClassification classification = ShopItemClassification.Unknown) =>
        new(category, id, id, id, classification);

    private sealed class FixedRandom : Random
    {
        private readonly Queue<double> _doubles;
        private readonly Queue<int> _integers;

        public FixedRandom(double categoryRoll, int candidateIndex)
        {
            _doubles = new Queue<double>(new[] { categoryRoll });
            _integers = new Queue<int>(new[] { candidateIndex });
        }

        public override double NextDouble() => _doubles.Dequeue();

        public override int Next(int maxValue)
        {
            var value = _integers.Dequeue();
            return Math.Clamp(value, 0, maxValue - 1);
        }
    }
}
