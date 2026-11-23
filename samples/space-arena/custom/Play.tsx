// SpaceArena flight screen: hangar (customize ship) → arena (fly, fight) with live multiplayer and procedural audio.
import React, { useEffect, useRef, useState } from "react";
import { joinRoom, LiveRoom } from "../neoapps/live";
import { Engine, COLORS, HULLS, Hull, ShipLook, ShipState, MAX_HP } from "./engine";
import { GameAudio } from "./audio";
import * as THREE from "three";

const SEND_MS = 50;
const KEY = "spacearena.look";

interface Peer { id: string; look: ShipLook; kills: number; deaths: number; last: number; gaps: number[] }

function loadLook(): ShipLook {
  try { const v = JSON.parse(localStorage.getItem(KEY) || ""); if (v && v.name) return v; } catch { /* ignore */ }
  return { name: "Pilot" + Math.floor(Math.random() * 900 + 100), color: COLORS[Math.floor(Math.random() * COLORS.length)], hull: "dart" };
}
function saveLook(l: ShipLook) { try { localStorage.setItem(KEY, JSON.stringify(l)); } catch { /* ignore */ } }
/** Phones and tablets: a coarse pointer or touch support without a fine pointer. */
const isTouch = () => typeof window !== "undefined" && (window.matchMedia?.("(pointer: coarse)").matches || ("ontouchstart" in window && !window.matchMedia?.("(pointer: fine)").matches));
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
  const [short, setShort] = useState(() => window.innerHeight < 560);
  useEffect(() => { const f = () => setShort(window.innerHeight < 560); window.addEventListener("resize", f); return () => window.removeEventListener("resize", f); }, []);
  const label: React.CSSProperties = short ? { ...S.label, margin: "8px 0 4px" } : S.label;
  const btn = (active: boolean): React.CSSProperties => ({ padding: short ? "6px 12px" : "8px 14px", borderRadius: 8, border: active ? "2px solid #fff" : "1px solid #334", background: active ? "#1e293b" : "#0b1220", color: "#e2e8f0", cursor: "pointer", fontWeight: 600 });
  return (
    <div style={S.page}>
      <canvas ref={preview} style={{ position: "absolute", inset: 0, width: "100%", height: "100%" }} />
      <div style={{ ...S.panel, ...(short ? { padding: "14px 18px 0", top: 12, bottom: 12, transform: "none", maxHeight: "none" } : {}) }} data-testid="hangar">
        <div style={{ fontSize: short ? 20 : 30, fontWeight: 800, letterSpacing: 2 }}>SPACE ARENA</div>
        {!short && <div style={{ opacity: 0.7, marginBottom: 18 }}>Customize your ship and launch.</div>}
        <label style={label}>Callsign</label>
        <input data-testid="name" style={S.input} value={look.name} maxLength={16} onChange={e => setLook({ ...look, name: e.target.value.replace(/[^\w -]/g, "") || "Pilot" })} />
        <label style={label}>Hull</label>
        <div style={{ display: "flex", gap: 8 }}>{HULLS.map(h => <button key={h.id} data-testid={"hull-" + h.id} style={btn(look.hull === h.id)} onClick={() => setLook({ ...look, hull: h.id as Hull })}>{h.label}</button>)}</div>
        <label style={label}>Colour</label>
        <div style={{ display: "flex", gap: 8 }}>{COLORS.map(c => <button key={c} aria-label={c} data-testid={"color-" + c.slice(1)} onClick={() => setLook({ ...look, color: c })} style={{ width: 32, height: 32, borderRadius: 16, background: c, border: look.color === c ? "3px solid #fff" : "2px solid #0008", cursor: "pointer" }} />)}</div>
        <label style={label}>Match code (share it to fly together)</label>
        <input data-testid="code" style={S.input} value={code} onChange={e => setCode(cleanCode(e.target.value))} />
        <div style={{ position: "sticky", bottom: 0, paddingTop: short ? 10 : 18, paddingBottom: short ? 12 : 0, background: short ? "linear-gradient(transparent, rgba(2,6,23,0.95) 30%)" : undefined, zIndex: 1 }}>
          <button data-testid="launch" onClick={onLaunch} style={{ ...S.input, background: look.color, color: "#020617", fontWeight: 800, fontSize: 18, cursor: "pointer", border: "none" }}>LAUNCH</button>
        </div>
        <div style={{ marginTop: 14, fontSize: 13, opacity: 0.7, lineHeight: 1.6 }}>
          {isTouch()
            ? <>Left thumb: drag the stick to steer. Right thumb: FIRE, BOOST and BRAKE. Your ship cruises forward on its own. Turn your phone sideways for the best view.</>
            : <>W/S throttle · A/D turn · ↑/↓ pitch · Q/E roll<br />Space fire · Shift boost · M mute</>}
          <br />Radar: arrows point to other ships; the scope shows everyone around you.
        </div>
        <a href="/" style={{ display: "inline-block", marginTop: 12, color: "#94a3b8", fontSize: 14 }}>← Back to the app</a>
      </div>
    </div>
  );
};

