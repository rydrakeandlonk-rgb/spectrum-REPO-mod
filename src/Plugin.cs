using System;
using System.Collections;
using System.Linq;
using System.Threading;
using BepInEx;
using BepInEx.Logging;
using SpectrumRepoMod.Rewards;
using SpectrumRepoMod.Runtime;
using UnityEngine;

namespace SpectrumRepoMod;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class Plugin : BaseUnityPlugin
{
    public const string PluginGuid = "com.spectrum.repomod";
    public const string PluginName = "Spectrum R.E.P.O. Mod";
    public const string PluginVersion = "0.2.0";

    internal static ManualLogSource Log { get; private set; } = null!;
    internal static SpawnCatalogSnapshot<RepoSpawnTarget>? Catalog { get; set; }
    internal static SpawnPolicy SpawnPolicy { get; set; } = SpawnPolicy.Default;
    internal static RepoSpawnExecutor SpawnExecutor { get; } = new();
    private RepoGatewayClient? _gateway;
    private RepoPairingClient? _pairing;
    private CancellationTokenSource? _pairingCancellation;
    private bool _showConnectionPanel;
    private bool _pairingInProgress;
    private string _connectionStatus = "Not connected";
    private string _pairingCode = "";
    private string _verificationUri = "";
    private Rect _connectionWindowRect = new(24f, 24f, 390f, 250f);
    private object? _catalogueLevel;

