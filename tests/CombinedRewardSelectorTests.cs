using System;
using System.Collections.Generic;
using System.Linq;
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
    public void WeaponTaggedItemsAreExcludedFromShopRewardPool()
    {
        var candidates = new[]
        {
            new RewardCandidate<string>(RewardCategory.UtilityItem, "shop.weapon.sample", "Sample weapon",
                "weapon", ShopItemClassification.Utility, new[] { "weapon" }),
            Candidate(RewardCategory.UtilityItem, "approved-utility", ShopItemClassification.Utility)
        };
        var selected = CombinedRewardSelector.Select(candidates, RewardCategoryWeights.Default,
            new FixedRandom(0.2, 0));
        Assert.Equal("approved-utility", selected.Id);
    }

    [Fact]
    public void ExplicitItemIdExclusionIsCaseInsensitive()
    {
        var exclusions = new SpawnExclusionList(
            new[] { new SpawnExclusionEntry(RewardCategory.UtilityItem, "Shop.SpecialItem", "Balance") },
            new[] { "weapon" });
        Assert.True(exclusions.IsExcluded(RewardCategory.UtilityItem, "shop.specialitem"));
        Assert.True(exclusions.IsExcluded(RewardCategory.Monster, "another.id", new[] { "WEAPON" }));
        Assert.False(exclusions.IsExcluded(RewardCategory.Valuable, "ordinary.valuable", new[] { "utility" }));
        Assert.False(exclusions.IsExcluded(RewardCategory.Valuable, "Shop.SpecialItem"));
    }

    [Theory]
    [InlineData(RewardCategory.Monster, "monster.blocked")]
    [InlineData(RewardCategory.UtilityItem, "shop.blocked")]
    [InlineData(RewardCategory.Valuable, "valuable.blocked")]
    public void CategorySpecificExclusionsRemoveOnlyMatchingSpawnEntry(RewardCategory category, string id)
    {
        var exclusions = new SpawnExclusionList(new[]
        {
            new SpawnExclusionEntry(category, id, "Balance or stability")
        });

        Assert.True(exclusions.IsExcluded(category, id));
        var otherCategory = category == RewardCategory.Monster
            ? RewardCategory.Valuable
            : RewardCategory.Monster;
        Assert.False(exclusions.IsExcluded(otherCategory, id));
    }

    [Fact]
    public void CustomExclusionListIsAppliedDuringSelection()
    {
        var candidates = new[]
        {
            Candidate(RewardCategory.Monster, "monster.blocked"),
            Candidate(RewardCategory.Monster, "monster.allowed")
        };
        var exclusions = new SpawnExclusionList(new[]
        {
            new SpawnExclusionEntry(RewardCategory.Monster, "monster.blocked", "Too disruptive")
        });

        var selected = CombinedRewardSelector.Select(candidates, RewardCategoryWeights.Default,
            new FixedRandom(0.1, 0), exclusions);

        Assert.Equal("monster.allowed", selected.Id);
    }

    [Fact]
    public void CatalogueSnapshotListsAllowedAndExcludedRows()
    {
        var candidates = new[]
        {
            Candidate(RewardCategory.Monster, "monster-z"),
            Candidate(RewardCategory.UtilityItem, "utility-unknown"),
            Candidate(RewardCategory.UtilityItem, "utility-safe", ShopItemClassification.Utility),
            Candidate(RewardCategory.Valuable, "valuable-a")
        };
        var exclusions = new SpawnExclusionList(new[]
        {
            new SpawnExclusionEntry(RewardCategory.Monster, "monster-z", "Balance")
        });

        var snapshot = SpawnCatalogSnapshot<string>.Build(candidates, exclusions);

        Assert.Equal(4, snapshot.TotalCount);
        Assert.Equal(2, snapshot.AllowedCount);
        Assert.Equal(2, snapshot.ExcludedCount);
        Assert.Equal("Balance", snapshot.Rows.Single(row => row.Candidate.Id == "monster-z").ExclusionReason);
        Assert.Equal("Shop item has not been classified",
            snapshot.Rows.Single(row => row.Candidate.Id == "utility-unknown").ExclusionReason);
    }

    [Fact]
    public void CatalogueSnapshotSortsRowsDeterministically()
    {
        var snapshot = SpawnCatalogSnapshot<string>.Build(new[]
        {
            Candidate(RewardCategory.Valuable, "z-value"),
            Candidate(RewardCategory.Monster, "b-monster"),
            Candidate(RewardCategory.Monster, "a-monster")
        });

        Assert.Equal(new[] { "a-monster", "b-monster", "z-value" },
            snapshot.Rows.Select(row => row.Candidate.Id));
    }

    [Fact]
    public void CatalogueSnapshotRejectsDuplicateIdsWithinCategory()
    {
        Assert.Throws<InvalidOperationException>(() => SpawnCatalogSnapshot<string>.Build(new[]
        {
            Candidate(RewardCategory.Monster, "duplicate"),
            Candidate(RewardCategory.Monster, "DUPLICATE")
        }));
    }

    [Fact]
    public void FinalSpawnGuardRechecksCategorySpecificExclusion()
    {
        var candidate = Candidate(RewardCategory.Valuable, "valuable.blocked");
        var exclusions = new SpawnExclusionList(new[]
        {
            new SpawnExclusionEntry(RewardCategory.Valuable, "valuable.blocked", "Balance")
        });

        Assert.False(SpawnSafetyGuard.TryValidate(candidate, exclusions, out var reason));
        Assert.Equal("Balance", reason);
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
    public void RejectsWeightsWhoseCombinedTotalOverflows()
    {
        Assert.Throws<ArgumentException>(() =>
            new RewardCategoryWeights(double.MaxValue, double.MaxValue, 1d));
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