// ------------------------------------------------------------------ arena

const Arena: React.FC<{ look: ShipLook; code: string; onExit: () => void }> = ({ look, code, onExit }) => {
  const canvas = useRef<HTMLCanvasElement>(null);
  const radarRef = useRef<HTMLCanvasElement>(null);
  const [touchMode] = useState(isTouch);
  const touchRef = useRef<TouchState>({ x: 0, y: 0, fire: false, boost: false, brake: false, apply: () => undefined });
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
    // touch controls (phones): stick steers, buttons fire / boost / brake, ship cruises forward
    const touch = touchRef.current;
    touch.apply = () => {
      const c = engine.controls, t = touch;
      audio.start();
      c.yaw = t.x; c.pitch = -t.y; c.roll = 0;
      c.fire = t.fire; c.boost = t.boost;
      c.throttle = t.brake ? -0.6 : t.boost ? 1 : 0.55;
    };
    if (touchMode) touch.apply();
    window.addEventListener("keydown", kd); window.addEventListener("keyup", ku);
    const onBlur = () => { keys.clear(); Object.assign(engine.controls, { throttle: 0, yaw: 0, pitch: 0, roll: 0, boost: false, fire: false }); if (touchMode) { Object.assign(touch, { x: 0, y: 0, fire: false, boost: false, brake: false }); touch.apply(); } };
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
    const stopRadar = startRadar(radarRef.current!, engine, touchMode);
    return () => {
      stopRadar();
      clearInterval(sendTimer); clearInterval(hudTimer);
      window.removeEventListener("keydown", kd); window.removeEventListener("keyup", ku);
      window.removeEventListener("blur", onBlur); window.removeEventListener("resize", onResize);
      room.close(); engine.stop(); audio.stop(); delete (window as any).__arena;
    };
  }, [look, code, touchMode]);

  const hpPct = Math.max(0, hud.hp) / MAX_HP;
  return (
    <div style={S.page} onClick={() => (window as any).__arena?.audio.start()}>
      <canvas ref={canvas} data-testid="arena" style={{ position: "absolute", inset: 0, width: "100%", height: "100%" }} />
      <canvas ref={radarRef} data-testid="radar" style={{ position: "absolute", inset: 0, width: "100%", height: "100%", pointerEvents: "none" }} />
      <div style={{ position: "absolute", inset: 0, pointerEvents: "none", background: `radial-gradient(circle, transparent 55%, rgba(255,40,40,${hud.flash * 0.55}))` }} />
      {/* crosshair */}
      <div style={{ position: "absolute", left: "50%", top: "46%", width: 26, height: 26, marginLeft: -13, marginTop: -13, border: `2px solid ${look.color}`, borderRadius: 13, opacity: 0.8, pointerEvents: "none" }} />
      <div style={{ ...S.hud, left: 16, top: 16, ...(touchMode ? { fontSize: 12, padding: "6px 10px" } : {}) }} data-testid="hud">
        <div><b data-testid="fps" style={{ color: hud.fps >= 55 ? "#4ade80" : hud.fps >= 30 ? "#facc15" : "#f87171" }}>{hud.fps} FPS</b> · {hud.net === "open" ? <span style={{ color: "#4ade80" }}>online</span> : <span style={{ color: "#facc15" }}>{hud.net}</span>} · {hud.players} in match "{code}"</div>
        {hud.cadence > 0 && <div style={{ opacity: 0.7 }}>updates every {hud.cadence.toFixed(1)} ms</div>}
        <div style={{ marginTop: 8, width: touchMode ? 150 : 220, height: 10, background: "#1e293b", borderRadius: 5 }}><div style={{ width: `${hpPct * 100}%`, height: "100%", borderRadius: 5, background: hpPct > 0.5 ? "#4ade80" : hpPct > 0.25 ? "#facc15" : "#f87171" }} /></div>
        <div style={{ marginTop: 6, opacity: 0.8 }}>Music: {hud.layers.join(" + ") || "—"} {hud.muted && <b style={{ color: "#f87171" }}> · MUTED</b>}</div>
      </div>
      <div style={{ ...S.hud, right: 16, top: 16, minWidth: touchMode ? 130 : 180, ...(touchMode ? { fontSize: 12, padding: "6px 10px" } : {}) }}>
        {board.map((b, i) => <div key={i} style={{ display: "flex", justifyContent: "space-between", gap: 16, fontWeight: b.me ? 800 : 400 }}><span style={{ color: b.color }}>{b.name}</span><span>{b.kills} / {b.deaths}</span></div>)}
      </div>
      <div style={{ ...S.hud, ...(touchMode ? { left: 16, top: 120, fontSize: 12, padding: "6px 10px" } : { left: 16, bottom: 16 }) }}>{hud.feed.slice(0, touchMode ? 3 : 5).map((f, i) => <div key={i} style={{ opacity: 1 - i * 0.17 }}>{f}</div>)}</div>
      {touchMode && <TouchControls state={touchRef.current} />}
      <div style={{ position: "absolute", right: 16, ...(touchMode ? { top: 16 + 22 * (board.length + 1) } : { bottom: 16 }), display: "flex", gap: 8 }}>
        <button style={S.small} onClick={e => { e.stopPropagation(); const a = (window as any).__arena; a.audio.start(); a.audio.setMuted(!a.audio.muted); }}>{hud.muted ? "Unmute" : "Mute"} (M)</button>
        <button style={S.small} onClick={onExit}>Hangar</button>
      </div>
      {!hud.alive && <div style={{ position: "absolute", top: "40%", width: "100%", textAlign: "center", fontSize: 40, fontWeight: 800, color: "#f87171", textShadow: "0 0 20px #000" }}>DESTROYED · respawning…</div>}
    </div>
  );
};

