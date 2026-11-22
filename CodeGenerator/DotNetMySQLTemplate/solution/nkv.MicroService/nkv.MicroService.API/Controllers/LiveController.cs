using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using nkv.MicroService.Utility;

namespace nkv.MicroService.API.Controllers
{
    /// <summary>
    /// Live rooms: a WebSocket that relays small messages between everyone in the same room, for multiplayer games, live
    /// cursors, presence, chat or instant refresh. Nothing is stored. Rooms are declared in ui.config.json:
    ///   "live": { "rooms": { "arena": "public", "team": "members" }, "maxPeers": 16 }
    /// "public" = anyone (no sign-in, per-IP connection limit); "members" = people signed in to this app. A room name may
    /// carry a suffix for separate matches: "arena-7f3k" joins room "arena", match "7f3k".
    /// Protocol (JSON text frames):
    ///   server → you on join:  {"t":"welcome","id":"p12","peers":[{"id":"p3","name":"Ana"}]}
    ///   server → others:       {"t":"join","id":"p12","name":"Ben"}  /  {"t":"leave","id":"p12"}
    ///   you → server:          any JSON value up to 4 KB (at most 60 a second; extra ones are dropped)
    ///   server → others:       {"t":"msg","from":"p12","d":&lt;your JSON&gt;,"at":&lt;server ms&gt;}
    /// Rooms live in this API process, so an app with live rooms runs a single copy (NeoApps Cloud sets that).
    /// </summary>
    [ApiController]
    public class LiveController : ControllerBase
    {
        const int MaxMessageBytes = 4096, MessagesPerSecond = 60, MaxRoomsPerApp = 200, MaxConnectionsPerIp = 8;
        static readonly Regex RoomName = new(@"^([a-z0-9_]{1,30})(?:-([a-z0-9]{1,16}))?$", RegexOptions.Compiled);
        static readonly ConcurrentDictionary<string, Room> Rooms = new();
        static readonly ConcurrentDictionary<string, int> ConnectionsPerIp = new();
        static long _nextId;

