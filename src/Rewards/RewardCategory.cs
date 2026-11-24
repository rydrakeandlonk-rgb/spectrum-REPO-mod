namespace SpectrumRepoMod.Rewards;

public enum RewardCategory
{
    Monster,
    UtilityItem,
    Valuable
}

/// <summary>
/// Classification is supplied by the game-specific catalogue adapter.
/// Unknown shop entries are deliberately excluded until classified.
/// </summary>
public enum ShopItemClassification
{
    Unknown,
    Utility,
    Excluded
}
