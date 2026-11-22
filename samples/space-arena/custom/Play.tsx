// SpaceArena flight screen: hangar (customize ship) → arena (fly, fight) with live multiplayer and procedural audio.
import React, { useEffect, useRef, useState } from "react";
import { joinRoom, LiveRoom } from "../neoapps/live";
import { Engine, COLORS, HULLS, Hull, ShipLook, ShipState, MAX_HP } from "./engine";
import { GameAudio } from "./audio";

const SEND_MS = 50;
const KEY = "spacearena.look";

interface Peer { id: string; look: ShipLook; kills: number; deaths: number; last: number; gaps: number[] }

function loadLook(): ShipLook {
  try { const v = JSON.parse(localStorage.getItem(KEY) || ""); if (v && v.name) return v; } catch { /* ignore */ }
  return { name: "Pilot" + Math.floor(Math.random() * 900 + 100), color: COLORS[Math.floor(Math.random() * COLORS.length)], hull: "dart" };
}
function saveLook(l: ShipLook) { try { localStorage.setItem(KEY, JSON.stringify(l)); } catch { /* ignore */ } }
const cleanCode = (s: string) => s.toLowerCase().replace(/[^a-z0-9]/g, "").slice(0, 16) || "lobby";

export const Play: React.FC = () => {
  const params = new URLSearchParams(window.location.search);
  const [look, setLook] = useState<ShipLook>(loadLook);
  const [code, setCode] = useState(cleanCode(params.get("match") || "lobby"));
  const [flying, setFlying] = useState(false);
  if (!flying) return <Hangar look={look} setLook={setLook} code={code} setCode={setCode} onLaunch={() => { saveLook(look); setFlying(true); }} />;
  return <Arena look={look} code={code} onExit={() => setFlying(false)} />;
};

// ------------------------------------------------------------------ hangar

const Hangar: React.FC<{ look: ShipLook; setLook: (l: ShipLook) => void; code: string; setCode: (c: string) => void; onLaunch: () => void }> =
  ({ look, setLook, code, setCode, onLaunch }) => {
  const preview = useRef<HTMLCanvasElement>(null);
  useEffect(() => {
    if (!preview.current) return;
    const e = new Engine(preview.current, look, "hangar");
    e.controls.yaw = 0.35; e.controls.throttle = 0; (e as any).hangar = true;
    e.start(); return () => e.stop();
  }, [look]);
  const btn = (active: boolean): React.CSSProperties => ({ padding: "8px 14px", borderRadius: 8, border: active ? "2px solid #fff" : "1px solid #334", background: active ? "#1e293b" : "#0b1220", color: "#e2e8f0", cursor: "pointer", fontWeight: 600 });
  return (
    <div style={S.page}>
      <canvas ref={preview} style={{ position: "absolute", inset: 0, width: "100%", height: "100%" }} />
      <div style={S.panel} data-testid="hangar">
        <div style={{ fontSize: 30, fontWeight: 800, letterSpacing: 2 }}>SPACE ARENA</div>
        <div style={{ opacity: 0.7, marginBottom: 18 }}>Customize your ship and launch.</div>
        <label style={S.label}>Callsign</label>
        <input data-testid="name" style={S.input} value={look.name} maxLength={16} onChange={e => setLook({ ...look, name: e.target.value.replace(/[^\w -]/g, "") || "Pilot" })} />
        <label style={S.label}>Hull</label>
        <div style={{ display: "flex", gap: 8 }}>{HULLS.map(h => <button key={h.id} data-testid={"hull-" + h.id} style={btn(look.hull === h.id)} onClick={() => setLook({ ...look, hull: h.id as Hull })}>{h.label}</button>)}</div>
        <label style={S.label}>Colour</label>
        <div style={{ display: "flex", gap: 8 }}>{COLORS.map(c => <button key={c} aria-label={c} data-testid={"color-" + c.slice(1)} onClick={() => setLook({ ...look, color: c })} style={{ width: 32, height: 32, borderRadius: 16, background: c, border: look.color === c ? "3px solid #fff" : "2px solid #0008", cursor: "pointer" }} />)}</div>
        <label style={S.label}>Match code (share it to fly together)</label>
        <input data-testid="code" style={S.input} value={code} onChange={e => setCode(cleanCode(e.target.value))} />
        <button data-testid="launch" onClick={onLaunch} style={{ ...S.input, marginTop: 18, background: look.color, color: "#020617", fontWeight: 800, fontSize: 18, cursor: "pointer", border: "none" }}>LAUNCH</button>
        <div style={{ marginTop: 14, fontSize: 13, opacity: 0.7, lineHeight: 1.6 }}>
          W/S throttle · A/D turn · ↑/↓ pitch · Q/E roll<br />Space fire · Shift boost · M mute
        </div>
      </div>
    </div>
  );
};

