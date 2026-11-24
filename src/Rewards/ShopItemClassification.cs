namespace SpectrumRepoMod.Rewards;

/// <summary>
/// Classification supplied by the game-specific catalogue adapter.
/// Unknown shop entries are deliberately excluded until classified.
/// </summary>
public enum ShopItemClassification
{
    Unknown = 0,
    Utility = 1,
    Valuable = 2,
    Weapon = 3,
    Excluded = 4
}
