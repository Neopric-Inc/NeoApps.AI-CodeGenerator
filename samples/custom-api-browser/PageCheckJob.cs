using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using nkv.MicroService.API.Jobs;

namespace nkv.MicroService.API.Custom
{
    /// <summary>
    /// Example browser job ("browser.page-check"): opens a page in headless Chromium, records a video, takes a full-page
    /// screenshot, and reports console errors and failed network requests. Everything shows up in the job's live feed.
    /// Copy this folder (both files) to workspace/&lt;App&gt;/custom-api/ and regenerate.
    ///
    /// Start it: POST /v1/api/Jobs {"kind": "browser.page-check", "input": {"url": "http://localhost:3100"}}
    /// The server's browser can reach whatever the server can, so only hosts listed in Browser:AllowedHosts
    /// (comma-separated, default "localhost,127.0.0.1") may be opened.
    /// </summary>
    public sealed class PageCheckJob : JobHandler
    {
        private readonly IConfiguration _configuration;

        public PageCheckJob(IConfiguration configuration) => _configuration = configuration;

        public override string Kind => "browser.page-check";

        private Uri Target(JsonObject input)
        {
            if (!Uri.TryCreate(input["url"]?.ToString(), UriKind.Absolute, out var url) || (url.Scheme != "http" && url.Scheme != "https"))
                throw new JobException("Give an http(s) address to check (input.url).");
            var allowed = (_configuration["Browser:AllowedHosts"] ?? "localhost,127.0.0.1")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (!allowed.Contains(url.Host, StringComparer.OrdinalIgnoreCase))
                throw new JobException($"{url.Host} isn't in Browser:AllowedHosts.");
            return url;
        }

        public override string Validate(JobUser user, JsonObject input) => "Check " + Target(input);

        public override async Task RunAsync(JobContext ctx)
        {
            var url = Target(ctx.Input);
            var consoleErrors = new List<string>();
            var failedRequests = new List<string>();

            ctx.Progress("Starting the browser", 5);
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
            var videoDir = Path.Combine(ctx.TempDir, "video");
            var context = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = 1280, Height = 800 },
                RecordVideoDir = videoDir,
                RecordVideoSize = new() { Width = 1280, Height = 800 },
            });
            var page = await context.NewPageAsync();
            page.Console += (_, m) => { if (m.Type == "error") consoleErrors.Add(m.Text); };
            page.PageError += (_, e) => consoleErrors.Add(e);
            page.Response += (_, r) => { if (r.Status >= 400) failedRequests.Add($"{r.Status} {r.Request.Method} {r.Url}"); };
            page.RequestFailed += (_, r) => failedRequests.Add($"failed {r.Method} {r.Url}: {r.Failure}");

            ctx.Progress($"Opening {url}", 25);
            var started = DateTime.UtcNow;
            var response = await page.GotoAsync(url.ToString(), new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 30000 });
            var loadMs = (int)(DateTime.UtcNow - started).TotalMilliseconds;
            ctx.Cancel.ThrowIfCancellationRequested();
            var title = await page.TitleAsync();
            ctx.Progress($"Loaded \"{title}\" in {loadMs} ms (HTTP {response?.Status})", 60);

            var shot = await page.ScreenshotAsync(new() { FullPage = true });
            ctx.AttachFile("page.png", "image/png", shot, "Screenshot of " + url);

            var recording = page.Video;
            await context.CloseAsync(); // finishes the video file
            var video = recording != null ? await recording.PathAsync() : null;
            if (video != null && File.Exists(video)) ctx.AttachFile(video, "video/webm", "Recording of the visit");
            else ctx.Warn("No video was recorded (the browser's ffmpeg may be missing).");

            foreach (var e in consoleErrors.Take(10)) ctx.Warn("Console error: " + e);
            foreach (var f in failedRequests.Take(10)) ctx.Warn("Request: " + f);
            var ok = (response?.Ok ?? false) && consoleErrors.Count == 0 && failedRequests.Count == 0;
            ctx.Complete(new JsonObject
            {
                ["url"] = url.ToString(),
                ["title"] = title,
                ["status"] = response?.Status,
                ["load_ms"] = loadMs,
                ["console_errors"] = new JsonArray(consoleErrors.Select(e => (JsonNode)JsonValue.Create(e)).ToArray()),
                ["failed_requests"] = new JsonArray(failedRequests.Select(e => (JsonNode)JsonValue.Create(e)).ToArray()),
                ["passed"] = ok,
            }, ok ? "Page check passed." : $"Page check found {consoleErrors.Count} console error(s) and {failedRequests.Count} failed request(s).");
        }
    }
}
