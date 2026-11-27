using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Logging;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using BepInEx.Configuration;
using UnityEngine;
using SpectrumRepoMod.Runtime;

namespace SpectrumRepoMod;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class Plugin : BaseUnityPlugin, IOnEventCallback
{
    public const string PluginGuid = "com.spectrum.repomod";
    public const string PluginName = "Spectrum R.E.P.O. Mod";
    public const string PluginVersion = "0.8.3";

    internal static Plugin? Instance { get; private set; }
    internal static ManualLogSource Log { get; private set; } = null!;
    internal static RepoRuntimePolicy SpawnPolicy { get; set; } = new(true, true, true);
    internal static RepoCatalogSnapshot Catalog { get; set; } = RepoCatalogSnapshot.Empty;

    private const byte SpawnCalloutEventCode = 198;
    private static readonly Color SpawnCalloutColor = new(1f, 0.82f, 0.35f, 1f);
    private readonly RepoSpawnExecutor _spawnExecutor = new();
    private ConfigEntry<bool> _announcementsEnabled = null!;
    private ConfigEntry<bool> _valuableAnnouncementsEnabled = null!;
    private ConfigEntry<bool> _monsterAnnouncementsEnabled = null!;
    private ConfigEntry<bool> _shopAnnouncementsEnabled = null!;
    private ConfigEntry<string> _valuableAnnouncementPhrases = null!;
    private ConfigEntry<string> _monsterAnnouncementPhrases = null!;
    private ConfigEntry<string> _shopAnnouncementPhrases = null!;
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
        BindAnnouncementSettings();
        PhotonNetwork.AddCallbackTarget(this);

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
        Log.LogInfo("R.E.P.O. redemption request received: request=" + requestId + ", category=" + category + ", targetPhotonId=" + targetPhotonId + ".");

        if (!string.Equals(category, "valuable", StringComparison.Ordinal) &&
            !string.Equals(category, "monster", StringComparison.Ordinal) &&
            !string.Equals(category, "utility_item", StringComparison.Ordinal) &&
            !string.Equals(category, "random", StringComparison.Ordinal))
        {
            _gateway.CompleteSpawn(requestId, category, false, "",
                "This build supports valuables, monsters and approved shop items only.");
            return;
        }

