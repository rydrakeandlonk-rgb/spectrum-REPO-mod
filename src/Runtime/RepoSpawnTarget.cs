using REPOLib.Objects;

namespace SpectrumRepoMod.Runtime;

/// <summary>Typed target resolved from the current REPOLib registries.</summary>
public sealed class RepoSpawnTarget
{
    public EnemySetup? Enemy { get; }
    public Item? ShopItem { get; }
    public PrefabRef? Valuable { get; }

    private RepoSpawnTarget(EnemySetup? enemy, Item? shopItem, PrefabRef? valuable)
    {
        Enemy = enemy;
        ShopItem = shopItem;
        Valuable = valuable;
    }

    public static RepoSpawnTarget ForEnemy(EnemySetup value) => new(value, null, null);
    public static RepoSpawnTarget ForShopItem(Item value) => new(null, value, null);
    public static RepoSpawnTarget ForValuable(PrefabRef value) => new(null, null, value);
}