    private void Awake()
    {
        Log = Logger;
        Log.LogInfo($"{PluginName} {PluginVersion} loaded.");
        _gateway = new RepoGatewayClient(Config);
        _gateway.SpawnRequested += HandleSpawnRequest;
        _gateway.Start();
        _pairing = new RepoPairingClient();
        _showConnectionPanel = !_gateway.IsConfigured;
        _connectionStatus = _gateway.IsConnected ? "Connected to Spectrum" : "Not connected";
        Log.LogInfo("Press F8 to open Spectrum connection settings. Use Connect to Twitch to pair this host.");
        StartCoroutine(WaitForGameCatalogue());
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F8))
            _showConnectionPanel = !_showConnectionPanel;

        if (_gateway?.IsConnected == true)
            _connectionStatus = "Connected to Spectrum";
        else if (!_pairingInProgress && _gateway?.IsConfigured == true)
            _connectionStatus = "Saved connection; reconnecting…";
    }

    private void OnGUI()
    {
        if (!_showConnectionPanel) return;
        _connectionWindowRect = GUI.Window(72841, _connectionWindowRect, DrawConnectionWindow, "Spectrum R.E.P.O.");
    }

    private void DrawConnectionWindow(int windowId)
    {
        GUILayout.BeginVertical();
        GUILayout.Label("Connect this game host to your Spectrum Twitch account.");
        GUILayout.Space(6f);
        GUILayout.Label("Status: " + _connectionStatus);

        if (!string.IsNullOrWhiteSpace(_pairingCode))
        {
            GUILayout.Space(8f);
            GUILayout.Label("Approval code: " + _pairingCode);
            if (GUILayout.Button("Open Control Center"))
                Application.OpenURL(_verificationUri);
            GUILayout.Label("Approve the connection in your browser. This code expires after 10 minutes.");
        }

        GUILayout.Space(10f);
        GUI.enabled = !_pairingInProgress && !(_gateway?.IsConnected ?? false);
        if (GUILayout.Button("Connect to Twitch"))
            BeginPairing();
        GUI.enabled = true;

        if (_pairingInProgress && GUILayout.Button("Cancel pairing"))
            _pairingCancellation?.Cancel();

        if (GUILayout.Button("Close"))
            _showConnectionPanel = false;

        GUILayout.EndVertical();
        GUI.DragWindow(new Rect(0f, 0f, 10000f, 24f));
    }

    private void BeginPairing()
    {
        if (_pairingInProgress || _pairing == null) return;
        _pairingCancellation?.Cancel();
        _pairingCancellation?.Dispose();
        _pairingCancellation = new CancellationTokenSource();
        StartCoroutine(CompletePairing(_pairingCancellation.Token));
    }

    private IEnumerator CompletePairing(CancellationToken cancellationToken)
    {
        _pairingInProgress = true;
        _pairingCode = "";
        _verificationUri = "";
        _connectionStatus = "Requesting a secure pairing code…";

        var startTask = _pairing!.StartAsync(cancellationToken);
        yield return new WaitUntil(() => startTask.IsCompleted);
        if (startTask.IsCanceled || cancellationToken.IsCancellationRequested)
        {
            _connectionStatus = "Pairing cancelled";
            _pairingInProgress = false;
            yield break;
        }
        if (startTask.IsFaulted)
        {
            _connectionStatus = startTask.Exception?.GetBaseException().Message ?? "Could not start pairing";
            _pairingInProgress = false;
            yield break;
        }

        var pairing = startTask.Result;
        _pairingCode = pairing.user_code;
        _verificationUri = pairing.verification_uri;
        _connectionStatus = "Waiting for Twitch approval…";
        Application.OpenURL(_verificationUri);

        var approvalTask = _pairing.WaitForApprovalAsync(pairing, cancellationToken);
        yield return new WaitUntil(() => approvalTask.IsCompleted);
        if (approvalTask.IsCanceled || cancellationToken.IsCancellationRequested)
        {
            _connectionStatus = "Pairing cancelled";
            _pairingInProgress = false;
            _pairingCode = "";
            yield break;
        }
        if (approvalTask.IsFaulted)
        {
            _connectionStatus = approvalTask.Exception?.GetBaseException().Message ?? "Pairing was not completed";
            _pairingInProgress = false;
            _pairingCode = "";
            yield break;
        }

        try
        {
            var credentials = approvalTask.Result;
            _gateway?.ApplyCredentials(credentials.BroadcasterUserId, credentials.HostToken);
            _connectionStatus = "Approved. Connecting to Spectrum…";
            _pairingCode = "";
            _verificationUri = "";
            Log.LogInfo("R.E.P.O. host pairing approved for @" + credentials.BroadcasterLogin + ". Credentials saved locally.");
        }
        catch (Exception error)
        {
            _connectionStatus = "Could not save the approved connection: " + error.Message;
        }
        _pairingInProgress = false;
    }

    private IEnumerator WaitForGameCatalogue()
    {
        StartCoroutine(PublishGatewayState());

        while (true)
        {
            yield return new WaitUntil(() =>
                RunManager.instance != null &&
                LevelGenerator.Instance != null &&
                LevelGenerator.Instance.Level != null &&
                LevelGenerator.Instance.Generated);

            var currentLevel = LevelGenerator.Instance.Level;
            if (!ReferenceEquals(currentLevel, _catalogueLevel))
            {
                try
                {
                    var prefabRefs = RepoCatalogBuilder.GetLevelPrefabRefs();
                    yield return LevelGenerator.Instance.PrefabsPrepare(prefabRefs);

                    Catalog = RepoCatalogBuilder.Build(SpawnPolicy);
                    _catalogueLevel = currentLevel;
                    Log.LogInfo(
                        $"Vanilla valuable catalogue ready: {Catalog.TotalCount} entries, " +
                        $"{Catalog.AllowedCount} eligible, {Catalog.ExcludedCount} excluded.");
                    _gateway?.PublishCatalogue();
                }
                catch (Exception error)
                {
                    Log.LogError($"Could not build the vanilla valuable catalogue: {error}");
                }
            }

            yield return new WaitForSeconds(1f);
        }
    }

    private void HandleSpawnRequest(string categoryName, string requestId)
    {
        if (Catalog == null || _gateway == null)
        {
            _gateway?.CompleteSpawn(requestId, categoryName, false, "", "The live valuable catalogue is not ready.");
            return;
        }

        if (!string.Equals(categoryName, "valuable", StringComparison.OrdinalIgnoreCase))
        {
            _gateway.CompleteSpawn(requestId, categoryName, false, "", "This test build only supports valuable requests.");
            return;
        }

        var category = RewardCategory.Valuable;
        if (!SemiFunc.IsMasterClientOrSingleplayer() || RunManager.instance == null || LevelGenerator.Instance == null)
        {
            _gateway.CompleteSpawn(requestId, categoryName, false, "", "This instance is not the active R.E.P.O. host in a running session.");
            return;
        }

        var candidates = Catalog.AllowedCandidates
            .Where(candidate => candidate.Category == category &&
                SpawnPolicy.TryValidateSpawn(candidate, out _))
            .ToArray();
        if (candidates.Length == 0)
        {
            _gateway.CompleteSpawn(requestId, categoryName, false, "", "No eligible valuables are available in this level.");
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
        catch (Exception error)
        {
            Log.LogError("R.E.P.O. valuable test spawn failed: " + error);
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
        _pairingCancellation?.Cancel();
        _pairingCancellation?.Dispose();
        _pairing?.Dispose();
        _gateway?.Stop();
    }
}
