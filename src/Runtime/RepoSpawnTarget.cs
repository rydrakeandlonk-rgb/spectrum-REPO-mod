namespace SpectrumRepoMod.Runtime;

/// <summary>Typed target holding a verified vanilla spawn definition.</summary>
public sealed class RepoSpawnTarget
{
    public PrefabRef? Valuable { get; }
    public EnemySetup? Monster { get; }
    public Item? UtilityItem { get; }
    public string UtilityItemKind { get; }

    private RepoSpawnTarget(PrefabRef? valuable, EnemySetup? monster, Item? utilityItem,
        string utilityItemKind)
    {
        Valuable = valuable;
        Monster = monster;
        UtilityItem = utilityItem;
        UtilityItemKind = utilityItemKind ?? "";
    }

    public static RepoSpawnTarget ForValuable(PrefabRef value) => new(value, null, null, "");
    public static RepoSpawnTarget ForMonster(EnemySetup value) => new(null, value, null, "");
    public static RepoSpawnTarget ForUtilityItem(Item value, string kind) =>
        new(null, null, value, kind);
}
