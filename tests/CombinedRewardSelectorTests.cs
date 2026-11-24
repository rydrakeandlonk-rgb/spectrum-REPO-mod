using System;
using System.Collections.Generic;
using System.Linq;
using SpectrumRepoMod.Rewards;
using Xunit;

namespace SpectrumRepoMod.Tests;

public sealed class CombinedRewardSelectorTests
{
    [Fact]
    public void InitialVortexTestPolicyAllowsOnlyValuables()
    {
        var policy = SpawnPolicy.Default;
        Assert.False(policy.IsCategoryEnabled(RewardCategory.Monster));
        Assert.False(policy.IsCategoryEnabled(RewardCategory.UtilityItem));
        Assert.True(policy.IsCategoryEnabled(RewardCategory.Valuable));
        Assert.Equal(0d, policy.Weights.Monster);
        Assert.Equal(0d, policy.Weights.UtilityItem);
        Assert.Equal(1d, policy.Weights.Valuable);
    }

    [Fact]
    public void UnknownShopItemsAreEligibleDuringUnrestrictedCatalogueTest()
    {
        var candidate = Candidate(RewardCategory.UtilityItem, "item:ordinary-tool");
        Assert.True(candidate.IsEligible);
        Assert.True(SpawnSafetyGuard.TryValidate(candidate, out _));
    }

    [Fact]
    public void WeaponClassificationDoesNotRestrictInitialCatalogue()
    {
        var candidate = Candidate(RewardCategory.UtilityItem, "item:weapon-prefab",
            ShopItemClassification.Weapon);
        Assert.True(SpawnSafetyGuard.TryValidate(candidate, out _));
    }

    [Fact]
    public void EmptyWeaponsBlockerAllowsEveryUnlistedItem()
    {
        Assert.False(SpawnWeaponsBlocker.IsBlocked(RewardCategory.UtilityItem, "item:ordinary-item"));
        Assert.False(SpawnWeaponsBlocker.IsBlocked(RewardCategory.UtilityItem, "item:weapon-item"));
        Assert.False(SpawnWeaponsBlocker.IsBlocked(RewardCategory.Monster, "enemy:sample"));
    }

    [Fact]
    public void SpawnPolicyAppliesCategoryTogglesAndWeights()
    {
        var policy = new SpawnPolicy(true, 3d, false, 8d, true, 2d);
        Assert.True(policy.IsCategoryEnabled(RewardCategory.Monster));
        Assert.False(policy.IsCategoryEnabled(RewardCategory.UtilityItem));
        Assert.True(policy.IsCategoryEnabled(RewardCategory.Valuable));
        Assert.Equal(3d, policy.Weights.Monster);
        Assert.Equal(0d, policy.Weights.UtilityItem);
        Assert.Equal(2d, policy.Weights.Valuable);
    }

    [Fact]
    public void SpawnPolicyBlocksDisabledCategoryAtFinalValidation()
    {
        var policy = new SpawnPolicy(false, 1d, true, 1d, true, 1d);
        Assert.False(policy.TryValidateSpawn(Candidate(RewardCategory.Monster, "enemy:a"), out var reason));
        Assert.Equal("This reward category is disabled in Control Center", reason);
    }

    [Fact]
    public void SpawnPolicyFiltersOnlyDisabledCategories()
    {
        var policy = new SpawnPolicy(true, 1d, true, 1d, true, 1d);
        var candidates = new[]
        {
            Candidate(RewardCategory.Monster, "enemy:a"),
            Candidate(RewardCategory.UtilityItem, "item:unknown", ShopItemClassification.Unknown),
            Candidate(RewardCategory.UtilityItem, "item:weapon", ShopItemClassification.Weapon),
            Candidate(RewardCategory.Valuable, "valuable:a")
        };
        Assert.Equal(4, policy.GetEligibleCandidates(candidates).Count);
    }

    [Fact]
    public void CombinedSelectorCanConsumeValidatedSpawnPolicy()
    {
        var policy = new SpawnPolicy(true, 1d, true, 1d, true, 1d);
        var candidates = new[]
        {
            Candidate(RewardCategory.Monster, "enemy:a"),
            Candidate(RewardCategory.UtilityItem, "item:ordinary", ShopItemClassification.Unknown),
            Candidate(RewardCategory.Valuable, "valuable:a")
        };
        var selected = CombinedRewardSelector.Select(candidates, policy, new FixedRandom(0.4, 0));
        Assert.Equal(RewardCategory.UtilityItem, selected.Category);
    }

