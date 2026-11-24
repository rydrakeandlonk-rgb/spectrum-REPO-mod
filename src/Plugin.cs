using System.Collections;
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
    internal static SpawnCatalogSnapshot<RepoSpawnTarget>? Catalog { get; private set; }
    internal static SpawnPolicy SpawnPolicy { get; set; } = SpawnPolicy.Default;
    internal static RepoSpawnExecutor SpawnExecutor { get; } = new();
    private RepoGatewayClient? _gateway;

    private void Awake()
    {
        Log = Logger;
        Log.LogInfo($"{PluginName} {PluginVersion} loaded.");
        Log.LogInfo($"REPOLib {REPOLib.MyPluginInfo.PLUGIN_VERSION} dependency resolved.");
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
            _gateway = new RepoGatewayClient(Config);
            _gateway.Start();
            StartCoroutine(PublishGatewayState());
        }
        catch (System.Exception error)
        {
            Log.LogError($"Could not build the R.E.P.O. catalogue: {error.Message}");
        }
    }

    private IEnumerator PublishGatewayState()
    {
        var nextPing = 0f;
        while (true)
        {
            var runActive = RunManager.instance != null && LevelGenerator.Instance != null;
            var isMaster = runActive && SemiFunc.IsMasterClientOrSingleplayer();
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