// ------------------------------------------------------------------ arena

const Arena: React.FC<{ look: ShipLook; code: string; onExit: () => void }> = ({ look, code, onExit }) => {
  const canvas = useRef<HTMLCanvasElement>(null);
  const [hud, setHud] = useState({ fps: 0, hp: MAX_HP, alive: true, net: "connecting", players: 1, kills: 0, deaths: 0, muted: false, layers: [] as string[], intensity: 0, cadence: 0, flash: 0, feed: [] as string[] });
  const [board, setBoard] = useState<{ name: string; color: string; kills: number; deaths: number; me?: boolean }[]>([]);

  useEffect(() => {
    const engine = new Engine(canvas.current!, look, "match:" + code);
    const audio = new GameAudio();
    const peers = new Map<string, Peer>();
    const me = { kills: 0, deaths: 0 };
    const feed: string[] = [];
    const say = (s: string) => { feed.unshift(s); feed.length = Math.min(feed.length, 5); };
    let muted = false;
    let room: LiveRoom;
    const nameOf = (id: string) => peers.get(id)?.look.name ?? "someone";

    // ---- input
    const keys = new Set<string>();
    const onKey = (down: boolean) => (e: KeyboardEvent) => {
      if ((e.target as HTMLElement)?.tagName === "INPUT") return;
      const k = e.code;
      if (["Space", "ArrowUp", "ArrowDown", "ArrowLeft", "ArrowRight"].includes(k)) e.preventDefault();
      audio.start(); // first key press unlocks audio
      if (down && k === "KeyM") { muted = !muted; audio.setMuted(muted); }
      if (down) keys.add(k); else keys.delete(k);
      const c = engine.controls;
      c.throttle = (keys.has("KeyW") ? 1 : 0) - (keys.has("KeyS") ? 0.6 : 0);
      c.yaw = (keys.has("KeyD") || keys.has("ArrowRight") ? 1 : 0) - (keys.has("KeyA") || keys.has("ArrowLeft") ? 1 : 0);
      c.pitch = (keys.has("ArrowUp") ? 1 : 0) - (keys.has("ArrowDown") ? 1 : 0);
      c.roll = (keys.has("KeyE") ? 1 : 0) - (keys.has("KeyQ") ? 1 : 0);
      c.boost = keys.has("ShiftLeft") || keys.has("ShiftRight");
      c.fire = keys.has("Space");
    };
    const kd = onKey(true), ku = onKey(false);
    window.addEventListener("keydown", kd); window.addEventListener("keyup", ku);
    const onBlur = () => { keys.clear(); Object.assign(engine.controls, { throttle: 0, yaw: 0, pitch: 0, roll: 0, boost: false, fire: false }); };
    window.addEventListener("blur", onBlur);
    const onResize = () => engine.resize(); window.addEventListener("resize", onResize);

    // ---- engine → audio/network
    engine.events = {
      onFire: (p, d, id) => { audio.laser(-0.15 + Math.random() * 0.3); room?.send({ k: "f", p: p.map(r2), d: d.map(r3), id }); },
      onHitBy: (by, hp) => { audio.hit(); room?.send({ k: "h", by, hp }); },
      onDied: killer => { me.deaths++; room?.send({ k: "d", by: killer }); say(killer ? `${nameOf(killer)} destroyed you` : "You crashed"); },
      onSpark: () => audio.bump(0.04),
      onCrash: () => audio.hit(),
      onExplosion: near => audio.explosion(near),
    };

    // ---- network
    room = joinRoom("arena-" + code, {
      name: look.name,
      onWelcome: (meP, list) => { engine.myId = meP.id; say(`Joined match "${code}" (${list.length} other${list.length === 1 ? "" : "s"} here)`); },
      onJoin: p => say(`${p.name} joined`),
      onLeave: id => { const p = peers.get(id); if (p) say(`${p.look.name} left`); peers.delete(id); engine.removeRemote(id); },
      onStatus: s => setHud(h => ({ ...h, net: s })),
      onMessage: (from, m, _at) => {
        if (!m || typeof m !== "object") return;
        let p = peers.get(from);
        if (m.k === "s") {
          const lk = sanitizeLook(m.look); const s = sanitizeState(m.s); if (!lk || !s) return;
          const now = performance.now();
          if (!p) { p = { id: from, look: lk, kills: 0, deaths: 0, last: now, gaps: [] }; peers.set(from, p); }
          else { p.gaps.push(now - p.last); if (p.gaps.length > 200) p.gaps.shift(); p.last = now; p.look = lk; }
          if (typeof m.kills === "number") p.kills = m.kills | 0; if (typeof m.deaths === "number") p.deaths = m.deaths | 0;
          engine.upsertRemote(from, lk, s);
        } else if (m.k === "f" && p && Array.isArray(m.p) && Array.isArray(m.d)) {
          engine.remoteFire(from, m.p.map(Number), m.d.map(Number), String(m.id || ""));
          const s = engine.others.get(from); if (s) { const dist = s.pos.distanceTo(engine.me.pos); if (dist < 300) audio.laser(0); }
        } else if (m.k === "h" && p) {
          if (m.by === engine.myId) audio.bump(0.1);
          engine.remoteHit(from);
        } else if (m.k === "d" && p) {
          if (m.by === engine.myId) { me.kills++; say(`You destroyed ${p.look.name}`); }
          else if (m.by && peers.get(m.by)) say(`${nameOf(m.by)} destroyed ${p.look.name}`);
          else say(`${p.look.name} crashed`);
        }
      },
    });

    const sendTimer = window.setInterval(() => {
      room.send({ k: "s", look, s: engine.myState(), kills: me.kills, deaths: me.deaths });
    }, SEND_MS);

    // ---- HUD + audio params, 10 times a second
    const hudTimer = window.setInterval(() => {
      audio.thruster(engine.controls.throttle, engine.controls.boost);
      const gaps = [...peers.values()].flatMap(p => p.gaps.slice(-40));
      const cadence = gaps.length ? gaps.reduce((a, b) => a + b, 0) / gaps.length : 0;
      setHud({ fps: engine.fps, hp: engine.me.hp, alive: engine.me.alive, net: room.status, players: peers.size + 1, kills: me.kills, deaths: me.deaths, muted, layers: audio.activeLayers(), intensity: audio.intensity, cadence, flash: engine.flash, feed: [...feed] });
      setBoard([{ name: look.name, color: look.color, kills: me.kills, deaths: me.deaths, me: true }, ...[...peers.values()].map(p => ({ name: p.look.name, color: p.look.color, kills: p.kills, deaths: p.deaths }))].sort((a, b) => b.kills - a.kills));
    }, 100);

    // test/measurement hook (read-only snapshot + controls), used by the automated checks
    (window as any).__arena = {
      engine, audio, peers, me,
      stats: () => ({
        fps: engine.fps, frameTimes: engine.frameTimes.length, myId: engine.myId, status: room.status,
        peers: [...peers.values()].map(p => ({ id: p.id, name: p.look.name, gaps: p.gaps.slice(), visible: engine.others.has(p.id) })),
        quality: engine.quality, remoteShots: engine.remoteShots, deadUntil: engine.deadUntil, now: performance.now(), hp: engine.me.hp, alive: engine.me.alive, kills: me.kills, deaths: me.deaths, lasers: engine.lasers.length, bursts: engine.bursts.length,
        audio: { state: audio.ctx?.state ?? "none", muted: audio.muted, master: audio.masterLevel, intensity: audio.intensity, layers: audio.activeLayers(), ...audio.stats },
        pos: engine.me.pos.toArray(),
      }),
      dropConnection: () => room.close(),
    };

    engine.start();
    return () => {
      clearInterval(sendTimer); clearInterval(hudTimer);
      window.removeEventListener("keydown", kd); window.removeEventListener("keyup", ku);
      window.removeEventListener("blur", onBlur); window.removeEventListener("resize", onResize);
      room.close(); engine.stop(); audio.stop(); delete (window as any).__arena;
    };
  }, [look, code]);

  const hpPct = Math.max(0, hud.hp) / MAX_HP;
  return (
    <div style={S.page} onClick={() => (window as any).__arena?.audio.start()}>
      <canvas ref={canvas} data-testid="arena" style={{ position: "absolute", inset: 0, width: "100%", height: "100%" }} />
      <div style={{ position: "absolute", inset: 0, pointerEvents: "none", background: `radial-gradient(circle, transparent 55%, rgba(255,40,40,${hud.flash * 0.55}))` }} />
      {/* crosshair */}
      <div style={{ position: "absolute", left: "50%", top: "46%", width: 26, height: 26, marginLeft: -13, marginTop: -13, border: `2px solid ${look.color}`, borderRadius: 13, opacity: 0.8, pointerEvents: "none" }} />
      <div style={{ ...S.hud, left: 16, top: 16 }} data-testid="hud">
        <div><b data-testid="fps" style={{ color: hud.fps >= 55 ? "#4ade80" : hud.fps >= 30 ? "#facc15" : "#f87171" }}>{hud.fps} FPS</b> · {hud.net === "open" ? <span style={{ color: "#4ade80" }}>online</span> : <span style={{ color: "#facc15" }}>{hud.net}</span>} · {hud.players} in match "{code}"</div>
        {hud.cadence > 0 && <div style={{ opacity: 0.7 }}>updates every {hud.cadence.toFixed(1)} ms</div>}
        <div style={{ marginTop: 8, width: 220, height: 10, background: "#1e293b", borderRadius: 5 }}><div style={{ width: `${hpPct * 100}%`, height: "100%", borderRadius: 5, background: hpPct > 0.5 ? "#4ade80" : hpPct > 0.25 ? "#facc15" : "#f87171" }} /></div>
        <div style={{ marginTop: 6, opacity: 0.8 }}>Music: {hud.layers.join(" + ") || "—"} {hud.muted && <b style={{ color: "#f87171" }}> · MUTED</b>}</div>
      </div>
      <div style={{ ...S.hud, right: 16, top: 16, minWidth: 180 }}>
        {board.map((b, i) => <div key={i} style={{ display: "flex", justifyContent: "space-between", gap: 16, fontWeight: b.me ? 800 : 400 }}><span style={{ color: b.color }}>{b.name}</span><span>{b.kills} / {b.deaths}</span></div>)}
      </div>
      <div style={{ ...S.hud, left: 16, bottom: 16 }}>{hud.feed.map((f, i) => <div key={i} style={{ opacity: 1 - i * 0.17 }}>{f}</div>)}</div>
      <div style={{ position: "absolute", right: 16, bottom: 16, display: "flex", gap: 8 }}>
        <button style={S.small} onClick={e => { e.stopPropagation(); const a = (window as any).__arena; a.audio.start(); a.audio.setMuted(!a.audio.muted); }}>{hud.muted ? "Unmute" : "Mute"} (M)</button>
        <button style={S.small} onClick={onExit}>Hangar</button>
      </div>
      {!hud.alive && <div style={{ position: "absolute", top: "40%", width: "100%", textAlign: "center", fontSize: 40, fontWeight: 800, color: "#f87171", textShadow: "0 0 20px #000" }}>DESTROYED · respawning…</div>}
    </div>
  );
};

