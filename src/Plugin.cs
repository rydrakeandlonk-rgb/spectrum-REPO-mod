using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Logging;
using Photon.Pun;
using UnityEngine;
using SpectrumRepoMod.Runtime;

namespace SpectrumRepoMod;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class Plugin : BaseUnityPlugin
{
    public const string PluginGuid = "com.spectrum.repomod";
    public const string PluginName = "Spectrum R.E.P.O. Mod";
    public const string PluginVersion = "0.4.0";

    internal static Plugin? Instance { get; private set; }
    internal static ManualLogSource Log { get; private set; } = null!;
    internal static RepoRuntimePolicy SpawnPolicy { get; set; } = new(true);
    internal static RepoCatalogSnapshot Catalog { get; set; } = RepoCatalogSnapshot.Empty;

    private readonly RepoSpawnExecutor _spawnExecutor = new();
    private readonly HashSet<string> _processedRequests = new(StringComparer.Ordinal);
    private readonly Queue<string> _processedRequestOrder = new();
    private RepoPairingClient? _pairingClient;
    private RepoGatewayClient? _gateway;
    private CancellationTokenSource? _pairingCancellation;
    private bool _showPanel;
    private bool _pairingInProgress;
    private string _pairingStatus = "Not connected";
    private string _pairingCode = "";
    private string _lastLobbyId = "";
    private bool _lastRunActive;
    private bool _lastIsMaster;
    private int _lastLevelInstanceId = -1;
    private float _nextStateReportAt;
    private Rect _windowRect = new(24f, 24f, 430f, 310f);

    private void Awake()
    {
        Instance = this;
        Log = Logger;
        SpawnPolicy = new RepoRuntimePolicy(true);
        Catalog = RepoCatalogSnapshot.Empty;

        _pairingClient = new RepoPairingClient();
        _gateway = new RepoGatewayClient(Config);
        _gateway.SpawnRequested += HandleSpawnRequest;
        _gateway.Start();

        _showPanel = !_gateway.IsConfigured;
        Log.LogInfo("Spectrum R.E.P.O. Mod 0.4.0 initialised. Press F8 to open its panel.");
    }

