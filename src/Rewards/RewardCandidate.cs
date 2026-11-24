using System;
using System.Collections.Generic;
using System.Linq;

namespace SpectrumRepoMod.Rewards;

public sealed class RewardCandidate<T>
{
    public RewardCategory Category { get; }
    public string Id { get; }
    public string DisplayName { get; }
    public T Value { get; }
    public ShopItemClassification ShopItemClassification { get; }
    public IReadOnlyCollection<string> Tags { get; }

    public RewardCandidate(RewardCategory category, string id, string displayName, T value,
        ShopItemClassification shopItemClassification = ShopItemClassification.Unknown,
        IEnumerable<string>? tags = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("A stable candidate ID is required.", nameof(id));
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("A display name is required.", nameof(displayName));
        Category = category;
        Id = id.Trim();
        DisplayName = displayName.Trim();
        Value = value;
        ShopItemClassification = shopItemClassification;
        Tags = (tags ?? Array.Empty<string>()).Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim()).ToArray();
    }

    public bool IsEligible =>
        (Category != RewardCategory.UtilityItem || ShopItemClassification == ShopItemClassification.Utility) &&
        ShopItemClassification != ShopItemClassification.Weapon &&
        ShopItemClassification != ShopItemClassification.Excluded;
}
