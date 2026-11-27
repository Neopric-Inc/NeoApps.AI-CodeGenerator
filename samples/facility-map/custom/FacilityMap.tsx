// FacilityMap: buildings, floors and units drawn in 3D, each unit coloured by its worst open ticket.
// Data comes through the normal API (so each person sees only what their role allows: a tenant sees only their unit).
// The live room "facility" (ui.config.json live.tables) tells every screen within a second when a ticket, unit or
// appliance changes; the screen then re-reads it.
import React, { useCallback, useEffect, useMemo, useRef, useState } from "react";
import * as THREE from "three";
import { OrbitControls } from "three/examples/jsm/controls/OrbitControls.js";
import { Alert, Box, Button, Chip, CircularProgress, MenuItem, Paper, Stack, TextField, Typography } from "@mui/material";
import { createRecord, listRecords, updateRecord } from "../neoapps/api";
import { useApp } from "../neoapps/AppContext";
import { joinRoom } from "../neoapps/live";

type Row = Record<string, any>;
type Level = "hold" | "urgent" | "routine" | "clear";

const SEVERITY: Record<string, Level> = { "Safety Hold": "hold", "Urgent Repair": "urgent", "Routine Maintenance": "routine" };
const RANK: Record<Level, number> = { hold: 3, urgent: 2, routine: 1, clear: 0 };
const COLOR: Record<Level, string> = { hold: "#ef4444", urgent: "#f97316", routine: "#facc15", clear: "#22c55e" };
const LABEL: Record<Level, string> = { hold: "Safety hold", urgent: "Urgent repair", routine: "Routine maintenance", clear: "All clear" };
const FLOOR_H = 3.8;
const opacityOf = (l: Level, shared: boolean) => (l === "clear" ? (shared ? 0.16 : 0.22) : 0.95);

const isOpen = (t: Row) => t.ticket_status !== "Resolved";
const levelOf = (tickets: Row[]): Level =>
  tickets.filter(isOpen).reduce<Level>((worst, t) => {
    const l = SEVERITY[t.severity] || "routine";
    return RANK[l] > RANK[worst] ? l : worst;
  }, "clear");

async function all(table: string): Promise<Row[]> {
  try { return (await listRecords(table, 1, 1000)).records; } catch { return []; }
}

function textSprite(text: string, size = 0.9, color = "#e2e8f0", weight = 600): THREE.Sprite {
  const c = document.createElement("canvas");
  const ctx = c.getContext("2d")!;
  const font = `${weight} 64px Inter, system-ui, sans-serif`;
  ctx.font = font;
  const w = Math.ceil(ctx.measureText(text).width) + 24;
  c.width = w; c.height = 88;
  ctx.font = font; ctx.fillStyle = color; ctx.textBaseline = "middle";
  ctx.shadowColor = "rgba(0,0,0,.6)"; ctx.shadowBlur = 8;
  ctx.fillText(text, 12, 46);
  const tex = new THREE.CanvasTexture(c);
  tex.colorSpace = THREE.SRGBColorSpace;
  const s = new THREE.Sprite(new THREE.SpriteMaterial({ map: tex, depthTest: false, transparent: true }));
  s.scale.set((size * w) / 88, size, 1);
  s.renderOrder = 10;
  return s;
}

interface UnitMesh { mesh: THREE.Mesh; edges: THREE.LineSegments; label: THREE.Sprite; level: Level; pulse: number; shared: boolean }

