namespace SpectrumRepoMod.Rewards;

/// <summary>
/// Final validation shared by catalogue filtering and the host's immediate
/// pre-spawn check. Item restrictions live only in SpawnWeaponsBlocker.
/// </summary>
public static class SpawnSafetyGuard
{
    public static bool TryValidate<T>(RewardCandidate<T>? candidate, out string reason)
    {
        if (candidate == null)
        {
            reason = "Missing candidate";
            return false;
        }

        if (SpawnWeaponsBlocker.IsBlocked(candidate.Category, candidate.Id))
        {
            reason = "Item is listed in SpawnWeaponsBlocker";
            return true;
        }

        reason = string.Empty;
        return true;
    }
}