        var result = SpawnRandomCandidate(category, targetPhotonId, out var selectedCategory, out var itemName, out var detail);
        if (result)
            TrySendSpawnCallout(selectedCategory, targetPhotonId);
        _gateway.CompleteSpawn(requestId, selectedCategory, result, itemName, detail);
    }

    private void SpawnLocalTest(string category)
    {
        var localPlayer = PlayerAvatar.instance;
        if (localPlayer == null)
        {
            _pairingStatus = "Local test failed: your player avatar is not available yet.";
            Log.LogWarning("R.E.P.O. local spawn test skipped because PlayerAvatar.instance is null.");
            return;
        }

        var localPhotonId = PhotonNetwork.LocalPlayer?.ActorNumber ?? -1;
        var success = SpawnRandomCandidate(category, localPhotonId, out _, out var itemName, out var detail, localPlayer);
        _pairingStatus = success ? "Local test spawned: " + itemName : "Local test failed: " + detail;
        if (success)
            Log.LogInfo("R.E.P.O. local " + category + " test spawn succeeded: " + itemName + ".");
        else
            Log.LogError("R.E.P.O. local " + category + " test spawn failed: " + detail);
    }

    private bool SpawnRandomCandidate(string requestedCategory, int targetPhotonId, out string selectedCategory, out string itemName, out string detail, PlayerAvatar? targetOverride = null)
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

            var targetCategory = selectedCategory;
            if (!Catalog.AllowedCandidates.Any(row => row.Category == targetCategory))
                Catalog = RepoCatalogBuilder.Build(SpawnPolicy);

            var candidates = Catalog.AllowedCandidates.Where(row => row.Category == targetCategory).ToArray();
            if (candidates.Length == 0)
            {
                if (selectedCategory == "utility_item")
                {
                    var matched = Catalog.Rows.Count(row => row.Category == targetCategory);
                    var excluded = Catalog.Rows.FirstOrDefault(row => row.Category == targetCategory && !row.Allowed);
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

            var player = targetOverride ?? ResolveTargetPlayer(targetPhotonId);
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

            var position = selectedCategory == "monster"
                ? player.transform.position + Vector3.down * 20f
                : player.transform.position + forward * 1.35f + Vector3.up * 0.7f;
            var rotation = Quaternion.LookRotation(forward, Vector3.up);
            var success = _spawnExecutor.TrySpawn(candidate, SpawnPolicy, position, rotation, out detail);
            if (success)
                itemName = candidate.DisplayName;
            return success;
        }
        catch (Exception error)
        {
            detail = "The game rejected the " + selectedCategory + " spawn: " + error.Message;
            Log.LogError("R.E.P.O. " + selectedCategory + " spawn failed: " + error);
            return false;
        }
    }


    private static PlayerAvatar? ResolveTargetPlayer(int photonId)
    {
        var room = PhotonNetwork.CurrentRoom;
        var localActorNumber = PhotonNetwork.LocalPlayer?.ActorNumber ?? 0;
        var localAvatar = PlayerAvatar.instance;

        Log.LogInfo("Resolving R.E.P.O. redemption target: targetPhotonId=" + photonId +
            ", localActorNumber=" + localActorNumber +
            ", localAvatarAvailable=" + (localAvatar != null) +
            ", room=" + (room?.Name ?? "<single-player/no room>") + ".");

        if (photonId <= 0)
        {
            if (room == null && localAvatar != null)
            {
                Log.LogInfo("Using the local player avatar because this is a single-player session.");
                return localAvatar;
            }

            Log.LogWarning("Cannot resolve redemption target: the supplied Photon actor ID is not valid for this lobby.");
            return null;
        }

        // The host's own PlayerAvatar is already available directly. Prefer it when
        // the authoritative target ID matches the local Photon actor number rather
        // than depending on a game helper lookup that can return null during setup.
        if (photonId == localActorNumber && localAvatar != null)
        {
            Log.LogInfo("Resolved redemption target directly to the local host avatar (Photon actor " + photonId + ").");
            return localAvatar;
        }

        try
        {
            var method = typeof(SemiFunc).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .FirstOrDefault(candidate =>
                    candidate.Name == "PlayerAvatarGetFromPhotonID" &&
                    candidate.GetParameters().Length == 1 &&
                    candidate.GetParameters()[0].ParameterType == typeof(int));

            if (method == null)
            {
                Log.LogWarning("R.E.P.O. target lookup helper PlayerAvatarGetFromPhotonID(int) was not found.");
                return null;
            }

            var resolved = method.Invoke(null, new object[] { photonId }) as PlayerAvatar;
            if (resolved != null)
            {
                Log.LogInfo("Resolved redemption target through PlayerAvatarGetFromPhotonID: Photon actor " + photonId + ".");
                return resolved;
            }

            Log.LogWarning("PlayerAvatarGetFromPhotonID returned null for target Photon actor " + photonId +
                ". Local actor is " + localActorNumber + "; local avatar available=" + (localAvatar != null) + ".");
        }
        catch (Exception error)
        {
            Log.LogWarning("Could not resolve R.E.P.O. redemption target Photon actor " + photonId + ": " + error);
        }

        return null;
    }

    private void BindAnnouncementSettings()
    {
        _announcementsEnabled = Config.Bind("Spawn Announcements", "Enabled", true,
            "Master switch for native in-game spawn callouts.");
        _valuableAnnouncementsEnabled = Config.Bind("Spawn Announcements", "ValuablesEnabled", true,
            "Allow callouts when a valuable is spawned.");
        _monsterAnnouncementsEnabled = Config.Bind("Spawn Announcements", "MonstersEnabled", true,
            "Allow callouts when a monster is spawned.");
        _shopAnnouncementsEnabled = Config.Bind("Spawn Announcements", "ShopItemsEnabled", true,
            "Allow callouts when a shop item is spawned.");
        _valuableAnnouncementPhrases = Config.Bind("Spawn Announcements", "ValuablePhrases",
            "Ooh, what's this? | Valuable incoming! | Something shiny just arrived! | Delivery for me!",
            "Separate phrases with |. A random phrase is selected for each valuable.");
        _monsterAnnouncementPhrases = Config.Bind("Spawn Announcements", "MonsterPhrases",
            "We've got company! | Uh, that's not friendly! | Something just joined the party! | I have a bad feeling about this.",
            "Separate phrases with |. A random phrase is selected for each monster.");
        _shopAnnouncementPhrases = Config.Bind("Spawn Announcements", "ShopItemPhrases",
            "Item spawning! | New equipment! | Look what just arrived! | A little upgrade never hurts!",
            "Separate phrases with |. A random phrase is selected for each shop item.");
        Config.Save();
    }

    private void TrySendSpawnCallout(string category, int targetPhotonId)
    {
        try
        {
            if (!_announcementsEnabled.Value || targetPhotonId <= 0)
                return;

            ConfigEntry<bool> enabled;
            ConfigEntry<string> phrases;
            switch (category)
            {
                case "valuable":
                    enabled = _valuableAnnouncementsEnabled;
                    phrases = _valuableAnnouncementPhrases;
                    break;
                case "monster":
                    enabled = _monsterAnnouncementsEnabled;
                    phrases = _monsterAnnouncementPhrases;
                    break;
                case "utility_item":
                    enabled = _shopAnnouncementsEnabled;
                    phrases = _shopAnnouncementPhrases;
                    break;
                default:
                    return;
            }

            if (!enabled.Value)
                return;

            var options = phrases.Value.Split(new[] { '|' },
                StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line => line.Length > 180 ? line.Substring(0, 180) : line)
                .ToArray();
            if (options.Length == 0)
                return;

            var phrase = options[UnityEngine.Random.Range(0, options.Length)];
            var localActor = PhotonNetwork.LocalPlayer?.ActorNumber ?? 0;
            if (targetPhotonId == localActor)
            {
                PlaySpawnCallout(phrase);
                return;
            }

            if (PhotonNetwork.CurrentRoom == null)
                return;

            var sent = PhotonNetwork.RaiseEvent(SpawnCalloutEventCode,
                new object[] { phrase },
                new RaiseEventOptions { TargetActors = new[] { targetPhotonId } },
                SendOptions.SendReliable);
            if (!sent)
                Log.LogWarning("Spawn succeeded, but its player-specific callout could not be queued for Photon actor " + targetPhotonId + ".");
        }
        catch (Exception error)
        {
            Log.LogWarning("Spawn succeeded, but its callout could not be sent: " + error.Message);
        }
    }

    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent == null || photonEvent.Code != SpawnCalloutEventCode)
            return;

        var localActor = PhotonNetwork.LocalPlayer?.ActorNumber ?? 0;
        var masterActor = PhotonNetwork.MasterClient?.ActorNumber ?? 0;
        if (localActor <= 0 || masterActor <= 0 || photonEvent.Sender != masterActor)
            return;

        try
        {
            if (!(photonEvent.CustomData is object[] payload) ||
                payload.Length < 1 ||
                !(payload[0] is string phrase) ||
                string.IsNullOrWhiteSpace(phrase))
                return;

            PlaySpawnCallout(phrase);
        }
        catch (Exception error)
        {
            Log.LogWarning("Could not play the received spawn callout: " + error.Message);
        }
    }

    private static void PlaySpawnCallout(string phrase)
    {
        try
        {
            if (ChatManager.instance == null)
                return;

            var localeCode = TTSPronunciation.LocaleCodeGet(SemiFunc.LocaleGetCode());
            ChatManager.instance.PossessChatScheduleStart(10);
            ChatManager.instance.PossessChat(ChatManager.PossessChatID.LovePotion,
                phrase, 1f, SpawnCalloutColor, 0f, sendInTaxmanChat: false, 0, null, localeCode);
            ChatManager.instance.PossessChatScheduleEnd();
        }
        catch (Exception error)
        {
            Log.LogWarning("Could not play native R.E.P.O. spawn callout: " + error.Message);
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
        PhotonNetwork.RemoveCallbackTarget(this);
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