    [Fact]
    public void CatalogueSnapshotIncludesUnclassifiedAndWeaponClassifiedItems()
    {
        var snapshot = SpawnCatalogSnapshot<string>.Build(new[]
        {
            Candidate(RewardCategory.UtilityItem, "item:unknown", ShopItemClassification.Unknown),
            Candidate(RewardCategory.UtilityItem, "item:weapon", ShopItemClassification.Weapon),
            Candidate(RewardCategory.UtilityItem, "item:valuable", ShopItemClassification.Excluded)
        });
        Assert.Equal(3, snapshot.TotalCount);
        Assert.Equal(3, snapshot.AllowedCount);
        Assert.Equal(0, snapshot.ExcludedCount);
    }

    [Fact]
    public void CatalogueSnapshotReflectsCategoryPolicy()
    {
        var policy = new SpawnPolicy(true, 1d, false, 1d, true, 1d);
        var snapshot = SpawnCatalogSnapshot<string>.Build(new[]
        {
            Candidate(RewardCategory.Monster, "enemy:a"),
            Candidate(RewardCategory.UtilityItem, "item:a"),
            Candidate(RewardCategory.Valuable, "valuable:a")
        }, policy);
        Assert.Equal(2, snapshot.AllowedCount);
        Assert.Equal("This reward category is disabled in Control Center",
            snapshot.Rows.Single(row => row.Candidate.Category == RewardCategory.UtilityItem).ExclusionReason);
    }

    [Fact]
    public void CatalogueSnapshotSortsRowsDeterministically()
    {
        var snapshot = SpawnCatalogSnapshot<string>.Build(new[]
        {
            Candidate(RewardCategory.Valuable, "valuable:z"),
            Candidate(RewardCategory.Monster, "enemy:b"),
            Candidate(RewardCategory.Monster, "enemy:a")
        });
        Assert.Equal(new[] { "enemy:a", "enemy:b", "valuable:z" },
            snapshot.Rows.Select(row => row.Candidate.Id));
    }

    [Fact]
    public void CatalogueSnapshotRejectsDuplicateIdsWithinCategory()
    {
        Assert.Throws<InvalidOperationException>(() => SpawnCatalogSnapshot<string>.Build(new[]
        {
            Candidate(RewardCategory.Monster, "enemy:duplicate"),
            Candidate(RewardCategory.Monster, "ENEMY:DUPLICATE")
        }));
    }

    [Fact]
    public void UnavailableCategoriesAreRemovedBeforeWeightedSelection()
    {
        var selected = CombinedRewardSelector.Select(
            new[] { Candidate(RewardCategory.Valuable, "valuable:a") },
            RewardCategoryWeights.Default, new FixedRandom(0.99, 0));
        Assert.Equal(RewardCategory.Valuable, selected.Category);
    }

    [Fact]
    public void ZeroWeightCategoryIsNotSelected()
    {
        var selected = CombinedRewardSelector.Select(new[]
        {
            Candidate(RewardCategory.Monster, "enemy:a"),
            Candidate(RewardCategory.Valuable, "valuable:a")
        }, new RewardCategoryWeights(0d, 1d, 4d), new FixedRandom(0.99, 0));
        Assert.Equal(RewardCategory.Valuable, selected.Category);
    }

    [Fact]
    public void RejectsWeightsWhoseCombinedTotalOverflows()
    {
        Assert.Throws<ArgumentException>(() => new RewardCategoryWeights(double.MaxValue, double.MaxValue, 1d));
    }

    [Fact]
    public void FailsClearlyWhenNothingIsEligible()
    {
        Assert.Throws<InvalidOperationException>(() =>
            CombinedRewardSelector.Select(Array.Empty<RewardCandidate<string>>(),
                SpawnPolicy.Default, new Random(1)));
    }

    private static RewardCandidate<string> Candidate(
        RewardCategory category, string id,
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
        public override int Next(int maxValue) => Math.Clamp(_integers.Dequeue(), 0, maxValue - 1);
    }
}