interface TouchState { x: number; y: number; fire: boolean; boost: boolean; brake: boolean; apply: () => void }

/** On-screen controls for phones: a stick on the left, FIRE / BOOST / BRAKE on the right (multi-touch). */
const TouchControls: React.FC<{ state: TouchState }> = ({ state }) => {
  const [knob, setKnob] = useState({ x: 0, y: 0, active: false });
  const [btn, setBtn] = useState({ fire: false, boost: false, brake: false });
  const base = useRef<HTMLDivElement>(null);
  const stickId = useRef<number | null>(null);
  const SIZE = 132, R = SIZE / 2 - 22;
  const move = (e: React.PointerEvent) => {
    const r = base.current!.getBoundingClientRect();
    let dx = e.clientX - (r.left + r.width / 2), dy = e.clientY - (r.top + r.height / 2);
    const len = Math.hypot(dx, dy); if (len > R) { dx *= R / len; dy *= R / len; }
    const nx = dx / R, ny = dy / R;
    // small dead zone, then a gentle curve for fine aim
    const curve = (v: number) => (Math.abs(v) < 0.08 ? 0 : Math.sign(v) * Math.pow(Math.abs(v), 1.4));
    state.x = curve(nx); state.y = curve(ny); state.apply();
    setKnob({ x: dx, y: dy, active: true });
  };
  const end = () => { stickId.current = null; state.x = 0; state.y = 0; state.apply(); setKnob({ x: 0, y: 0, active: false }); };
  const hold = (key: "fire" | "boost" | "brake", on: boolean) => (e: React.PointerEvent) => {
    e.preventDefault(); if (on) (e.currentTarget as HTMLElement).setPointerCapture?.(e.pointerId);
    state[key] = on; state.apply(); setBtn(b => ({ ...b, [key]: on }));
  };
  const button = (key: "fire" | "boost" | "brake", label: string, size: number, color: string, extra: React.CSSProperties) => (
    <div data-testid={"touch-" + key} onPointerDown={hold(key, true)} onPointerUp={hold(key, false)} onPointerCancel={hold(key, false)} onLostPointerCapture={() => { if (state[key]) { state[key] = false; state.apply(); setBtn(b => ({ ...b, [key]: false })); } }}
      style={{ position: "absolute", width: size, height: size, borderRadius: size / 2, display: "flex", alignItems: "center", justifyContent: "center", fontWeight: 800, fontSize: size > 80 ? 16 : 12, letterSpacing: 1,
        color: btn[key] ? "#020617" : color, background: btn[key] ? color : "rgba(2,6,23,0.45)", border: `2px solid ${color}`, touchAction: "none", userSelect: "none", ...extra }}>{label}</div>
  );
  return (
    <>
      <div ref={base} data-testid="touch-stick"
        onPointerDown={e => { e.preventDefault(); stickId.current = e.pointerId; (e.currentTarget as HTMLElement).setPointerCapture?.(e.pointerId); move(e); }}
        onPointerMove={e => { if (stickId.current === e.pointerId) move(e); }}
        onPointerUp={e => { if (stickId.current === e.pointerId) end(); }} onPointerCancel={end}
        style={{ position: "absolute", left: "max(20px, env(safe-area-inset-left))", bottom: "max(20px, env(safe-area-inset-bottom))", width: SIZE, height: SIZE, borderRadius: SIZE / 2,
          background: "rgba(2,6,23,0.4)", border: "2px solid rgba(148,163,184,0.5)", touchAction: "none" }}>
        <div style={{ position: "absolute", left: SIZE / 2 - 26 + knob.x, top: SIZE / 2 - 26 + knob.y, width: 52, height: 52, borderRadius: 26,
          background: knob.active ? "rgba(226,232,240,0.85)" : "rgba(226,232,240,0.45)", pointerEvents: "none" }} />
      </div>
      <div style={{ position: "absolute", right: "max(20px, env(safe-area-inset-right))", bottom: "max(20px, env(safe-area-inset-bottom))", width: 190, height: 170 }}>
        {button("fire", "FIRE", 96, "#f87171", { right: 0, bottom: 0 })}
        {button("boost", "BOOST", 64, "#22d3ee", { right: 104, bottom: 40 })}
        {button("brake", "BRAKE", 56, "#facc15", { right: 20, bottom: 108 })}
      </div>
    </>
  );
};

