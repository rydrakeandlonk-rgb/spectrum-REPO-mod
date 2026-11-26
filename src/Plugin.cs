using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
    public const string PluginVersion = "0.8.0";

    internal static Plugin? Instance { get; private set; }
    internal static ManualLogSource Log { get; private set; } = null!;
    internal static RepoRuntimePolicy SpawnPolicy { get; set; } = new(true, true, true);
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
    private float _nextCatalogueRefreshAt;
    private Rect _windowRect = new(24f, 24f, 430f, 310f);

    private void Awake()
    {
        Instance = this;
        Log = Logger;
        SpawnPolicy = new RepoRuntimePolicy(true, true, true);
        Catalog = RepoCatalogSnapshot.Empty;

        _pairingClient = new RepoPairingClient();
        _gateway = new RepoGatewayClient(Config);
        _gateway.SpawnRequested += HandleSpawnRequest;
        _gateway.Start();

        _showPanel = !_gateway.IsConfigured;
        Log.LogInfo(PluginName + " " + PluginVersion + " initialised. Press F8 to open its panel.");
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
        GUILayout.Label("Valuables: " + Catalog.AllowedCandidates.Count(row => row.Category == "valuable") +
            " | Monsters: " + Catalog.AllowedCandidates.Count(row => row.Category == "monster") +
            " | Tools: " + Catalog.AllowedCandidates.Count(row => row.Category == "utility_item"));

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
                SpawnLocalTest("valuable");
            if (GUILayout.Button("Test spawn one monster beside me"))
                SpawnLocalTest("monster");
            if (GUILayout.Button("Test spawn one shop item beside me"))
                SpawnLocalTest("utility_item");
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
        var catalogueRefreshDue = Time.unscaledTime >= _nextCatalogueRefreshAt;

        if (!force && !stateChanged && !levelChanged && !reportDue && !catalogueRefreshDue)
            return;

        // Mod loaders may register bundles and assets after our first level scan.
        // Re-scan at a bounded interval rather than performing expensive Unity
        // resource discovery every frame.
        if (force || levelChanged || catalogueRefreshDue)
        {
            try
            {
                Catalog = RepoCatalogBuilder.Build(SpawnPolicy);
            }
            catch (Exception error)
            {
                Catalog = RepoCatalogSnapshot.Empty;
                Log.LogError("Could not rebuild the current R.E.P.O. spawn catalogue: " + error.Message);
            }
        }

        _gateway.SendSessionState(runActive, lobbyId, isMaster, PhotonNetwork.LocalPlayer?.ActorNumber ?? 0);
        if (force || stateChanged || levelChanged || catalogueRefreshDue)
            _gateway.PublishCatalogue(runActive, lobbyId, isMaster);

        _lastRunActive = runActive;
        _lastLobbyId = lobbyId;
        _lastIsMaster = isMaster;
        _lastLevelInstanceId = levelId;
        _nextStateReportAt = Time.unscaledTime + 10f;
        if (force || levelChanged || catalogueRefreshDue)
            _nextCatalogueRefreshAt = Time.unscaledTime + 30f;
    }

    private void RefreshRuntimeState(bool force) => PublishCurrentState(force);

    private void HandleSpawnRequest(string category, string requestId, int targetPhotonId)
    {
        if (string.IsNullOrWhiteSpace(requestId) || _gateway == null)
            return;

        if (_processedRequests.Contains(requestId))
        {
            _gateway.CompleteSpawn(requestId, category, true, "", "Duplicate request ignored; no second spawn was created.");
            return;
        }

        RememberRequest(requestId);

        if (!string.Equals(category, "valuable", StringComparison.Ordinal) &&
            !string.Equals(category, "monster", StringComparison.Ordinal) &&
            !string.Equals(category, "utility_item", StringComparison.Ordinal))
        {
            _gateway.CompleteSpawn(requestId, category, false, "",
                "This build supports valuables, monsters and approved shop items only.");
            return;
        }

        var result = SpawnRandomCandidate(category, targetPhotonId, out var selectedCategory, out var itemName, out var detail);
        _gateway.CompleteSpawn(requestId, selectedCategory, result, itemName, detail);
    }

    private void SpawnLocalTest(string category)
    {
        var localPhotonId = PhotonNetwork.LocalPlayer?.ActorNumber ?? -1;
        var success = SpawnRandomCandidate(category, localPhotonId, out _, out var itemName, out var detail);
        _pairingStatus = success ? "Local test spawned: " + itemName : "Local test failed: " + detail;
    }

    private bool SpawnRandomCandidate(string requestedCategory, int targetPhotonId, out string selectedCategory, out string itemName, out string detail)
    {
        selectedCategory = requestedCategory;
        itemName = "";
        detail = "";

        try
        {
            if (!SemiFunc.IsMasterClientOrSingleplayer())
            {
                detail = "Only the current game host can execute a shared spawn.";
                return false;
            }

            if (requestedCategory == "random")
            {
                var available = new[]
                {
                    new { Category = "monster", Weight = SpawnPolicy.MonsterWeight },
                    new { Category = "utility_item", Weight = SpawnPolicy.UtilityItemWeight },
                    new { Category = "valuable", Weight = SpawnPolicy.ValuableWeight }
                }.Where(entry => entry.Weight > 0d && Catalog.AllowedCandidates.Any(row => row.Category == entry.Category)).ToArray();
                if (available.Length == 0)
                {
                    Catalog = RepoCatalogBuilder.Build(SpawnPolicy);
                    available = new[]
                    {
                        new { Category = "monster", Weight = SpawnPolicy.MonsterWeight },
                        new { Category = "utility_item", Weight = SpawnPolicy.UtilityItemWeight },
                        new { Category = "valuable", Weight = SpawnPolicy.ValuableWeight }
                    }.Where(entry => entry.Weight > 0d && Catalog.AllowedCandidates.Any(row => row.Category == entry.Category)).ToArray();
                }
                if (available.Length == 0)
                {
                    detail = "No enabled category currently has an eligible spawn candidate.";
                    return false;
                }
                var totalWeight = available.Sum(entry => entry.Weight);
                var roll = UnityEngine.Random.value * (float)totalWeight;
                var chosen = available[available.Length - 1];
                foreach (var entry in available)
                {
                    if (roll < entry.Weight) { chosen = entry; break; }
                    roll -= (float)entry.Weight;
                }
                selectedCategory = chosen.Category;
            }

            if (!Catalog.AllowedCandidates.Any(row => row.Category == selectedCategory))
                Catalog = RepoCatalogBuilder.Build(SpawnPolicy);

            var candidates = Catalog.AllowedCandidates.Where(row => row.Category == selectedCategory).ToArray();
            if (candidates.Length == 0)
            {
                if (selectedCategory == "utility_item")
                {
                    var matched = Catalog.Rows.Count(row => row.Category == category);
                    var excluded = Catalog.Rows.FirstOrDefault(row => row.Category == category && !row.Allowed);
                    detail = matched == 0
                        ? "No approved non-combat shop entries matched the loaded game assets. Check the BepInEx log for the catalogue diagnostic."
                        : matched + " approved shop entries matched, but none passed validation. First exclusion: " +
                            (excluded?.DisplayName ?? "unknown") + " — " + (excluded?.ExclusionReason ?? "no reason reported");
                }
                else
                {
                    detail = "No eligible " + selectedCategory + " entries are currently available.";
                }
                return false;
            }

            var player = ResolveTargetPlayer(targetPhotonId);
            if (player == null)
            {
                detail = "The redemption target is no longer present in this lobby.";
                return false;
            }

            var candidate = candidates[UnityEngine.Random.Range(0, candidates.Length)];
            var forward = player.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f)
                forward = Vector3.forward;
            forward.Normalize();

            var position = player.transform.position + forward * 1.35f + Vector3.up * 0.7f;
            var rotation = Quaternion.LookRotation(forward, Vector3.up);
            var success = _spawnExecutor.TrySpawn(candidate, SpawnPolicy, position, rotation, out detail);
            if (success)
                itemName = candidate.DisplayName;
            return success;
        }
        catch (Exception error)
        {
            detail = "The game rejected the " + category + " spawn: " + error.Message;
            Log.LogError("R.E.P.O. " + selectedCategory + " spawn failed: " + error);
            return false;
        }
    }


    private static PlayerAvatar? ResolveTargetPlayer(int photonId)
    {
        if (photonId <= 0) return PhotonNetwork.CurrentRoom == null ? PlayerAvatar.instance : null;
        try
        {
            var method = typeof(SemiFunc).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .FirstOrDefault(candidate =>
                    candidate.Name == "PlayerAvatarGetFromPhotonID" &&
                    candidate.GetParameters().Length == 1 &&
                    candidate.GetParameters()[0].ParameterType == typeof(int));
            if (method != null)
                return method.Invoke(null, new object[] { photonId }) as PlayerAvatar;
        }
        catch (Exception error)
        {
            Log.LogWarning("Could not resolve the R.E.P.O. redemption target: " + error.Message);
        }
        return null;
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