const r2 = (n: number) => Math.round(n * 100) / 100, r3 = (n: number) => Math.round(n * 1000) / 1000;
function sanitizeLook(l: any): ShipLook | null {
  if (!l || typeof l !== "object") return null;
  const hull = HULLS.some(h => h.id === l.hull) ? l.hull : "dart";
  const color = /^#[0-9a-f]{6}$/i.test(l.color) ? l.color : "#ffffff";
  return { name: String(l.name || "Pilot").slice(0, 16), color, hull };
}
function sanitizeState(s: any): ShipState | null {
  const nums = (a: any, n: number) => Array.isArray(a) && a.length === n && a.every((x: any) => Number.isFinite(x));
  if (!s || !nums(s.p, 3) || !nums(s.q, 4) || !nums(s.v, 3)) return null;
  return { p: s.p, q: s.q, v: s.v, hp: Number(s.hp) || 0, thr: Number(s.thr) || 0, alive: !!s.alive };
}

const S: Record<string, React.CSSProperties> = {
  page: { position: "fixed", inset: 0, background: "#02030a", color: "#e2e8f0", fontFamily: "system-ui, sans-serif", overflow: "hidden", userSelect: "none" },
  panel: { position: "absolute", left: 32, top: "50%", transform: "translateY(-50%)", width: 340, padding: 24, borderRadius: 16, background: "rgba(2,6,23,0.78)", border: "1px solid #1e293b", backdropFilter: "blur(6px)" },
  label: { display: "block", fontSize: 12, textTransform: "uppercase", letterSpacing: 1, opacity: 0.6, margin: "14px 0 6px" },
  input: { width: "100%", boxSizing: "border-box", padding: "10px 12px", borderRadius: 8, border: "1px solid #334155", background: "#0b1220", color: "#e2e8f0", fontSize: 16 },
  hud: { position: "absolute", padding: "10px 14px", borderRadius: 10, background: "rgba(2,6,23,0.6)", fontSize: 14, lineHeight: 1.5, pointerEvents: "none" },
  small: { padding: "8px 12px", borderRadius: 8, border: "1px solid #334155", background: "rgba(2,6,23,0.8)", color: "#e2e8f0", cursor: "pointer" },
};