        readonly string[] _allowedOrigins;
        public LiveController(IConfiguration config)
        {
            _allowedOrigins = (config["Cors:AllowedOrigins"] ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        sealed class Peer
        {
            public string Id, Name; public WebSocket Socket; public readonly SemaphoreSlim SendLock = new(1, 1);
            public double Tokens = MessagesPerSecond; public long LastRefill = Environment.TickCount64;
        }
        sealed class Room { public readonly ConcurrentDictionary<string, Peer> Peers = new(); }

        static string Access(string baseName)
        {
            var a = DataGuard.Config["live"]?["rooms"]?[baseName]?.ToString();
            return a is "public" or "members" ? a : null;
        }
        static int MaxPeers() =>
            DataGuard.Config["live"]?["maxPeers"] is JsonValue v && v.TryGetValue<int>(out var n) ? Math.Clamp(n, 2, 50) : 16;

        /// <summary>Browsers send cookies with WebSocket handshakes from any site: only this app's own pages may connect.</summary>
        bool OriginAllowed()
        {
            var origin = Request.Headers.Origin.ToString();
            if (string.IsNullOrEmpty(origin)) return true; // not a browser (tests, servers): no cookie risk without one
            if (!Uri.TryCreate(origin, UriKind.Absolute, out var o)) return false;
            if (string.Equals(o.Authority, Request.Host.Value, StringComparison.OrdinalIgnoreCase)) return true;
            return _allowedOrigins.Any(a => a == "*" || string.Equals(a.TrimEnd('/'), origin.TrimEnd('/'), StringComparison.OrdinalIgnoreCase));
        }

        [AllowAnonymous, HttpGet, Route(APIEndpoint.Version + "/" + APIEndpoint.APIRoute + "/live/{room}")]
        public async Task Connect(string room, string name = null)
        {
            var m = RoomName.Match(room ?? "");
            var access = m.Success ? Access(m.Groups[1].Value) : null;
            if (access == null) { Response.StatusCode = 404; return; }
            if (!HttpContext.WebSockets.IsWebSocketRequest) { Response.StatusCode = 400; await Response.WriteAsync("Connect with a WebSocket."); return; }
            if (!OriginAllowed()) { Response.StatusCode = 403; return; }
            var member = User?.Identity?.IsAuthenticated == true ? User.FindFirst(ClaimTypes.Name)?.Value : null;
            if (access == "members" && member == null) { Response.StatusCode = 401; return; }

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "?";
            if (ConnectionsPerIp.AddOrUpdate(ip, 1, (_, c) => c + 1) > MaxConnectionsPerIp)
            {
                ConnectionsPerIp.AddOrUpdate(ip, 0, (_, c) => Math.Max(0, c - 1));
                Response.StatusCode = 429; return;
            }
            try
            {
                if (!Rooms.ContainsKey(room) && Rooms.Count >= MaxRoomsPerApp) { Response.StatusCode = 503; return; }
                var r = Rooms.GetOrAdd(room, _ => new Room());
                if (r.Peers.Count >= MaxPeers()) { Response.StatusCode = 409; await Response.WriteAsync("This room is full."); return; }

                using var socket = await HttpContext.WebSockets.AcceptWebSocketAsync(new WebSocketAcceptContext { KeepAliveInterval = TimeSpan.FromSeconds(20) });
                var display = Clean(member ?? name) ?? "Guest";
                var peer = new Peer { Id = "p" + Interlocked.Increment(ref _nextId), Name = display, Socket = socket };
                var others = r.Peers.Values.Select(p => new { id = p.Id, name = p.Name }).ToArray();
                r.Peers[peer.Id] = peer;
                await Send(peer, JsonSerializer.Serialize(new { t = "welcome", id = peer.Id, name = display, peers = others }), important: true);
                await Broadcast(r, peer, JsonSerializer.Serialize(new { t = "join", id = peer.Id, name = display }), important: true);
                try { await Receive(r, peer, HttpContext.RequestAborted); }
                finally
                {
                    r.Peers.TryRemove(peer.Id, out _);
                    await Broadcast(r, peer, JsonSerializer.Serialize(new { t = "leave", id = peer.Id }), important: true);
                    if (r.Peers.IsEmpty) Rooms.TryRemove(new(room, r));
                }
            }
            finally { ConnectionsPerIp.AddOrUpdate(ip, 0, (_, c) => Math.Max(0, c - 1)); }
        }

        static string Clean(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            s = Regex.Replace(s, @"[\p{C}<>]+", "").Trim();
            return s.Length == 0 ? null : s.Length > 24 ? s[..24] : s;
        }

        static async Task Receive(Room room, Peer peer, CancellationToken ct)
        {
            var buffer = new byte[MaxMessageBytes + 1];
            while (peer.Socket.State == WebSocketState.Open && !ct.IsCancellationRequested)
            {
                int count = 0; WebSocketReceiveResult res;
                do
                {
                    res = await peer.Socket.ReceiveAsync(new ArraySegment<byte>(buffer, Math.Min(count, MaxMessageBytes), buffer.Length - Math.Min(count, MaxMessageBytes)), ct);
                    if (res.MessageType == WebSocketMessageType.Close) { await peer.Socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None); return; }
                    count += res.Count;
                } while (!res.EndOfMessage);
                if (count > MaxMessageBytes || res.MessageType != WebSocketMessageType.Text) continue; // too big or binary: dropped
                // at most MessagesPerSecond per peer (token bucket); extra messages are dropped, not queued
                var now = Environment.TickCount64;
                peer.Tokens = Math.Min(MessagesPerSecond, peer.Tokens + (now - peer.LastRefill) * MessagesPerSecond / 1000.0);
                peer.LastRefill = now;
                if (peer.Tokens < 1) continue;
                peer.Tokens -= 1;
                JsonNode data;
                try { data = JsonNode.Parse(Encoding.UTF8.GetString(buffer, 0, count)); } catch (JsonException) { continue; }
                var msg = new JsonObject { ["t"] = "msg", ["from"] = peer.Id, ["d"] = data, ["at"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() };
                await Broadcast(room, peer, msg.ToJsonString());
            }
        }

        static async Task Broadcast(Room room, Peer from, string text, bool important = false)
        {
            foreach (var p in room.Peers.Values)
                if (p.Id != from.Id) await Send(p, text, important);
        }

        /// <summary>Messages are latest-wins: a slow receiver skips one instead of holding up the room. Joins, leaves and
        /// the welcome (important) wait their turn.</summary>
        static async Task Send(Peer p, string text, bool important = false)
        {
            if (p.Socket.State != WebSocketState.Open) return;
            if (!await p.SendLock.WaitAsync(important ? 5000 : 0)) return;
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await p.Socket.SendAsync(Encoding.UTF8.GetBytes(text), WebSocketMessageType.Text, true, cts.Token);
            }
            catch (Exception) { /* closed or stuck: the receive loop ends and the peer leaves */ }
            finally { p.SendLock.Release(); }
        }
    }
}
