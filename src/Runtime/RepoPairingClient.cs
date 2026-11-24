using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace SpectrumRepoMod.Runtime;

internal sealed class RepoPairingClient : IDisposable
{
    private const string GameModsUrl = "https://spectrum-game-mods.rydrakeandlonk.workers.dev";
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(15) };

    public async Task<PairingStart> StartAsync(CancellationToken cancellationToken)
    {
        using var response = await _http.PostAsync(
            GameModsUrl + "/connect/repo/pairing/start",
            new StringContent("{}", Encoding.UTF8, "application/json"),
            cancellationToken);
        var json = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(ReadError(json, "Could not start R.E.P.O. pairing."));

        var result = JsonUtility.FromJson<PairingStart>(json);
        if (result == null || string.IsNullOrWhiteSpace(result.device_code) ||
            string.IsNullOrWhiteSpace(result.user_code) || string.IsNullOrWhiteSpace(result.verification_uri))
            throw new InvalidOperationException("The pairing service returned an incomplete response.");
        return result;
    }

    public async Task<PairingResult> WaitForApprovalAsync(PairingStart pairing, CancellationToken cancellationToken)
    {
        var expiresAt = DateTime.UtcNow.AddSeconds(Math.Max(30, pairing.expires_in));
        var interval = Math.Clamp(pairing.interval, 3, 15);
        while (DateTime.UtcNow < expiresAt)
        {
            await Task.Delay(TimeSpan.FromSeconds(interval), cancellationToken);
            var request = new PairingPollRequest { user_code = pairing.user_code, device_code = pairing.device_code };
            using var response = await _http.PostAsync(
                GameModsUrl + "/connect/repo/pairing/poll",
                new StringContent(JsonUtility.ToJson(request), Encoding.UTF8, "application/json"),
                cancellationToken);
            var json = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException(ReadError(json, "Could not check R.E.P.O. pairing status."));

            var status = JsonUtility.FromJson<PairingPollResponse>(json);
            if (status == null) throw new InvalidOperationException("The pairing service returned an invalid status.");
            if (status.status == "authorized")
            {
                if (string.IsNullOrWhiteSpace(status.broadcaster_user_id) || string.IsNullOrWhiteSpace(status.host_token))
                    throw new InvalidOperationException("The approved pairing did not include valid host credentials.");
                return new PairingResult(status.broadcaster_user_id, status.broadcaster_login, status.host_token);
            }
            if (status.status == "expired" || status.status == "consumed")
                throw new TimeoutException("The pairing code expired. Start a new connection request.");
        }
        throw new TimeoutException("The pairing request expired before it was approved.");
    }

    private static string ReadError(string json, string fallback)
    {
        try
        {
            var error = JsonUtility.FromJson<PairingError>(json);
            return string.IsNullOrWhiteSpace(error?.error) ? fallback : error.error;
        }
        catch { return fallback; }
    }

    public void Dispose() => _http.Dispose();

    [Serializable] internal sealed class PairingStart
    {
        public string device_code = "";
        public string user_code = "";
        public string verification_uri = "";
        public int expires_in = 600;
        public int interval = 5;
    }

    [Serializable] private sealed class PairingPollRequest
    {
        public string user_code = "";
        public string device_code = "";
    }

    [Serializable] private sealed class PairingPollResponse
    {
        public string status = "";
        public string broadcaster_user_id = "";
        public string broadcaster_login = "";
        public string host_token = "";
    }

    [Serializable] private sealed class PairingError { public string error = ""; }

    internal sealed class PairingResult
    {
        public string BroadcasterUserId { get; }
        public string BroadcasterLogin { get; }
        public string HostToken { get; }
        public PairingResult(string broadcasterUserId, string broadcasterLogin, string hostToken)
        {
            BroadcasterUserId = broadcasterUserId;
            BroadcasterLogin = broadcasterLogin;
            HostToken = hostToken;
        }
    }
}