export function FacilityMap() {
  const { me } = useApp();
  const role = me?.is_admin && me.role_id === 1 ? "Property Manager" : me?.role_name || "";
  const [data, setData] = useState<{ buildings: Row[]; floors: Row[]; units: Row[]; tickets: Row[]; appliances: Row[] } | null>(null);
  const [selected, setSelected] = useState<number | null>(null);
  const [peers, setPeers] = useState(1);
  const [live, setLive] = useState<"connecting" | "open" | "closed">("connecting");
  const [sync, setSync] = useState<string | null>(null);
  const [toasts, setToasts] = useState<{ id: number; text: string; level: Level }[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  // wall display: the map and panel fill the screen (a TV in the office), also ?wall=1 in the address
  const [wall, setWall] = useState(() => new URLSearchParams(window.location.search).get("wall") === "1");

  const levels = useMemo(() => {
    const m = new Map<number, Level>();
    if (!data) return m;
    for (const u of data.units) m.set(u.unit_id, levelOf(data.tickets.filter(t => t.unit_id === u.unit_id)));
    return m;
  }, [data]);
  const prevLevels = useRef<Map<number, Level> | null>(null);

  const TABLES = ["buildings", "floors", "units", "tickets", "appliances"] as const;
  /** Reads the given tables again (all of them by default) through the API. */
  const load = useCallback(async (only?: string[]) => {
    const names = (only?.length ? TABLES.filter(t => only.includes(t)) : [...TABLES]) as string[];
    const rows = await Promise.all(names.map(all));
    setData(d => {
      const next: any = { ...(d || { buildings: [], floors: [], units: [], tickets: [], appliances: [] }) };
      names.forEach((n, i) => { next[n] = rows[i]; });
      return next;
    });
  }, []);

  // first load + live room: re-read when anyone changes a ticket, unit or appliance
  useEffect(() => {
    load();
    let timer: any; let pendingAt = 0; const pending = new Set<string>();
    const room = joinRoom("facility", {
      name: me?.username,
      onStatus: setLive,
      onWelcome: (_me, others) => setPeers(others.length + 1),
      onJoin: () => setPeers(p => p + 1),
      onLeave: () => setPeers(p => Math.max(1, p - 1)),
      onChange: (table, _id, _action, at) => {
        pendingAt = pendingAt || at || Date.now();
        pending.add(table);
        clearTimeout(timer);
        timer = setTimeout(async () => {
          const started = pendingAt; pendingAt = 0;
          const tables = [...pending]; pending.clear();
          await load(tables);
          setSync(`Updated ${((Date.now() - started) / 1000).toFixed(2)} s after the change`);
        }, 30);
      },
    });
    return () => { clearTimeout(timer); room.close(); };
  }, [load, me?.username]);

  // toasts for units whose colour changed (by anyone)
  useEffect(() => {
    if (!data) return;
    const before = prevLevels.current;
    prevLevels.current = levels;
    if (!before) return;
    const changed: { id: number; text: string; level: Level }[] = [];
    for (const [id, l] of levels) {
      const was = before.get(id);
      if (was && was !== l) {
        const u = data.units.find(x => x.unit_id === id);
        changed.push({ id: Date.now() + id, text: `${unitName(u)} is now ${LABEL[l].toLowerCase()}`, level: l });
      }
    }
    if (changed.length) {
      setToasts(t => [...t, ...changed].slice(-3));
      setTimeout(() => setToasts(t => t.filter(x => !changed.some(c => c.id === x.id))), 4500);
    }
  }, [levels, data]);

  const unitName = (u?: Row) => {
    if (!u || !data) return "";
    const f = data.floors.find(x => x.floor_id === u.floor_id);
    const b = data.buildings.find(x => x.building_id === f?.building_id);
    return `${b?.building_name ?? ""} ${u.unit_kind === "Shared" ? u.unit_label : "unit " + u.unit_label}`.trim();
  };

  // ------------------------------------------------------------------ three.js scene
  const host = useRef<HTMLDivElement>(null);
  const three = useRef<{ scene: THREE.Scene; group: THREE.Group; camera: THREE.PerspectiveCamera; controls: OrbitControls;
    renderer: THREE.WebGLRenderer; units: Map<number, UnitMesh>; built: string } | null>(null);
  const selectedRef = useRef<number | null>(null);
  selectedRef.current = selected;

  useEffect(() => {
    const el = host.current!;
    const renderer = new THREE.WebGLRenderer({ antialias: true, preserveDrawingBuffer: true });
    renderer.setPixelRatio(Math.min(2, window.devicePixelRatio));
    el.appendChild(renderer.domElement);
    const scene = new THREE.Scene();
    scene.background = new THREE.Color("#0b1220");
    scene.fog = new THREE.Fog("#0b1220", 70, 140);
    const camera = new THREE.PerspectiveCamera(42, 1, 0.1, 400);
    camera.position.set(42, 28, 44);
    const controls = new OrbitControls(camera, renderer.domElement);
    controls.target.set(16, 4.5, 6);
    controls.enableDamping = true;
    controls.autoRotate = true;
    controls.autoRotateSpeed = 0.45;
    controls.maxPolarAngle = Math.PI * 0.47;
    scene.add(new THREE.HemisphereLight("#cbd5e1", "#0f172a", 1.1));
    const sun = new THREE.DirectionalLight("#ffffff", 1.6);
    sun.position.set(30, 50, 20);
    scene.add(sun);
    const grid = new THREE.GridHelper(120, 60, "#1e293b", "#132033");
    grid.position.set(16, -0.01, 6);
    scene.add(grid);
    const group = new THREE.Group();
    scene.add(group);
    three.current = { scene, group, camera, controls, renderer, units: new Map(), built: "" };

    const resize = () => {
      const w = el.clientWidth, h = el.clientHeight;
      renderer.setSize(w, h); camera.aspect = w / Math.max(1, h); camera.updateProjectionMatrix();
    };
    const ro = new ResizeObserver(resize); ro.observe(el); resize();

    // hover + click
    const ray = new THREE.Raycaster(); const pt = new THREE.Vector2();
    let hovered: number | null = null; let down = { x: 0, y: 0 };
    const pick = (ev: PointerEvent) => {
      const r = renderer.domElement.getBoundingClientRect();
      pt.set(((ev.clientX - r.left) / r.width) * 2 - 1, -((ev.clientY - r.top) / r.height) * 2 + 1);
      ray.setFromCamera(pt, camera);
      // all-clear units are glass: a click goes through them to a unit with an issue behind
      const hits = ray.intersectObjects([...three.current!.units.values()].map(u => u.mesh));
      const solid = hits.find(h => three.current!.units.get(h.object.userData.unitId)?.level !== "clear");
      const hit = solid || hits[0];
      return hit ? (hit.object.userData.unitId as number) : null;
    };
    const onMove = (ev: PointerEvent) => { hovered = pick(ev); renderer.domElement.style.cursor = hovered ? "pointer" : "grab"; };
    const onDown = (ev: PointerEvent) => { down = { x: ev.clientX, y: ev.clientY }; controls.autoRotate = false; };
    const onUp = (ev: PointerEvent) => {
      if (Math.hypot(ev.clientX - down.x, ev.clientY - down.y) > 5) return;
      setSelected(pick(ev));
    };
    renderer.domElement.addEventListener("pointermove", onMove);
    renderer.domElement.addEventListener("pointerdown", onDown);
    renderer.domElement.addEventListener("pointerup", onUp);

    // for automated tests and demo recordings: where a unit is on screen, in page coordinates
    (window as any).facilityMap = {
      screenPos: (unitId: number) => {
        const u = three.current?.units.get(unitId); if (!u) return null;
        const p = u.mesh.position.clone().project(camera); const r = renderer.domElement.getBoundingClientRect();
        return { x: r.left + ((p.x + 1) / 2) * r.width, y: r.top + ((1 - p.y) / 2) * r.height };
      },
      stopRotation: () => { controls.autoRotate = false; },
    };

    let raf = 0; const clock = new THREE.Clock();
    const tick = () => {
      const dt = clock.getDelta(); const t = clock.elapsedTime;
      for (const [id, u] of three.current!.units) {
        const mat = u.mesh.material as THREE.MeshStandardMaterial;
        const sel = selectedRef.current === id;
        u.pulse = Math.max(0, u.pulse - dt);
        const bounce = u.pulse > 0 ? 1 + Math.sin(u.pulse * 12) * 0.08 * u.pulse : 1;
        u.mesh.scale.setScalar(bounce);
        u.edges.scale.setScalar(bounce);
        const glow = u.level === "hold" ? 0.45 + 0.35 * Math.sin(t * 4) : u.level === "clear" ? 0.05 : 0.25;
        mat.emissiveIntensity = sel ? 0.9 : hovered === id ? 0.6 : glow + u.pulse * 0.8;
        (u.edges.material as THREE.LineBasicMaterial).color.set(sel ? "#ffffff" : u.level === "clear" ? "#4ade80" : "#e2e8f0");
        (u.edges.material as THREE.LineBasicMaterial).opacity = sel ? 1 : u.level === "clear" ? 0.45 : 0.7;
        (u.label.material as THREE.SpriteMaterial).opacity = sel || hovered === id || u.level !== "clear" ? 1 : 0.28;
      }
      controls.update();
      renderer.render(scene, camera);
      raf = requestAnimationFrame(tick);
    };
    tick();
    return () => {
      cancelAnimationFrame(raf); ro.disconnect(); controls.dispose(); renderer.dispose();
      renderer.domElement.remove(); three.current = null;
    };
  }, []);

  // (re)build the buildings when the layout changes; recolour units when their level changes
  useEffect(() => {
    const t3 = three.current;
    if (!t3 || !data) return;
    const layoutKey = JSON.stringify([data.buildings.map(b => [b.building_id, b.site_x, b.site_z]),
      data.floors.map(f => [f.floor_id, f.level]), data.units.map(u => [u.unit_id, u.floor_id, u.plan_x, u.plan_z, u.plan_w, u.plan_d, u.unit_label])]);
    if (t3.built !== layoutKey) {
      t3.group.clear(); t3.units.clear(); t3.built = layoutKey;
      for (const b of data.buildings) {
        const bf = data.floors.filter(f => f.building_id === b.building_id);
        const bx = Number(b.site_x), bz = Number(b.site_z);
        for (const f of bf) {
          const y = (f.level - 1) * FLOOR_H;
          const slab = new THREE.Mesh(new THREE.BoxGeometry(12.6, 0.18, 10.6),
            new THREE.MeshStandardMaterial({ color: "#475569", transparent: true, opacity: 0.6 }));
          slab.position.set(bx + 6, y + 0.09, bz + 5);
          t3.group.add(slab);
        }
        const top = Math.max(1, ...bf.map(f => f.level));
        const name = textSprite(b.building_name, 1.4, "#f8fafc", 700);
        name.position.set(bx + 6, top * FLOOR_H + 1.6, bz + 5);
        t3.group.add(name);
      }
      for (const u of data.units) {
        const f = data.floors.find(x => x.floor_id === u.floor_id); if (!f) continue;
        const b = data.buildings.find(x => x.building_id === f.building_id); if (!b) continue;
        const w = Number(u.plan_w) - 0.45, d = Number(u.plan_d) - 0.45, h = FLOOR_H - 1.1;
        const geo = new THREE.BoxGeometry(w, h, d);
        const level = levels.get(u.unit_id) || "clear";
        const mat = new THREE.MeshStandardMaterial({ color: COLOR[level], emissive: COLOR[level], emissiveIntensity: 0.12,
          transparent: true, opacity: opacityOf(level, u.unit_kind === "Shared"), depthWrite: level !== "clear", roughness: 0.45 });
        const mesh = new THREE.Mesh(geo, mat);
        const x = Number(b.site_x) + Number(u.plan_x) + Number(u.plan_w) / 2, z = Number(b.site_z) + Number(u.plan_z) + Number(u.plan_d) / 2;
        const y = (f.level - 1) * FLOOR_H + 0.18 + h / 2;
        mesh.position.set(x, y, z);
        mesh.userData.unitId = u.unit_id;
        const edges = new THREE.LineSegments(new THREE.EdgesGeometry(geo), new THREE.LineBasicMaterial({ color: "#94a3b8", transparent: true, opacity: 0.35 }));
        edges.position.copy(mesh.position);
        const label = textSprite(u.unit_label, u.unit_kind === "Shared" ? 0.5 : 0.62);
        label.position.set(x, y + h / 2 + 0.3, z);
        t3.group.add(mesh, edges, label);
        t3.units.set(u.unit_id, { mesh, edges, label, level, pulse: 0, shared: u.unit_kind === "Shared" });
      }
    } else {
      for (const [id, um] of t3.units) {
        const l = levels.get(id) || "clear";
        if (l !== um.level) {
          um.level = l; um.pulse = 1.2;
          const mat = um.mesh.material as THREE.MeshStandardMaterial;
          mat.color.set(COLOR[l]); mat.emissive.set(COLOR[l]);
          mat.opacity = opacityOf(l, um.shared); mat.depthWrite = l !== "clear"; mat.needsUpdate = true;
        }
      }
    }
  }, [data, levels]);

  // ------------------------------------------------------------------ actions
  const can = {
    work: ["Handyman", "Safety Inspector", "Property Manager"].includes(role),
    report: ["Tenant", "Property Manager"].includes(role),
  };
  const act = async (fn: () => Promise<any>) => {
    setBusy(true); setError(null);
    try {
      const r = await fn();
      if (r && typeof r === "object" && r.code === 0) setError(r.message || "Not saved.");
      await load(["tickets"]);
    } catch (e: any) { setError(e?.message || String(e)); }
    finally { setBusy(false); }
  };
  const setStatus = (t: Row, status: string) => act(() => updateRecord("tickets", t.ticket_id, { ticket_status: status }));

  const unit = data?.units.find(u => u.unit_id === selected);
  const unitTickets = (data?.tickets || []).filter(t => t.unit_id === selected && isOpen(t));
  const unitApps = (data?.appliances || []).filter(a => a.unit_id === selected);
  const counts = useMemo(() => {
    const c: Record<Level, number> = { hold: 0, urgent: 0, routine: 0, clear: 0 };
    for (const l of levels.values()) c[l]++;
    return c;
  }, [levels]);

  const [form, setForm] = useState({ title: "", severity: "Routine Maintenance", appliance_id: "" });
  const report = () => act(async () => {
    const r = await createRecord("tickets", { unit_id: selected, title: form.title, severity: form.severity, details: "",
      appliance_id: form.appliance_id || null, ticket_status: "Open" });
    if (r?.code !== 0) setForm({ title: "", severity: "Routine Maintenance", appliance_id: "" });
    return r;
  });

  return (
    <Box sx={wall ? { position: "fixed", inset: 0, zIndex: 1300, bgcolor: "background.default", p: 1.5, display: "flex", gap: 1.5 }
                  : { display: "flex", gap: 2, height: "calc(100vh - 150px)", minHeight: 520 }}>
      <Paper sx={{ position: "relative", flex: 1, overflow: "hidden", borderRadius: 3, bgcolor: "#0b1220" }}>
        <Box ref={host} sx={{ position: "absolute", inset: 0 }} />
        {!data && <CircularProgress sx={{ position: "absolute", top: "50%", left: "50%" }} />}
        <Stack direction="row" sx={{ position: "absolute", top: 12, left: 12, flexWrap: "wrap", gap: 1, maxWidth: "calc(100% - 250px)" }}>
          {(["hold", "urgent", "routine", "clear"] as Level[]).map(l => (
            <Chip key={l} size="small" label={`${LABEL[l]} · ${counts[l]}`}
              sx={{ bgcolor: "rgba(15,23,42,.85)", color: "#e2e8f0", border: `1px solid ${COLOR[l]}`, "& .MuiChip-label": { fontWeight: 600 } }}
              icon={<Box component="span" sx={{ width: 10, height: 10, borderRadius: "50%", bgcolor: COLOR[l], ml: "8px !important" }} />} />
          ))}
        </Stack>
        <Stack sx={{ position: "absolute", top: 12, right: 12, alignItems: "flex-end" }} spacing={0.5}>
          <Chip size="small" label={live === "open" ? `● Live · ${peers} ${peers === 1 ? "person" : "people"} here` : live === "connecting" ? "Connecting…" : "Offline, retrying"}
            sx={{ bgcolor: live === "open" ? "rgba(34,197,94,.15)" : "rgba(148,163,184,.15)", color: live === "open" ? "#4ade80" : "#cbd5e1", fontWeight: 600 }} />
          {sync && <Typography variant="caption" sx={{ color: "#94a3b8" }}>{sync}</Typography>}
          <Typography variant="caption" sx={{ color: "#64748b" }}>Signed in as {me?.username} ({role})</Typography>
          <Button size="small" onClick={() => setWall(w => !w)} sx={{ color: "#94a3b8", minWidth: 0, p: 0, fontSize: 12 }}>{wall ? "Exit wall display" : "Wall display"}</Button>
        </Stack>
        <Stack spacing={1} sx={{ position: "absolute", bottom: 14, left: 14 }}>
          {toasts.map(t => (
            <Paper key={t.id} sx={{ px: 1.5, py: 0.75, bgcolor: "rgba(15,23,42,.92)", color: "#f8fafc", borderLeft: `4px solid ${COLOR[t.level]}` }}>
              <Typography variant="body2">{t.text}</Typography>
            </Paper>
          ))}
        </Stack>
      </Paper>

      <Paper sx={{ width: wall ? 330 : 380, flexShrink: 0, p: 2.5, overflowY: "auto", borderRadius: 3 }}>
        {!unit ? (
          <>
            <Typography variant="h6" sx={{ fontWeight: 700 }}>Facility map</Typography>
            <Typography color="text.secondary" sx={{ mt: 1 }}>
              Each unit shows its worst open issue. Click a unit to see its appliances and tickets. Changes made by anyone appear here within a second.
            </Typography>
            {role === "Tenant" && <Alert severity="info" sx={{ mt: 2 }}>As a tenant you see your own unit only.</Alert>}
          </>
        ) : (
          <>
            <Typography variant="overline" color="text.secondary">{data?.buildings.find(b => b.building_id === data?.floors.find(f => f.floor_id === unit.floor_id)?.building_id)?.building_name}</Typography>
            <Stack direction="row" spacing={1} sx={{ alignItems: "center" }}>
              <Typography variant="h5" sx={{ fontWeight: 700 }}>{unit.unit_kind === "Shared" ? unit.unit_label : `Unit ${unit.unit_label}`}</Typography>
              <Chip size="small" label={LABEL[levels.get(unit.unit_id) || "clear"]}
                sx={{ bgcolor: COLOR[levels.get(unit.unit_id) || "clear"], color: "#111827", fontWeight: 700 }} />
            </Stack>
            <Typography variant="body2" color="text.secondary">
              {data?.floors.find(f => f.floor_id === unit.floor_id)?.floor_label}{unit.bedrooms ? ` · ${unit.bedrooms} bedroom${unit.bedrooms > 1 ? "s" : ""}` : ""}
            </Typography>
            {error && <Alert severity="error" sx={{ mt: 2 }} onClose={() => setError(null)}>{error}</Alert>}

            <Typography variant="subtitle2" sx={{ mt: 2.5, mb: 1 }}>Open tickets ({unitTickets.length})</Typography>
            {unitTickets.length === 0 && <Typography variant="body2" color="text.secondary">Nothing open.</Typography>}
            <Stack spacing={1.25}>
              {unitTickets.map(t => {
                const l = SEVERITY[t.severity] || "routine";
                const app = unitApps.find(a => a.appliance_id === t.appliance_id);
                return (
                  <Paper key={t.ticket_id} variant="outlined" sx={{ p: 1.5, borderLeft: `4px solid ${COLOR[l]}` }}>
                    <Typography variant="body2" sx={{ fontWeight: 600 }}>{t.title}</Typography>
                    <Typography variant="caption" color="text.secondary">
                      {t.ticket_number || `#${t.ticket_id}`} · {t.severity} · {t.ticket_status}{app ? ` · ${app.appliance_type}` : ""}
                    </Typography>
                    {can.work && (
                      <Stack direction="row" spacing={1} sx={{ mt: 1 }}>
                        {t.ticket_status === "Open" && <Button size="small" variant="outlined" disabled={busy} onClick={() => setStatus(t, "In progress")}>Start work</Button>}
                        {role !== "Safety Inspector" && <Button size="small" variant="contained" color="success" disabled={busy} onClick={() => setStatus(t, "Resolved")}>Mark resolved</Button>}
                        {role === "Safety Inspector" && <Button size="small" variant="contained" color={t.severity === "Safety Hold" ? "error" : "success"} disabled={busy}
                          onClick={() => setStatus(t, "Resolved")}>{t.severity === "Safety Hold" ? "Clear safety hold" : "Mark resolved"}</Button>}
                      </Stack>
                    )}
                  </Paper>
                );
              })}
            </Stack>

            <Typography variant="subtitle2" sx={{ mt: 2.5, mb: 1 }}>Appliances</Typography>
            <Stack spacing={0.75}>
              {unitApps.map(a => (
                <Stack key={a.appliance_id} direction="row" sx={{ justifyContent: "space-between", alignItems: "center" }}>
                  <Box>
                    <Typography variant="body2">{a.appliance_type}</Typography>
                    <Typography variant="caption" color="text.secondary">{a.make_model}</Typography>
                  </Box>
                  <Chip size="small" label={a.appliance_status} color={a.appliance_status === "Working" ? "success" : a.appliance_status === "Needs repair" ? "warning" : "error"} variant="outlined" />
                </Stack>
              ))}
            </Stack>

            {can.report && (
              <>
                <Typography variant="subtitle2" sx={{ mt: 2.5, mb: 1 }}>Report an issue</Typography>
                <Stack spacing={1.25}>
                  <TextField size="small" label="What's wrong?" value={form.title} onChange={e => setForm({ ...form, title: e.target.value })} />
                  <TextField size="small" select label="Severity" value={form.severity} onChange={e => setForm({ ...form, severity: e.target.value })}>
                    {Object.keys(SEVERITY).map(s => <MenuItem key={s} value={s}>{s}</MenuItem>)}
                  </TextField>
                  <TextField size="small" select label="Appliance (optional)" value={form.appliance_id} onChange={e => setForm({ ...form, appliance_id: e.target.value })}>
                    <MenuItem value="">None</MenuItem>
                    {unitApps.map(a => <MenuItem key={a.appliance_id} value={a.appliance_id}>{a.appliance_type}</MenuItem>)}
                  </TextField>
                  <Button variant="contained" disabled={busy || !form.title.trim()} onClick={report}>Report issue</Button>
                </Stack>
              </>
            )}
          </>
        )}
      </Paper>
    </Box>
  );
}
