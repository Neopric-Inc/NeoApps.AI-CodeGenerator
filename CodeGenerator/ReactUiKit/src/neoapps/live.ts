// Live rooms: a WebSocket to /v1/api/live/{room} that relays small JSON messages between everyone in the room
// (multiplayer games, presence, live cursors, chat). Rooms are declared in ui.config.json "live.rooms".
//
//   const room = joinRoom("arena-" + matchCode, {
//     name: "Ana",
//     onWelcome: (me, peers) => ...,          // your id and who is already here
//     onJoin: (peer) => ..., onLeave: (id) => ...,
//     onMessage: (from, data, at) => ...,     // a message another player sent
//     onStatus: (s) => ...,                   // "connecting" | "open" | "closed"
//   });
//   room.send({ x, y, z });                   // up to 4 KB, at most 60 a second (extra ones are dropped)
//   room.close();
//
// It reconnects by itself (with a growing pause) until close() is called; after a reconnect you get a new id.

import { apiBase } from "./api";

export interface LivePeer { id: string; name: string }
export type LiveStatus = "connecting" | "open" | "closed";

export interface LiveHandlers {
  name?: string;
  onWelcome?: (me: LivePeer, peers: LivePeer[]) => void;
  onJoin?: (peer: LivePeer) => void;
  onLeave?: (id: string) => void;
  onMessage?: (from: string, data: any, at: number) => void;
  onStatus?: (status: LiveStatus) => void;
}

export interface LiveRoom {
  send: (data: any) => boolean;
  close: () => void;
  readonly id: string | null;
  readonly status: LiveStatus;
}

/** The WebSocket address of a room, on the API's origin (the app's own origin when deployed). */
export function liveUrl(room: string, name?: string): string {
  const base = new URL(apiBase(), window.location.href);
  base.protocol = base.protocol === "https:" ? "wss:" : "ws:";
  const url = new URL("live/" + encodeURIComponent(room), base);
  if (name) url.searchParams.set("name", name);
  return url.toString();
}

export function joinRoom(room: string, h: LiveHandlers = {}): LiveRoom {
  let ws: WebSocket | null = null;
  let id: string | null = null;
  let status: LiveStatus = "connecting";
  let stopped = false;
  let retry = 0;
  let timer: ReturnType<typeof setTimeout> | undefined;

  const setStatus = (s: LiveStatus) => { status = s; h.onStatus?.(s); };

  const connect = () => {
    setStatus("connecting");
    ws = new WebSocket(liveUrl(room, h.name));
    ws.onopen = () => { retry = 0; setStatus("open"); };
    ws.onmessage = (ev) => {
      let m: any;
      try { m = JSON.parse(ev.data); } catch { return; }
      if (m.t === "welcome") { id = m.id; h.onWelcome?.({ id: m.id, name: m.name }, m.peers || []); }
      else if (m.t === "join") h.onJoin?.({ id: m.id, name: m.name });
      else if (m.t === "leave") h.onLeave?.(m.id);
      else if (m.t === "msg") h.onMessage?.(m.from, m.d, m.at);
    };
    ws.onclose = () => {
      id = null;
      setStatus("closed");
      if (stopped) return;
      // reconnect: 0.5 s, 1 s, 2 s ... up to 10 s
      const wait = Math.min(10000, 500 * 2 ** retry++);
      timer = setTimeout(connect, wait);
    };
  };
  connect();

  return {
    send: (data: any) => {
      if (!ws || ws.readyState !== WebSocket.OPEN) return false;
      ws.send(JSON.stringify(data));
      return true;
    },
    close: () => { stopped = true; clearTimeout(timer); ws?.close(); },
    get id() { return id; },
    get status() { return status; },
  };
}
