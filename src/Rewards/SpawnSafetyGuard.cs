namespace SpectrumRepoMod.Rewards;

/// <summary>
/// Final fail-closed validation shared by catalogue filtering and the host's
/// immediate pre-spawn check.
/// </summary>
public static class SpawnSafetyGuard
{
    public static bool TryValidate<T>(
        RewardCandidate<T>? candidate,
        SpawnExclusionList? exclusions,
        out string reason)
    {
        exclusions ??= SpawnExclusionList.Default;
        if (candidate == null)
        {
            reason = "Missing candidate";
            return false;
        }

        if (candidate.Category == RewardCategory.UtilityItem)
        {
            if (candidate.ShopItemClassification == ShopItemClassification.Unknown)
            {
                reason = "Shop item has not been classified";
                return false;
            }
            if (candidate.ShopItemClassification == ShopItemClassification.Weapon)
            {
                reason = "Shop item is blocked by the default weapon policy";
                return false;
            }
            if (candidate.ShopItemClassification != ShopItemClassification.Utility)
            {
                reason = "Shop item is not classified as an eligible utility";
                return false;
            }
        }
        else if (candidate.ShopItemClassification == ShopItemClassification.Weapon ||
                 candidate.ShopItemClassification == ShopItemClassification.Excluded)
        {
            reason = "Candidate is blocked by its catalogue classification";
            return false;
        }

        if (exclusions.TryGetExclusionReason(candidate.Category, candidate.Id, candidate.Tags, out reason))
            return false;

        reason = string.Empty;
        return true;
    }
}
