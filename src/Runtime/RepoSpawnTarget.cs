namespace SpectrumRepoMod.Runtime;

/// <summary>Typed target holding a vanilla valuable prefab reference.</summary>
public sealed class RepoSpawnTarget
{
    public PrefabRef? Valuable { get; }

    private RepoSpawnTarget(PrefabRef? valuable)
    {
        Valuable = valuable;
    }

    public static RepoSpawnTarget ForValuable(PrefabRef value) => new(value);
}
