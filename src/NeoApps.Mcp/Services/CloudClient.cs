using System.Net;
using System.Net.Http.Headers;
using System.Text.Json.Nodes;

namespace NeoApps.Mcp.Services;

/// <summary>Outcome of a cloud deploy: either live (Url) or waiting for the customer to pay (CheckoutUrl).</summary>
public sealed record CloudDeployResult(string Slug, string Region, string? Url, string? CheckoutUrl, string? AdminUsername, string? AdminPassword, string? Message);

/// <summary>
/// Talks to the NeoApps Cloud control plane (NEOAPPS_CLOUD_URL, default https://cloud.neoapps.ai):
/// register the app in a region, upload the bundle to a one-time link, start the cloud.deploy job and
/// follow its events until it ends. The deploy token comes from NEOAPPS_CLOUD_TOKEN or the OS keychain.
/// </summary>
public sealed class CloudClient : IDisposable
{
    public const string KeychainService = "neoapps-cloud";
    public static readonly string[] Regions = ["us", "ca"];
    private static readonly HashSet<string> Terminal = ["completed", "failed", "cancelled", "rejected"];

    private readonly HttpClient _http;
    public Uri BaseUrl { get; }

    public CloudClient(string? token = null, string? baseUrl = null, HttpMessageHandler? handler = null)
    {
        BaseUrl = new Uri((baseUrl ?? Environment.GetEnvironmentVariable("NEOAPPS_CLOUD_URL") ?? "https://cloud.neoapps.ai").TrimEnd('/') + "/");
        token ??= ReadToken() ?? throw new InvalidOperationException("Not signed in to NeoApps Cloud. Run: neoapps cloud login");
        _http = handler is null ? new HttpClient() : new HttpClient(handler);
        _http.BaseAddress = BaseUrl;
        _http.Timeout = TimeSpan.FromMinutes(10);
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        _http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("neoapps-cli", "1"));
    }

    public async Task<CloudDeployResult> DeployAsync(ProjectState p, string region, string bundle, Action<string> log, CancellationToken ct)
    {
        var slug = CloudBundle.Slug(p.Name);
        await StartSessionAsync(ct);
        log($"Registering {slug} in region '{region}'");
        var app = await SendAsync(HttpMethod.Post, "v1/api/cloud/apps", new JsonObject { ["app"] = p.Name, ["slug"] = slug, ["region"] = region }, ct);
        if (app["checkoutUrl"]?.GetValue<string>() is { Length: > 0 } checkout)
            return new(slug, region, null, checkout, null, null,
                "This account has no active plan. Open the checkout link, pay, then deploy again.");

        var upload = await SendAsync(HttpMethod.Post, $"v1/api/cloud/apps/{slug}/upload", null, ct);
        var uploadUrl = upload["uploadUrl"]?.GetValue<string>() ?? throw new InvalidOperationException("The cloud did not return an upload link.");
        var blob = upload["blob"]?.GetValue<string>() ?? throw new InvalidOperationException("The cloud did not return a blob name.");

        log($"Uploading {new FileInfo(bundle).Length / 1024} KB");
        await using (var fs = File.OpenRead(bundle))
        {
            using var put = new HttpRequestMessage(HttpMethod.Put, uploadUrl) { Content = new StreamContent(fs) };
            put.Content.Headers.ContentType = new MediaTypeHeaderValue("application/gzip");
            put.Headers.Add("x-ms-blob-type", "BlockBlob");
            using var bare = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
            using var res = await (uploadUrl.StartsWith(BaseUrl.ToString(), StringComparison.OrdinalIgnoreCase) ? _http : bare).SendAsync(put, ct);
            if (!res.IsSuccessStatusCode) throw new InvalidOperationException($"Upload failed: {(int)res.StatusCode} {res.ReasonPhrase}");
        }

        var job = await SendAsync(HttpMethod.Post, "v1/api/Jobs",
            new JsonObject { ["kind"] = "cloud.deploy", ["input"] = new JsonObject { ["slug"] = slug, ["blob"] = blob } }, ct);
        var id = job["job_id"]?.GetValue<string>() ?? throw new InvalidOperationException("The cloud did not start a deploy job.");
        log($"Deploy job {id} started");

        long after = 0;
        while (true)
        {
            var page = await SendAsync(HttpMethod.Get, $"v1/api/Jobs/{id}/events?after={after}&wait=20", null, ct);
            foreach (var e in page["events"]?.AsArray() ?? [])
            {
                after = Math.Max(after, e?["id"]?.GetValue<long>() ?? after);
                if (e?["message"]?.GetValue<string>() is { Length: > 0 } m) log(m);
            }
            var status = page["job"]?["status"]?.GetValue<string>() ?? "";
            if (!Terminal.Contains(status)) continue;
            if (status != "completed")
                throw new InvalidOperationException($"Deploy {status}: {page["job"]?["error"]?.GetValue<string>() ?? "see the job in the dashboard"}");
            var result = page["job"]?["result"] as JsonObject ?? [];
            return new(slug, region, result["url"]?.GetValue<string>(), null,
                result["adminUsername"]?.GetValue<string>(), result["adminPassword"]?.GetValue<string>(), null);
        }
    }

    /// <summary>Sets or removes settings of a live app (values go straight to its host; names come back, never values).</summary>
    public async Task<string[]> SetEnvAsync(string slug, IDictionary<string, string> set, IEnumerable<string> unset, CancellationToken ct)
    {
        await StartSessionAsync(ct);
        var body = new JsonObject
        {
            ["set"] = new JsonObject(set.Select(kv => KeyValuePair.Create(kv.Key, (JsonNode?)JsonValue.Create(kv.Value)))),
            ["unset"] = new JsonArray(unset.Select(u => (JsonNode?)JsonValue.Create(u)).ToArray()),
        };
        var res = await SendAsync(HttpMethod.Put, $"v1/api/cloud/apps/{slug}/env", body, ct);
        return res["settings"]?.AsArray().Select(n => n!.GetValue<string>()).ToArray() ?? [];
    }

    /// <summary>New password for the cloud app's first administrator (unlocks it, ends its sessions). Returns (username, password).</summary>
    public async Task<(string Username, string Password)> ResetAdminAsync(string slug, CancellationToken ct)
    {
        await StartSessionAsync(ct);
        var res = await SendAsync(HttpMethod.Post, $"v1/api/cloud/apps/{slug}/reset-admin", new JsonObject(), ct);
        return (res["username"]!.GetValue<string>(), res["password"]!.GetValue<string>());
    }

    private bool _session;
    private async Task StartSessionAsync(CancellationToken ct)
    {
        if (_session) return;
        // the deploy token is only good for this exchange; everything else uses the one-hour session it returns
        var session = await SendAsync(HttpMethod.Post, "v1/api/cloud/session", null, ct);
        var jwt = session["accessToken"]?.GetValue<string>() ?? throw new InvalidOperationException("NeoApps Cloud did not return a session.");
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        _session = true;
    }

    private async Task<JsonObject> SendAsync(HttpMethod method, string path, JsonObject? body, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(method, path)
        {
            Content = body is null ? null : new StringContent(body.ToJsonString(), System.Text.Encoding.UTF8, "application/json"),
        };
        using var res = await _http.SendAsync(req, ct);
        var text = await res.Content.ReadAsStringAsync(ct);
        JsonObject? json = null;
        try { json = text.Length > 0 ? JsonNode.Parse(text) as JsonObject : null; } catch (System.Text.Json.JsonException) { }
        if (res.IsSuccessStatusCode) return json ?? [];
        var message = json?["message"]?.GetValue<string>() ?? res.ReasonPhrase;
        throw new InvalidOperationException(res.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "The deploy token was refused. Create a new one at https://cloud.neoapps.ai, then run: neoapps cloud login",
            HttpStatusCode.Forbidden => "Not allowed: " + message,
            _ => $"NeoApps Cloud: {(int)res.StatusCode} {message}",
        });
    }

    public void Dispose() => _http.Dispose();

    /// <summary>
    /// Browser sign-in: asks the control plane for a short code, opens the connect page (sign up or sign in,
    /// choose a plan if needed, approve this computer), then polls until it hands out a deploy token.
    /// The token is saved to the keychain when there is one and kept for this process either way.
    /// </summary>
    public static async Task<string> DeviceLoginAsync(Action<string> log, CancellationToken ct, string? baseUrl = null, HttpMessageHandler? handler = null, bool openBrowser = true)
    {
        var root = new Uri((baseUrl ?? Environment.GetEnvironmentVariable("NEOAPPS_CLOUD_URL") ?? "https://cloud.neoapps.ai").TrimEnd('/') + "/");
        using var http = handler is null ? new HttpClient() : new HttpClient(handler);
        http.BaseAddress = root;
        http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("neoapps-cli", "1"));
        var device = $"{Environment.UserName} on {Environment.MachineName}";
        using var start = await http.PostAsync("v1/api/cloud/device/start", Json(new JsonObject { ["device"] = device }), ct);
        var s = await ReadJsonAsync(start, ct);
        if (!start.IsSuccessStatusCode) throw new InvalidOperationException(s?["message"]?.ToString() ?? $"Sign-in failed ({(int)start.StatusCode}).");
        string userCode = s!["userCode"]!.ToString(), deviceCode = s["deviceCode"]!.ToString(), url = s["verifyUrl"]!.ToString();
        var interval = TimeSpan.FromSeconds(Math.Max(2, s["interval"]?.GetValue<int>() ?? 3));
        var until = DateTimeOffset.UtcNow.AddSeconds(s["expiresIn"]?.GetValue<int>() ?? 900);
        log($"Sign in to NeoApps Cloud: open {url} and check the code is {userCode}.");
        if (openBrowser) OpenBrowser(url);
        while (DateTimeOffset.UtcNow < until)
        {
            await Task.Delay(interval, ct);
            using var poll = await http.PostAsync("v1/api/cloud/device/poll", Json(new JsonObject { ["deviceCode"] = deviceCode }), ct);
            if (poll.StatusCode == HttpStatusCode.Accepted) continue;
            var p = await ReadJsonAsync(poll, ct);
            if (!poll.IsSuccessStatusCode) throw new InvalidOperationException(p?["message"]?.ToString() ?? "Sign-in failed; start again.");
            var token = p!["token"]!.ToString();
            Environment.SetEnvironmentVariable("NEOAPPS_CLOUD_TOKEN", token); // this process (MCP server) keeps working without a keychain
            try { SaveToken(token); log("Signed in. The token is in your keychain."); }
            catch (InvalidOperationException) { log("Signed in for this session (no keychain here)."); }
            return token;
        }
        throw new TimeoutException("The sign-in code expired. Start again.");
    }

    /// <summary>False when the saved token was revoked or expired (it is then removed), so the caller signs in again.</summary>
    public static async Task<bool> TokenWorksAsync(CancellationToken ct)
    {
        using var c = new CloudClient();
        using var res = await c._http.PostAsync("v1/api/cloud/session", null, ct);
        if (res.StatusCode != HttpStatusCode.Unauthorized) return true; // other failures surface in the deploy itself
        DeleteToken();
        Environment.SetEnvironmentVariable("NEOAPPS_CLOUD_TOKEN", null);
        return false;
    }

    private static StringContent Json(JsonObject o) => new(o.ToJsonString(), System.Text.Encoding.UTF8, "application/json");

    private static async Task<JsonObject?> ReadJsonAsync(HttpResponseMessage res, CancellationToken ct)
    {
        try { return JsonNode.Parse(await res.Content.ReadAsStringAsync(ct)) as JsonObject; } catch (System.Text.Json.JsonException) { return null; }
    }

    public static void OpenBrowser(string url)
    {
        if (!url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) && !url.StartsWith("http://127.0.0.1", StringComparison.Ordinal) && !url.StartsWith("http://localhost", StringComparison.Ordinal)) return;
        try
        {
            var psi = OperatingSystem.IsWindows() ? new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true }
                : new System.Diagnostics.ProcessStartInfo(OperatingSystem.IsMacOS() ? "open" : "xdg-open", [url]);
            psi.RedirectStandardOutput = !OperatingSystem.IsWindows(); psi.RedirectStandardError = !OperatingSystem.IsWindows();
            System.Diagnostics.Process.Start(psi);
        }
        catch (System.ComponentModel.Win32Exception) { } // no browser (CI, SSH): the URL is in the log
    }

    // ---- deploy token: NEOAPPS_CLOUD_TOKEN, else the macOS keychain or the Linux secret service; never a file ----

    public static string? ReadToken()
    {
        if (Environment.GetEnvironmentVariable("NEOAPPS_CLOUD_TOKEN") is { Length: > 0 } env) return env;
        var r = OperatingSystem.IsMacOS()
            ? Run("security", ["find-generic-password", "-a", Environment.UserName, "-s", KeychainService, "-w"], null)
            : OperatingSystem.IsLinux() ? Run("secret-tool", ["lookup", "service", KeychainService], null) : null;
        return r is { Ok: true } && r.Value.Output.Trim() is { Length: > 0 } t ? t : null;
    }

    public static void SaveToken(string token)
    {
        if (!token.StartsWith("nac_", StringComparison.Ordinal)) throw new ArgumentException("A NeoApps Cloud deploy token starts with nac_.");
        var r = OperatingSystem.IsMacOS()
            ? Run("security", ["add-generic-password", "-U", "-a", Environment.UserName, "-s", KeychainService, "-l", "NeoApps Cloud deploy token", "-w", token], null)
            : OperatingSystem.IsLinux() ? Run("secret-tool", ["store", "--label=NeoApps Cloud deploy token", "service", KeychainService], token) : null;
        if (r is not { Ok: true })
            throw new InvalidOperationException("No keychain available here. Set NEOAPPS_CLOUD_TOKEN in the environment instead.");
    }

    public static bool DeleteToken()
    {
        var r = OperatingSystem.IsMacOS()
            ? Run("security", ["delete-generic-password", "-a", Environment.UserName, "-s", KeychainService], null)
            : OperatingSystem.IsLinux() ? Run("secret-tool", ["clear", "service", KeychainService], null) : null;
        return r is { Ok: true };
    }

    private static (bool Ok, string Output)? Run(string file, string[] args, string? stdin)
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo(file) { RedirectStandardOutput = true, RedirectStandardError = true, RedirectStandardInput = stdin != null };
            foreach (var a in args) psi.ArgumentList.Add(a);
            using var p = System.Diagnostics.Process.Start(psi)!;
            if (stdin != null) { p.StandardInput.Write(stdin); p.StandardInput.Close(); }
            var output = p.StandardOutput.ReadToEnd();
            p.WaitForExit(15000);
            return (p.ExitCode == 0, output);
        }
        catch (System.ComponentModel.Win32Exception) { return null; }
    }
}
