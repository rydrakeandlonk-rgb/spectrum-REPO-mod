using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BepInEx.Configuration;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace SpectrumRepoMod.Runtime;

internal sealed class RepoGatewayClient
{
    private readonly ConfigFile _config;
    private readonly ConfigEntry<string> _gatewayUrl;
    private readonly ConfigEntry<string> _hostToken;
    private readonly string _sessionId = Guid.NewGuid().ToString("N");
    private readonly CancellationTokenSource _cancellation = new();
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly ConcurrentQueue<Action> _mainThreadActions = new();
    private ClientWebSocket? _socket;
    private bool _connectionStarted;

    public RepoGatewayClient(ConfigFile config)
    {
        _config = config;
        _gatewayUrl = config.Bind("Spectrum Gateway", "GatewayUrl", "",
            "Gateway address assigned during Spectrum Control Center pairing.");
        _hostToken = config.Bind("Spectrum Gateway", "HostToken", "",
            "Private R.E.P.O. host credential. Keep this value private.");
    }

    public event Action<string, string, int>? SpawnRequested;
    public bool IsConnected => _socket?.State == WebSocketState.Open;
    public bool IsConfigured =>
        Uri.TryCreate(_gatewayUrl.Value, UriKind.Absolute, out var uri) &&
        (uri.Scheme == "wss" || uri.Scheme == "https") &&
        !string.IsNullOrWhiteSpace(_hostToken.Value);