/**
 * Radar overlay: brackets on ships in view, arrows at the screen edge for ships out of view (with distance),
 * and a round scope at the bottom showing everyone around you from above (forward is up).
 */
function startRadar(cv: HTMLCanvasElement, engine: Engine, touch: boolean): () => void {
  const ctx = cv.getContext("2d")!;
  const inv = new THREE.Quaternion(), rel = new THREE.Vector3(), ndc = new THREE.Vector3();
  let raf = 0;
  (window as any).__radar = { blips: [] as any[] };
  const draw = () => {
    raf = requestAnimationFrame(draw);
    const dpr = Math.min(window.devicePixelRatio || 1, 2), w = cv.clientWidth, h = cv.clientHeight;
    if (cv.width !== Math.round(w * dpr) || cv.height !== Math.round(h * dpr)) { cv.width = Math.round(w * dpr); cv.height = Math.round(h * dpr); }
    ctx.setTransform(dpr, 0, 0, dpr, 0, 0); ctx.clearRect(0, 0, w, h);
    const cam = engine.camera, me = engine.me;
    const R = Math.min(70, Math.max(40, w * 0.09)), cx = w / 2, cy = touch ? R + 30 : h - R - 18, range = 420;
    // scope
    ctx.fillStyle = "rgba(2,6,23,0.55)"; ctx.strokeStyle = "rgba(148,163,184,0.45)"; ctx.lineWidth = 1;
    ctx.beginPath(); ctx.arc(cx, cy, R, 0, Math.PI * 2); ctx.fill(); ctx.stroke();
    ctx.beginPath(); ctx.arc(cx, cy, R / 2, 0, Math.PI * 2); ctx.stroke();
    ctx.beginPath(); ctx.moveTo(cx - R, cy); ctx.lineTo(cx + R, cy); ctx.moveTo(cx, cy - R); ctx.lineTo(cx, cy + R); ctx.stroke();
    ctx.fillStyle = me.look.color; ctx.beginPath(); ctx.moveTo(cx, cy - 6); ctx.lineTo(cx + 4, cy + 4); ctx.lineTo(cx - 4, cy + 4); ctx.closePath(); ctx.fill();
    const blips: any[] = [];
    inv.copy(me.quat).invert();
    let nearest: { id: string; d: number } | null = null;
    for (const [id, o] of engine.others) { if (!o.alive) continue; const d = o.pos.distanceTo(me.pos); if (!nearest || d < nearest.d) nearest = { id, d }; }
    for (const [id, o] of engine.others) {
      if (!o.alive) continue;
      const d = o.pos.distanceTo(me.pos); const color = o.look.color; const isNear = nearest?.id === id;
      // scope blip: ship's position in my own frame, seen from above (forward = up)
      rel.subVectors(o.pos, me.pos).applyQuaternion(inv);
      const k = Math.min(1, Math.hypot(rel.x, rel.z) / range) * (R - 6) / Math.max(1e-6, Math.hypot(rel.x, rel.z));
      const bx = cx + rel.x * k, by = cy + rel.z * k;
      ctx.fillStyle = color; ctx.beginPath(); ctx.arc(bx, by, isNear ? 4.5 : 3.5, 0, Math.PI * 2); ctx.fill();
      if (Math.abs(rel.y) > 15) { ctx.font = "bold 10px system-ui"; ctx.fillText(rel.y > 0 ? "▲" : "▼", bx + 5, by + 3); }
      // in-view bracket or edge arrow
      ndc.copy(o.pos).project(cam);
      const camRel = rel.subVectors(o.pos, cam.position).applyQuaternion(new THREE.Quaternion().copy(cam.quaternion).invert());
      const ahead = camRel.z < 0, sx = (ndc.x + 1) / 2 * w, sy = (1 - ndc.y) / 2 * h;
      const label = `${o.look.name} ${Math.round(d)}m`;
      ctx.font = "600 12px system-ui, sans-serif"; ctx.textAlign = "center";
      if (ahead && sx > 20 && sx < w - 20 && sy > 20 && sy < h - 20) {
        const s = Math.max(10, Math.min(28, 900 / Math.max(1, d)));
        ctx.strokeStyle = color; ctx.lineWidth = isNear ? 2.5 : 1.5;
        ctx.beginPath();
        for (const [ax, ay] of [[-1, -1], [1, -1], [1, 1], [-1, 1]]) { ctx.moveTo(sx + ax * s, sy + ay * s * 0.6); ctx.lineTo(sx + ax * s, sy + ay * s); ctx.lineTo(sx + ax * s * 0.6, sy + ay * s); }
        ctx.stroke();
        ctx.fillStyle = color; ctx.fillText(`${Math.round(d)}m`, sx, sy + s + 14);
        blips.push({ id, d: Math.round(d), onScreen: true });
      } else {
        let dx = camRel.x, dy = -camRel.y; const len = Math.hypot(dx, dy) || 1; dx /= len; dy /= len;
        const mx = w / 2 - 40, my = dy > 0 ? h / 2 - (2 * R + 60) : h / 2 - 40, t = Math.min(mx / Math.max(1e-6, Math.abs(dx)), my / Math.max(1e-6, Math.abs(dy)));
        const ex = w / 2 + dx * t, ey = h / 2 + dy * t, ang = Math.atan2(dy, dx);
        ctx.save(); ctx.translate(ex, ey); ctx.rotate(ang); ctx.fillStyle = color; ctx.globalAlpha = isNear ? 1 : 0.75;
        ctx.beginPath(); ctx.moveTo(14, 0); ctx.lineTo(-8, 9); ctx.lineTo(-3, 0); ctx.lineTo(-8, -9); ctx.closePath(); ctx.fill(); ctx.restore();
        ctx.fillStyle = color; ctx.fillText(label, Math.max(50, Math.min(w - 50, ex - dx * 26)), Math.max(16, Math.min(h - 8, ey - dy * 26 + 4)));
        blips.push({ id, d: Math.round(d), onScreen: false });
      }
    }
    if (nearest) { ctx.textAlign = "center"; ctx.font = "600 12px system-ui, sans-serif"; ctx.fillStyle = "#cbd5e1"; ctx.fillText(`Nearest ${engine.others.get(nearest.id)!.look.name} · ${Math.round(nearest.d)}m`, cx, cy - R - 8); }
    else { ctx.textAlign = "center"; ctx.font = "12px system-ui, sans-serif"; ctx.fillStyle = "#94a3b8"; ctx.fillText("No one else in this match yet", cx, cy - R - 8); }
    (window as any).__radar.blips = blips;
  };
  raf = requestAnimationFrame(draw);
  return () => { cancelAnimationFrame(raf); delete (window as any).__radar; };
}

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
  page: { position: "fixed", inset: 0, zIndex: 1300, background: "#02030a", color: "#e2e8f0", fontFamily: "system-ui, sans-serif", overflow: "hidden", userSelect: "none", WebkitUserSelect: "none", touchAction: "none", WebkitTouchCallout: "none", overscrollBehavior: "none" } as React.CSSProperties,
  panel: { position: "absolute", left: "max(16px, min(32px, 4vw))", top: "50%", transform: "translateY(-50%)", width: "min(340px, calc(100vw - 32px))", maxHeight: "calc(100% - 24px)", overflowY: "auto", touchAction: "pan-y", boxSizing: "border-box", padding: 24, borderRadius: 16, background: "rgba(2,6,23,0.78)", border: "1px solid #1e293b", backdropFilter: "blur(6px)" },
  label: { display: "block", fontSize: 12, textTransform: "uppercase", letterSpacing: 1, opacity: 0.6, margin: "14px 0 6px" },
  input: { width: "100%", boxSizing: "border-box", padding: "10px 12px", borderRadius: 8, border: "1px solid #334155", background: "#0b1220", color: "#e2e8f0", fontSize: 16 },
  hud: { position: "absolute", padding: "10px 14px", borderRadius: 10, background: "rgba(2,6,23,0.6)", fontSize: 14, lineHeight: 1.5, pointerEvents: "none" },
  small: { padding: "8px 12px", borderRadius: 8, border: "1px solid #334155", background: "rgba(2,6,23,0.8)", color: "#e2e8f0", cursor: "pointer" },
};