    private void Start()
    {
        PublishCurrentState(true);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F8))
            _showPanel = !_showPanel;

        _gateway?.PumpMainThread();
        RefreshRuntimeState(false);
    }

    private void OnGUI()
    {
        if (_showPanel)
            _windowRect = GUILayout.Window(731045, _windowRect, DrawWindow, "Spectrum R.E.P.O.");
    }

    private void DrawWindow(int windowId)
    {
        GUILayout.Label("Version " + PluginVersion);
        GUILayout.Space(4f);
        GUILayout.Label("Gateway: " + (_gateway == null ? "Unavailable" :
            _gateway.IsConnected ? "Connected" : _gateway.IsConfigured ? "Reconnecting..." : "Not paired"));

        var level = LevelGenerator.Instance?.Level;
        var inRun = RunManager.instance != null && level != null && ValuableDirector.instance != null;
        var room = PhotonNetwork.CurrentRoom;
        GUILayout.Label("Game: " + (inRun ? "Run active" : "Waiting for an active run"));
        GUILayout.Label("Lobby: " + (room == null ? "Single-player / not in a lobby" : room.Name));
        GUILayout.Label("Valuable catalogue: " + Catalog.AllowedCandidates.Count + " available");

        if (!string.IsNullOrWhiteSpace(_pairingStatus))
            GUILayout.Label("Twitch: " + _pairingStatus);
        if (!string.IsNullOrWhiteSpace(_pairingCode))
            GUILayout.Label("Approval code: " + _pairingCode);

        GUILayout.Space(8f);
        GUI.enabled = !_pairingInProgress;
        if (GUILayout.Button(_gateway?.IsConfigured == true ? "Connect another Twitch account" : "Connect Twitch"))
            BeginPairing();
        GUI.enabled = true;

        if (inRun && SemiFunc.IsMasterClientOrSingleplayer())
        {
            GUILayout.Space(4f);
            if (GUILayout.Button("Test spawn one valuable beside me"))
                SpawnLocalTestValuable();
        }

        GUILayout.Space(4f);
        if (GUILayout.Button("Close panel (F8 to reopen)"))
            _showPanel = false;

        GUI.DragWindow(new Rect(0f, 0f, 10000f, 24f));
    }

    private async void BeginPairing()
    {
        if (_pairingInProgress || _pairingClient == null || _gateway == null)
            return;

        _pairingInProgress = true;
        _pairingCode = "";
        _pairingStatus = "Requesting a pairing code...";
        _pairingCancellation?.Cancel();
        _pairingCancellation?.Dispose();
        _pairingCancellation = new CancellationTokenSource();
        var cancellationToken = _pairingCancellation.Token;

        try
        {
            var pairing = await _pairingClient.StartAsync(cancellationToken);
            _pairingCode = pairing.user_code;
            _pairingStatus = "Approve the code in Control Center. Your browser is opening...";
            Application.OpenURL(pairing.verification_uri);

            var approved = await _pairingClient.WaitForApprovalAsync(pairing, cancellationToken);
            _gateway.ApplyCredentials(approved.BroadcasterUserId, approved.HostToken);
            _pairingStatus = "Paired as " + approved.BroadcasterLogin;
            _pairingCode = "";
            PublishCurrentState(true);
            Log.LogInfo("R.E.P.O. host pairing completed for " + approved.BroadcasterLogin + ".");
        }
        catch (OperationCanceledException)
        {
            _pairingStatus = "Pairing cancelled.";
        }
        catch (Exception error)
        {
            _pairingStatus = error.Message;
            Log.LogError("R.E.P.O. pairing failed: " + error.Message);
        }
        finally
        {
            _pairingInProgress = false;
        }
    }

    internal void PublishCurrentState(bool force)
    {
        if (_gateway == null) return;

        var level = LevelGenerator.Instance?.Level;
        var runActive = RunManager.instance != null && level != null && ValuableDirector.instance != null;
        var room = PhotonNetwork.CurrentRoom;
        var lobbyId = runActive && room != null ? room.Name ?? "" : "";
        var isMaster = runActive && SemiFunc.IsMasterClientOrSingleplayer();
        var levelId = level == null ? 0 : level.GetInstanceID();

        var stateChanged = runActive != _lastRunActive ||
            !string.Equals(lobbyId, _lastLobbyId, StringComparison.Ordinal) ||
            isMaster != _lastIsMaster;
        var levelChanged = levelId != _lastLevelInstanceId;
        var reportDue = Time.unscaledTime >= _nextStateReportAt;

        if (!force && !stateChanged && !levelChanged && !reportDue)
            return;

        if (force || levelChanged)
            Catalog = RepoCatalogBuilder.Build(SpawnPolicy);

        _gateway.SendSessionState(runActive, lobbyId, isMaster);
        if (force || stateChanged || levelChanged)
            _gateway.PublishCatalogue(runActive, lobbyId, isMaster);

        _lastRunActive = runActive;
        _lastLobbyId = lobbyId;
        _lastIsMaster = isMaster;
        _lastLevelInstanceId = levelId;
        _nextStateReportAt = Time.unscaledTime + 10f;
    }

    private void RefreshRuntimeState(bool force) => PublishCurrentState(force);

    private void HandleSpawnRequest(string category, string requestId)
    {
        if (string.IsNullOrWhiteSpace(requestId) || _gateway == null)
            return;

        if (_processedRequests.Contains(requestId))
        {
            _gateway.CompleteSpawn(requestId, category, true, "", "Duplicate request ignored; no second spawn was created.");
            return;
        }

        RememberRequest(requestId);

        if (!string.Equals(category, "valuable", StringComparison.Ordinal))
        {
            _gateway.CompleteSpawn(requestId, category, false, "", "This test build supports valuable collectibles only.");
            return;
        }

        var result = SpawnRandomValuable(out var itemName, out var detail);
        _gateway.CompleteSpawn(requestId, "valuable", result, itemName, detail);
    }

    private void SpawnLocalTestValuable()
    {
        var success = SpawnRandomValuable(out var itemName, out var detail);
        _pairingStatus = success ? "Local test spawned: " + itemName : "Local test failed: " + detail;
    }

    private bool SpawnRandomValuable(out string itemName, out string detail)
    {
        itemName = "";
        detail = "";

        try
        {
            if (!SemiFunc.IsMasterClientOrSingleplayer())
            {
                detail = "Only the current game host can execute a shared spawn.";
                return false;
            }

            if (Catalog.AllowedCandidates.Count == 0)
                Catalog = RepoCatalogBuilder.Build(SpawnPolicy);

            if (Catalog.AllowedCandidates.Count == 0)
            {
                detail = "No eligible valuable collectibles are available in this level.";
                return false;
            }

            var player = PlayerAvatar.instance;
            if (player == null)
            {
                detail = "The local player is not ready.";
                return false;
            }

            var candidate = Catalog.AllowedCandidates[UnityEngine.Random.Range(0, Catalog.AllowedCandidates.Count)];
            var forward = player.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f)
                forward = Vector3.forward;
            forward.Normalize();

            // Spawn just ahead of the local player, slightly below waist height.
            var position = player.transform.position + forward * 1.35f + Vector3.up * 0.7f;
            var rotation = Quaternion.LookRotation(forward, Vector3.up);
            var success = _spawnExecutor.TrySpawn(candidate, SpawnPolicy, position, rotation, out detail);
            if (success)
                itemName = candidate.DisplayName;
            return success;
        }
        catch (Exception error)
        {
            detail = "The game rejected the valuable spawn: " + error.Message;
            Log.LogError("R.E.P.O. valuable spawn failed: " + error);
            return false;
        }
    }

    private void RememberRequest(string requestId)
    {
        if (!_processedRequests.Add(requestId)) return;
        _processedRequestOrder.Enqueue(requestId);
        while (_processedRequestOrder.Count > 512)
            _processedRequests.Remove(_processedRequestOrder.Dequeue());
    }

    private void OnDestroy()
    {
        _pairingCancellation?.Cancel();
        _pairingCancellation?.Dispose();
        _pairingClient?.Dispose();
        if (_gateway != null)
        {
            _gateway.SpawnRequested -= HandleSpawnRequest;
            _gateway.Stop();
        }
        if (Instance == this) Instance = null;
    }
}
