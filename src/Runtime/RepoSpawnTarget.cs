namespace SpectrumRepoMod.Runtime;

/// <summary>Typed target holding a verified vanilla spawn definition.</summary>
public sealed class RepoSpawnTarget
{
    public PrefabRef? Valuable { get; }
    public EnemySetup? Monster { get; }

    private RepoSpawnTarget(PrefabRef? valuable, EnemySetup? monster)
    {
        Valuable = valuable;
        Monster = monster;
    }

    public static RepoSpawnTarget ForValuable(PrefabRef value) => new(value, null);
    public static RepoSpawnTarget ForMonster(EnemySetup value) => new(null, value);
}