    public void ApplyCredentials(string broadcasterUserId, string hostToken)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(broadcasterUserId ?? "", "^[0-9]{1,30}$") ||
            string.IsNullOrWhiteSpace(hostToken))
            throw new ArgumentException("The approved R.E.P.O. host credentials are invalid.");

        _gatewayUrl.Value = "wss://spectrum-game-mods.rydrakeandlonk.workers.dev/connect/repo/" + broadcasterUserId;
        _hostToken.Value = hostToken;
        _config.Save();

        if (_connectionStarted)
        {
            try { _socket?.Abort(); } catch { }
        }
        else
        {
            Start();
        }
    }

    public void Start()
    {
        if (!IsConfigured)
        {
            Plugin.Log.LogInfo("Spectrum gateway is not paired. Open the F8 panel to connect Twitch.");
            return;
        }
        if (_connectionStarted) return;
        _connectionStarted = true;
        _ = Task.Run(() => ConnectionLoop(_cancellation.Token));
    }

    public void Stop()
    {
        _cancellation.Cancel();
        try { _socket?.Abort(); } catch { }
        try { _socket?.Dispose(); } catch { }
    }

    public void PumpMainThread()
    {
        while (_mainThreadActions.TryDequeue(out var action))
        {
            try { action(); }
            catch (Exception error) { Plugin.Log.LogError("Gateway main-thread action failed: " + error.Message); }
        }
    }

    public void SendSessionState(bool runActive, string lobbyId, bool isMasterClient, int photonId)
    {
        _ = Send(new SessionStateMessage
        {
            type = "session.state",
            run_active = runActive,
            lobby_id = lobbyId ?? "",
            is_master_client = isMasterClient,
            photon_id = photonId
        }, _cancellation.Token);
    }

    public void CompleteSpawn(string requestId, string category, bool success, string itemName, string detail)
    {
        _ = Send(new SpawnAckMessage
        {
            type = "command.ack",
            request_id = requestId,
            category = category,
            success = success,
            item_name = itemName ?? "",
            detail = detail ?? ""
        }, _cancellation.Token);
    }

    public void SendDiagnosticsSnapshot(string lobbyId)
    {
        try
        {
            var photonPlayers = new List<PhotonPlayerSnapshot>();
            foreach (var player in PhotonNetwork.PlayerList ?? Array.Empty<Player>())
            {
                if (player == null) continue;
                photonPlayers.Add(new PhotonPlayerSnapshot
                {
                    actor_number = player.ActorNumber,
                    user_id = player.UserId ?? "",
                    nickname = player.NickName ?? ""
                });
            }

            var avatarOwners = new List<AvatarOwnerSnapshot>();
            var avatars = SemiFunc.PlayerGetList();
            if (avatars != null)
            {
                foreach (var avatar in avatars)
                {
                    if (avatar == null) continue;
                    var view = avatar.photonView;
                    avatarOwners.Add(new AvatarOwnerSnapshot
                    {
                        owner_actor_number = view == null ? 0 : view.OwnerActorNr,
                        photon_view_id = view == null ? 0 : view.ViewID
                    });
                }
            }

            _ = Send(new DiagnosticsSnapshotMessage
            {
                type = "repo.diagnostics.snapshot",
                mod_version = Plugin.PluginVersion,
                lobby_id = lobbyId ?? "",
                local_actor_number = PhotonNetwork.LocalPlayer?.ActorNumber ?? 0,
                master_actor_number = PhotonNetwork.MasterClient?.ActorNumber ?? 0,
                local_avatar_available = PlayerAvatar.instance != null,
                photon_player_count = photonPlayers.Count,
                photon_players = photonPlayers.ToArray(),
                avatar_owners = avatarOwners.ToArray()
            }, _cancellation.Token);
        }
        catch (Exception error)
        {
            Plugin.Log.LogWarning("Could not publish R.E.P.O. host diagnostics snapshot: " + error.Message);
        }
    }

    public void PublishCatalogue(bool runActive, string lobbyId, bool isMasterClient)
    {
        var catalog = Plugin.Catalog;
        if (catalog == null) return;

        var entries = new System.Collections.Generic.List<RepoCatalogueEntry>();
        foreach (var row in catalog.Rows)
        {
            entries.Add(new RepoCatalogueEntry
            {
                category = row.Category,
                id = row.Id,
                name = row.DisplayName,
                eligible = row.Allowed,
                reason = row.Allowed ? "" : row.ExclusionReason
            });
        }

        _ = Send(new CatalogueMessage
        {
            type = "repo.catalogue.publish",
            mod_version = Plugin.PluginVersion,
            entries = entries.ToArray(),
            session = new CatalogueSession
            {
                run_active = runActive,
                lobby_id = lobbyId ?? "",
                is_master_client = isMasterClient
            }
        }, _cancellation.Token);
    }

    private async Task ConnectionLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            using var socket = new ClientWebSocket();
            try
            {
                var baseUrl = _gatewayUrl.Value.Trim();
                if (baseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    baseUrl = "wss://" + baseUrl.Substring("https://".Length);
                var builder = new UriBuilder(baseUrl);
                var query = string.IsNullOrWhiteSpace(builder.Query) ? "" : builder.Query.TrimStart('?') + "&";
                builder.Query = query + "session_id=" + Uri.EscapeDataString(_sessionId);
                socket.Options.SetRequestHeader("Authorization", "Bearer " + _hostToken.Value.Trim());
                _socket = socket;
                await socket.ConnectAsync(builder.Uri, cancellationToken);
                Plugin.Log.LogInfo("Connected to the Spectrum R.E.P.O. gateway.");
                await ReceiveLoop(socket, cancellationToken);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception error)
            {
                Plugin.Log.LogWarning("Spectrum gateway connection failed: " + error.Message);
            }
            finally
            {
                if (ReferenceEquals(_socket, socket)) _socket = null;
            }

            try { await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task ReceiveLoop(ClientWebSocket socket, CancellationToken cancellationToken)
    {
        var buffer = new byte[16 * 1024];
        while (socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
        {
            using var messageStream = new MemoryStream();
            WebSocketReceiveResult result;
            do
            {
                result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                if (result.MessageType == WebSocketMessageType.Close) return;
                messageStream.Write(buffer, 0, result.Count);
                if (messageStream.Length > 1024 * 1024)
                    throw new InvalidOperationException("Gateway message exceeded the 1 MB safety limit.");
            } while (!result.EndOfMessage);

            var message = Encoding.UTF8.GetString(messageStream.ToArray());
            _mainThreadActions.Enqueue(() => HandleGatewayMessage(message));
        }
    }

    private void HandleGatewayMessage(string message)
    {
        GatewayMessage? envelope;
        try { envelope = JsonUtility.FromJson<GatewayMessage>(message); }
        catch (Exception error)
        {
            Plugin.Log.LogWarning("Could not parse a Spectrum gateway message: " + error.Message);
            return;
        }
        if (envelope == null) return;

        if (envelope.type == "session.ready")
        {
            ApplyRemotePolicy(envelope.config);
            Plugin.Instance?.PublishCurrentState(true);
            Plugin.Log.LogInfo("Spectrum R.E.P.O. session registered with the gateway.");
        }
        else if (envelope.type == "repo.config.update")
        {
            ApplyRemotePolicy(envelope.config);
            Plugin.Instance?.PublishCurrentState(true);
        }
        else if (envelope.type == "repo.config.catalogue.request")
        {
            Plugin.Instance?.PublishCurrentState(true);
        }
        else if (envelope.type == "repo.catalogue.ack" && envelope.ok)
        {
            Plugin.Log.LogInfo("Live R.E.P.O. catalogue published to Control Center.");
        }
        else if (envelope.type == "repo.spawn.request")
        {
            SpawnRequested?.Invoke(envelope.category ?? "", envelope.request_id ?? "", envelope.target_photon_id);
        }
    }

    private void ApplyRemotePolicy(RepoConfigPayload? config)
    {
        if (config?.categories == null) return;
        try
        {
            Plugin.SpawnPolicy = new RepoRuntimePolicy(
                config.categories.valuable?.enabled ?? true,
                config.categories.monster?.enabled ?? true,
                config.categories.utility_item?.enabled ?? true,
                config.categories.monster?.weight ?? 1d,
                config.categories.utility_item?.weight ?? 1d,
                config.categories.valuable?.weight ?? 1d);
            Plugin.Catalog = RepoCatalogBuilder.Build(Plugin.SpawnPolicy);
        }
        catch (Exception error)
        {
            Plugin.Log.LogError("Could not apply the Control Center spawn policy: " + error.Message);
            Plugin.SpawnPolicy = new RepoRuntimePolicy(false, false, false);
            Plugin.Catalog = RepoCatalogSnapshot.Empty;
        }
    }

    private async Task Send<T>(T message, CancellationToken cancellationToken)
    {
        var socket = _socket;
        if (socket == null || socket.State != WebSocketState.Open) return;
        var bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(message));
        try
        {
            await _sendLock.WaitAsync(cancellationToken);
            try
            {
                if (socket.State == WebSocketState.Open)
                    await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationToken);
            }
            finally { _sendLock.Release(); }
        }
        catch (OperationCanceledException) { }
        catch (Exception error) { Plugin.Log.LogDebug("Gateway send failed: " + error.Message); }
    }

    [Serializable] private sealed class SessionStateMessage
    {
        public string type = "";
        public bool run_active;
        public string lobby_id = "";
        public bool is_master_client;
        public int photon_id;
    }

    [Serializable] private sealed class SpawnAckMessage
    {
        public string type = "";
        public string request_id = "";
        public string category = "";
        public bool success;
        public string item_name = "";
        public string detail = "";
    }

    [Serializable] private sealed class DiagnosticsSnapshotMessage
    {
        public string type = "";
        public string mod_version = "";
        public string lobby_id = "";
        public int local_actor_number;
        public int master_actor_number;
        public bool local_avatar_available;
        public int photon_player_count;
        public PhotonPlayerSnapshot[] photon_players = Array.Empty<PhotonPlayerSnapshot>();
        public AvatarOwnerSnapshot[] avatar_owners = Array.Empty<AvatarOwnerSnapshot>();
    }

    [Serializable] private sealed class PhotonPlayerSnapshot
    {
        public int actor_number;
        public string user_id = "";
        public string nickname = "";
    }

    [Serializable] private sealed class AvatarOwnerSnapshot
    {
        public int owner_actor_number;
        public int photon_view_id;
    }

    [Serializable] private sealed class CatalogueMessage
    {
        public string type = "";
        public string mod_version = "";
        public RepoCatalogueEntry[] entries = Array.Empty<RepoCatalogueEntry>();
        public CatalogueSession session = new();
    }

    [Serializable] private sealed class CatalogueSession
    {
        public bool run_active;
        public string lobby_id = "";
        public bool is_master_client;
    }

    [Serializable] private sealed class RepoCatalogueEntry
    {
        public string category = "";
        public string id = "";
        public string name = "";
        public bool eligible;
        public string reason = "";
    }

    [Serializable] private sealed class GatewayMessage
    {
        public string type = "";
        public bool ok;
        public string request_id = "";
        public string category = "";
        public int target_photon_id;
        public RepoConfigPayload? config;
    }

    [Serializable] private sealed class RepoConfigPayload
    {
        public RepoCategorySettings? categories;
    }

    [Serializable] private sealed class RepoCategorySettings
    {
        public RepoCategory? monster;
        public RepoCategory? utility_item;
        public RepoCategory? valuable;
    }

    [Serializable] private sealed class RepoCategory
    {
        public bool enabled = true;
        public double weight = 1d;
    }
}
