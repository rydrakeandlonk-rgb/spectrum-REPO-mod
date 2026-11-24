using System.Collections;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using SpectrumRepoMod.Rewards;
using SpectrumRepoMod.Runtime;
using UnityEngine;

namespace SpectrumRepoMod;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInDependency(REPOLib.MyPluginInfo.PLUGIN_GUID, BepInDependency.DependencyFlags.HardDependency)]
public sealed class Plugin : BaseUnityPlugin
{
    public const string PluginGuid = "com.spectrum.repomod";
    public const string PluginName = "Spectrum R.E.P.O. Mod";
    public const string PluginVersion = "0.1.0";

    internal static ManualLogSource Log { get; private set; } = null!;
    internal static SpawnCatalogSnapshot<RepoSpawnTarget>? Catalog { get; set; }
    internal static SpawnPolicy SpawnPolicy { get; set; } = SpawnPolicy.Default;
    internal static RepoSpawnExecutor SpawnExecutor { get; } = new();
    private RepoGatewayClient? _gateway;

    private void Awake()
    {
        Log = Logger;
        Log.LogInfo($"{PluginName} {PluginVersion} loaded.");
        Log.LogInfo($"REPOLib {REPOLib.MyPluginInfo.PLUGIN_VERSION} dependency resolved.");
        _gateway = new RepoGatewayClient(Config);
        _gateway.SpawnRequested += HandleSpawnRequest;
        _gateway.Start();
        StartCoroutine(WaitForGameCatalogue());
    }

    private IEnumerator WaitForGameCatalogue()
    {
        yield return new WaitUntil(() =>
            RunManager.instance != null &&
            EnemyDirector.instance != null &&
            StatsManager.instance != null);

        try
        {
            Catalog = RepoCatalogBuilder.Build(SpawnPolicy);
            Log.LogInfo(
                $"R.E.P.O. catalogue ready: {Catalog.TotalCount} entries, " +
                $"{Catalog.AllowedCount} eligible, {Catalog.ExcludedCount} excluded.");
            StartCoroutine(PublishGatewayState());
        }
        catch (System.Exception error)
        {
            Log.LogError($"Could not build the R.E.P.O. catalogue: {error.Message}");
        }
    }


    private void HandleSpawnRequest(string categoryName, string requestId)
    {
        var category = categoryName switch
        {
            "monster" => RewardCategory.Monster,
            "utility_item" => RewardCategory.UtilityItem,
            "valuable" => RewardCategory.Valuable,
            _ => (RewardCategory?)null
        };
        if (!category.HasValue || Catalog == null || _gateway == null)
        {
            _gateway?.CompleteSpawn(requestId, categoryName, false, "", "Unsupported category or live catalogue unavailable.");
            return;
        }

        if (!SemiFunc.IsMasterClientOrSingleplayer() || RunManager.instance == null || LevelGenerator.Instance == null)
        {
            _gateway.CompleteSpawn(requestId, categoryName, false, "", "This instance is not the active R.E.P.O. host in a running session.");
            return;
        }

        var candidates = Catalog.AllowedCandidates
            .Where(candidate => candidate.Category == category.Value &&
                SpawnPolicy.TryValidateSpawn(candidate, out _))
            .ToArray();
        if (candidates.Length == 0)
        {
            _gateway.CompleteSpawn(requestId, categoryName, false, "", "No eligible entries are available in this category.");
            return;
        }

        PlayerAvatar? player = null;
        foreach (var candidatePlayer in SemiFunc.PlayerGetAll())
        {
            if (candidatePlayer != null)
            {
                player = candidatePlayer;
                break;
            }
        }
        if (player == null)
        {
            _gateway.CompleteSpawn(requestId, categoryName, false, "", "No player position is available for this test.");
            return;
        }

        var selected = candidates[UnityEngine.Random.Range(0, candidates.Length)];
        var transform = player.transform;
        var position = transform.position + transform.forward * 3f + Vector3.up * 0.75f;
        var rotation = Quaternion.LookRotation(transform.forward, Vector3.up);
        try
        {
            var success = SpawnExecutor.TrySpawn(selected, SpawnPolicy, position, rotation, out var detail);
            _gateway.CompleteSpawn(requestId, categoryName, success, success ? selected.DisplayName : "", detail);
        }
        catch (System.Exception error)
        {
            Log.LogError("R.E.P.O. test spawn failed: " + error);
            _gateway.CompleteSpawn(requestId, categoryName, false, "", "The game rejected the spawn request. Check the mod log.");
        }
    }

    private IEnumerator PublishGatewayState()
    {
        var nextPing = 0f;
        while (true)
        {
            var runActive = RunManager.instance != null && LevelGenerator.Instance != null;
            var isMaster = runActive && SemiFunc.IsMasterClientOrSingleplayer();
            _gateway?.PumpMainThread();
            _gateway?.SendSessionState(runActive, isMaster);
            if (Time.unscaledTime >= nextPing)
            {
                _gateway?.SendPing();
                nextPing = Time.unscaledTime + 15f;
            }
            yield return new WaitForSeconds(5f);
        }
    }

    private void OnDestroy()
    {
        _gateway?.Stop();
    }
}
