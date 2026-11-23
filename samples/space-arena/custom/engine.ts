// SpaceArena engine: Three.js arena, ships, flight physics, collisions, lasers and hit effects.
// Rendering and simulation only; networking (net.ts) and sound (audio.ts) plug in through callbacks.
import * as THREE from "three";

export type Hull = "dart" | "wedge" | "orb";
export const HULLS: { id: Hull; label: string }[] = [
  { id: "dart", label: "Dart" }, { id: "wedge", label: "Wedge" }, { id: "orb", label: "Orb" },
];
export const COLORS = ["#22d3ee", "#f472b6", "#a3e635", "#facc15", "#fb923c", "#a78bfa"];

export interface ShipLook { name: string; color: string; hull: Hull }
export interface ShipState {
  p: [number, number, number]; q: [number, number, number, number]; v: [number, number, number];
  hp: number; thr: number; alive: boolean;
}
export interface Controls {
  throttle: number; // -1..1
  yaw: number; pitch: number; roll: number; // -1..1
  boost: boolean; fire: boolean;
}

const ARENA_RADIUS = 420;
const SHIP_RADIUS = 4.2;
const LASER_SPEED = 420;
const LASER_LIFE = 1.1; // seconds
const FIRE_COOLDOWN = 0.16;
const MAX_SPEED = 90, BOOST_SPEED = 150, ACCEL = 70, DAMPING = 0.6;
const TURN = { yaw: 1.6, pitch: 1.9, roll: 2.6 };
export const MAX_HP = 100;
const LASER_DAMAGE = 12, CRASH_DAMAGE = 8;

/** Deterministic random numbers so every player in a match builds the same asteroid field. */
export function seeded(seed: string) {
  let h = 2166136261;
  for (let i = 0; i < seed.length; i++) h = Math.imul(h ^ seed.charCodeAt(i), 16777619);
  return () => { h += 0x6d2b79f5; let t = h; t = Math.imul(t ^ (t >>> 15), t | 1); t ^= t + Math.imul(t ^ (t >>> 7), t | 61); return ((t ^ (t >>> 14)) >>> 0) / 4294967296; };
}

function buildShipMesh(look: ShipLook): THREE.Group {
  const g = new THREE.Group();
  const color = new THREE.Color(look.color);
  const body = new THREE.MeshStandardMaterial({ color: 0x9aa4b2, metalness: 0.75, roughness: 0.32 });
  const trim = new THREE.MeshStandardMaterial({ color, emissive: color, emissiveIntensity: 0.55, metalness: 0.4, roughness: 0.4 });
  const glass = new THREE.MeshStandardMaterial({ color: 0x0b1220, emissive: color.clone().multiplyScalar(0.25), metalness: 0.9, roughness: 0.1 });
  if (look.hull === "dart") {
    const nose = new THREE.Mesh(new THREE.ConeGeometry(1.4, 7, 10), body); nose.rotation.x = -Math.PI / 2; nose.position.z = -2; g.add(nose);
    const wing = new THREE.Mesh(new THREE.BoxGeometry(9, 0.25, 2.4), trim); wing.position.z = 1.2; g.add(wing);
    const fin = new THREE.Mesh(new THREE.BoxGeometry(0.25, 1.8, 2), trim); fin.position.set(0, 1, 1.6); g.add(fin);
    const cockpit = new THREE.Mesh(new THREE.SphereGeometry(0.9, 12, 8), glass); cockpit.position.set(0, 0.6, -1.2); cockpit.scale.z = 1.8; g.add(cockpit);
  } else if (look.hull === "wedge") {
    const shape = new THREE.Shape(); shape.moveTo(0, -4.5); shape.lineTo(4.2, 3); shape.lineTo(-4.2, 3); shape.lineTo(0, -4.5);
    const hullMesh = new THREE.Mesh(new THREE.ExtrudeGeometry(shape, { depth: 1.1, bevelEnabled: true, bevelSize: 0.3, bevelThickness: 0.3 }), body);
    hullMesh.rotation.x = Math.PI / 2; hullMesh.position.y = 0.55; g.add(hullMesh);
    const stripe = new THREE.Mesh(new THREE.BoxGeometry(0.6, 0.3, 6.5), trim); stripe.position.set(0, 0.8, -0.3); g.add(stripe);
    const cockpit = new THREE.Mesh(new THREE.SphereGeometry(0.8, 12, 8), glass); cockpit.position.set(0, 1, -0.8); cockpit.scale.z = 1.6; g.add(cockpit);
  } else {
    const core = new THREE.Mesh(new THREE.SphereGeometry(2.2, 20, 14), body); g.add(core);
    const ring = new THREE.Mesh(new THREE.TorusGeometry(3.4, 0.35, 10, 32), trim); ring.rotation.x = Math.PI / 2; g.add(ring);
    const eye = new THREE.Mesh(new THREE.SphereGeometry(0.9, 12, 8), glass); eye.position.z = -1.8; g.add(eye);
  }
  // engine glow, scaled with throttle
  const glow = new THREE.Mesh(new THREE.SphereGeometry(0.75, 10, 8), new THREE.MeshBasicMaterial({ color, transparent: true, opacity: 0.9 }));
  glow.position.z = look.hull === "orb" ? 2.4 : 3.4; glow.name = "glow"; g.add(glow);
  const light = new THREE.PointLight(color, 1.5, 20); light.position.copy(glow.position); light.name = "glowLight"; g.add(light);
  return g;
}

let dotTexture: THREE.Texture | null = null;
function dot(): THREE.Texture {
  if (dotTexture) return dotTexture;
  const c = document.createElement("canvas"); c.width = c.height = 64; const x = c.getContext("2d")!;
  const g = x.createRadialGradient(32, 32, 0, 32, 32, 32); g.addColorStop(0, "rgba(255,255,255,1)"); g.addColorStop(0.35, "rgba(255,255,255,0.75)"); g.addColorStop(1, "rgba(255,255,255,0)");
  x.fillStyle = g; x.fillRect(0, 0, 64, 64); dotTexture = new THREE.CanvasTexture(c); return dotTexture;
}

function nameSprite(text: string, color: string): THREE.Sprite {
  const c = document.createElement("canvas"); c.width = 256; c.height = 64;
  const x = c.getContext("2d")!;
  x.font = "bold 30px system-ui, sans-serif"; x.textAlign = "center"; x.textBaseline = "middle";
  x.fillStyle = "rgba(0,0,0,0.45)"; x.fillRect(0, 8, 256, 48);
  x.fillStyle = color; x.fillText(text, 128, 33);
  const s = new THREE.Sprite(new THREE.SpriteMaterial({ map: new THREE.CanvasTexture(c), depthTest: false, transparent: true }));
  s.scale.set(12, 3, 1); s.position.y = 6; s.renderOrder = 10;
  return s;
}

class Ship {
  group: THREE.Group; look: ShipLook;
  pos = new THREE.Vector3(); quat = new THREE.Quaternion(); vel = new THREE.Vector3();
  hp = MAX_HP; thr = 0; alive = true; cooldown = 0; shield = 0;
  // remote ships: target state for smoothing
  target?: { p: THREE.Vector3; q: THREE.Quaternion; v: THREE.Vector3; at: number };
  label?: THREE.Sprite;
  constructor(look: ShipLook, scene: THREE.Scene, labelled: boolean) {
    this.look = look; this.group = buildShipMesh(look); scene.add(this.group);
    if (labelled) { this.label = nameSprite(look.name, look.color); this.group.add(this.label); }
  }
  setLook(look: ShipLook, scene: THREE.Scene) {
    if (look.color === this.look.color && look.hull === this.look.hull && look.name === this.look.name) return;
    const labelled = !!this.label;
    scene.remove(this.group); this.group = buildShipMesh(look); this.look = look; scene.add(this.group);
    if (labelled) { this.label = nameSprite(look.name, look.color); this.group.add(this.label); }
  }
  sync() {
    this.group.position.copy(this.pos); this.group.quaternion.copy(this.quat); this.group.visible = this.alive;
    const glow = this.group.getObjectByName("glow") as THREE.Mesh; const l = this.group.getObjectByName("glowLight") as THREE.PointLight;
    const s = 0.6 + Math.max(0, this.thr) * 1.4; glow.scale.setScalar(s); l.intensity = 0.8 + Math.max(0, this.thr) * 3;
  }
}

interface Laser { mesh: THREE.Mesh; vel: THREE.Vector3; life: number; owner: string; id: string }
interface Burst { points: THREE.Points; vels: Float32Array; life: number; max: number; light?: THREE.PointLight }

export interface EngineEvents {
  onFire?: (p: number[], d: number[], id: string) => void;   // you fired
  onHitBy?: (shooter: string, hp: number) => void;            // a remote laser hit you
  onDied?: (killer: string | null) => void;                   // you were destroyed
  onSpark?: () => void;                                       // your laser hit something
  onCrash?: () => void;                                       // you bumped an asteroid or ship
  onExplosion?: (near: boolean) => void;                      // a ship exploded
}

export class Engine {
  renderer: THREE.WebGLRenderer; scene = new THREE.Scene(); camera: THREE.PerspectiveCamera;
  me: Ship; others = new Map<string, Ship>();
  asteroids: { mesh: THREE.Mesh; r: number; spin: THREE.Vector3 }[] = [];
  lasers: Laser[] = []; bursts: Burst[] = [];
  controls: Controls = { throttle: 0, yaw: 0, pitch: 0, roll: 0, boost: false, fire: false };
  events: EngineEvents = {};
  myId = "me"; deadUntil = 0; shake = 0; flash = 0;
  /** Render resolution scale: drops on slow devices to hold ~60 FPS, recovers when there is headroom. */
  quality = 1; remoteShots = 0; renderEnabled = true; thrOverride: number | null = null;
  /** Film mode: time advances only through advance(dt), so footage is smooth on any machine. */
  simTime: number | null = null;
  /** Optional camera override (return true to skip the chase camera). */
  cinema?: (cam: THREE.PerspectiveCamera, t: number) => boolean;
  now() { return this.simTime ?? performance.now(); }
  advance(dt: number) { if (this.simTime === null) this.simTime = performance.now(); this.simTime += dt * 1000; this.frame(dt); } private basePR = 1;
  // stats
  fps = 0; private frames = 0; private fpsAt = this.now(); frameTimes: number[] = [];
  private last = this.now(); private raf = 0; private laserSeq = 0;
  private laserGeo = new THREE.CylinderGeometry(0.32, 0.32, 7, 6).rotateX(Math.PI / 2);
  private tmp = new THREE.Vector3(); private tmpQ = new THREE.Quaternion();

  constructor(private canvas: HTMLCanvasElement, look: ShipLook, matchSeed: string) {
    this.renderer = new THREE.WebGLRenderer({ canvas, antialias: true, powerPreference: "high-performance" });
    this.basePR = Math.min(window.devicePixelRatio || 1, 2); this.renderer.setPixelRatio(this.basePR);
    this.renderer.toneMapping = THREE.ACESFilmicToneMapping;
    this.camera = new THREE.PerspectiveCamera(70, 1, 0.5, 3000);
    this.scene.background = new THREE.Color(0x02030a);
    this.scene.fog = new THREE.FogExp2(0x070a1a, 0.0016);
    this.scene.add(new THREE.AmbientLight(0x5566aa, 0.55));
    const sun = new THREE.DirectionalLight(0xfff1d6, 2.2); sun.position.set(300, 200, 100); this.scene.add(sun);
    this.buildArena(matchSeed);
    this.me = new Ship(look, this.scene, false);
    this.respawn();
    this.resize();
  }

  private buildArena(seed: string) {
    const rnd = seeded(seed);
    // stars
    const n = 2500, pts = new Float32Array(n * 3);
    for (let i = 0; i < n; i++) {
      const v = new THREE.Vector3(rnd() - 0.5, rnd() - 0.5, rnd() - 0.5).normalize().multiplyScalar(1400 + rnd() * 600);
      pts.set([v.x, v.y, v.z], i * 3);
    }
    const sg = new THREE.BufferGeometry(); sg.setAttribute("position", new THREE.BufferAttribute(pts, 3));
    this.scene.add(new THREE.Points(sg, new THREE.PointsMaterial({ color: 0xffffff, size: 2.2, sizeAttenuation: false, fog: false })));
    // boundary: a faint wireframe sphere
    const bound = new THREE.Mesh(new THREE.IcosahedronGeometry(ARENA_RADIUS, 3),
      new THREE.MeshBasicMaterial({ color: 0x3b82f6, wireframe: true, transparent: true, opacity: 0.07 }));
    this.scene.add(bound);
    // asteroids (same for everyone in the match)
    const rockMat = new THREE.MeshStandardMaterial({ color: 0x6b6258, roughness: 0.95, metalness: 0.05, flatShading: true });
    for (let i = 0; i < 34; i++) {
      const r = 6 + rnd() * 18;
      const geo = new THREE.IcosahedronGeometry(r, 1);
      const pos = geo.attributes.position as THREE.BufferAttribute;
      for (let k = 0; k < pos.count; k++) { const f = 0.8 + rnd() * 0.4; pos.setXYZ(k, pos.getX(k) * f, pos.getY(k) * f, pos.getZ(k) * f); }
      geo.computeVertexNormals();
      const m = new THREE.Mesh(geo, rockMat);
      let p: THREE.Vector3;
      do { p = new THREE.Vector3(rnd() - 0.5, rnd() - 0.5, rnd() - 0.5).multiplyScalar(ARENA_RADIUS * 1.6); } while (p.length() > ARENA_RADIUS - 40 || p.length() < 60);
      m.position.copy(p); m.rotation.set(rnd() * 6, rnd() * 6, rnd() * 6);
      this.scene.add(m);
      this.asteroids.push({ mesh: m, r: r * 0.92, spin: new THREE.Vector3(rnd() - 0.5, rnd() - 0.5, rnd() - 0.5).multiplyScalar(0.3) });
    }
    // a distant glowing planet for depth
    const planet = new THREE.Mesh(new THREE.SphereGeometry(260, 40, 30),
      new THREE.MeshStandardMaterial({ color: 0x2a3a7a, emissive: 0x0b1440, roughness: 1 }));
    planet.position.set(-900, -300, -1300); this.scene.add(planet);
  }

  respawn() {
    const a = Math.random() * Math.PI * 2;
    this.me.pos.set(Math.cos(a) * 180, (Math.random() - 0.5) * 60, Math.sin(a) * 180);
    this.me.quat.setFromRotationMatrix(new THREE.Matrix4().lookAt(this.me.pos, new THREE.Vector3(0, 0, 0), new THREE.Vector3(0, 1, 0)));
    this.me.vel.set(0, 0, 0); this.me.hp = MAX_HP; this.me.alive = true; this.me.shield = 2.5;
  }

  setLook(look: ShipLook) { this.me.setLook(look, this.scene); }

  private adapt() {
    const q = this.fps < 50 ? Math.max(0.35, this.quality * 0.8) : this.fps >= 59 && this.quality < 1 ? Math.min(1, this.quality * 1.1) : this.quality;
    if (Math.abs(q - this.quality) > 0.01) { this.quality = q; this.renderer.setPixelRatio(this.basePR * q); this.resize(); }
  }

  resize() {
    const w = this.canvas.clientWidth || window.innerWidth, h = this.canvas.clientHeight || window.innerHeight;
    this.renderer.setSize(w, h, false); this.camera.aspect = w / h; this.camera.updateProjectionMatrix();
  }

  start() { this.last = this.now(); const loop = () => { this.frame(); this.raf = requestAnimationFrame(loop); }; this.raf = requestAnimationFrame(loop); }
  stop() { cancelAnimationFrame(this.raf); this.renderer.dispose(); }
  pause() { cancelAnimationFrame(this.raf); }

  myState(): ShipState {
    const m = this.me;
    return { p: [m.pos.x, m.pos.y, m.pos.z], q: [m.quat.x, m.quat.y, m.quat.z, m.quat.w], v: [m.vel.x, m.vel.y, m.vel.z], hp: Math.round(m.hp), thr: +m.thr.toFixed(2), alive: m.alive };
  }

  // ---------------------------------------------------------------- remote players

  upsertRemote(id: string, look: ShipLook, s: ShipState) {
    let ship = this.others.get(id);
    if (!ship) { ship = new Ship(look, this.scene, true); ship.pos.fromArray(s.p); ship.quat.fromArray(s.q); this.others.set(id, ship); }
    else ship.setLook(look, this.scene);
    ship.target = { p: new THREE.Vector3().fromArray(s.p), q: new THREE.Quaternion().fromArray(s.q), v: new THREE.Vector3().fromArray(s.v), at: this.now() };
    ship.hp = s.hp; ship.thr = s.thr;
    if (ship.alive && !s.alive) this.explode(ship.pos, ship.look.color);
    ship.alive = s.alive;
  }
  removeRemote(id: string) { const s = this.others.get(id); if (s) { this.scene.remove(s.group); this.others.delete(id); } }

  /** A remote player fired: simulate the laser here; it can hit you (you decide your own damage). */
  remoteFire(owner: string, p: number[], d: number[], id: string) {
    this.remoteShots++;
    const color = this.others.get(owner)?.look.color ?? "#ff4d4d";
    this.spawnLaser(new THREE.Vector3().fromArray(p), new THREE.Vector3().fromArray(d), owner, id, color);
  }
  /** A remote ship told us it was hit by our laser: show it on that ship. */
  remoteHit(target: string) { const s = this.others.get(target); if (s) this.sparks(s.pos, s.look.color, 18); }

  // ---------------------------------------------------------------- simulation

  private spawnLaser(origin: THREE.Vector3, dir: THREE.Vector3, owner: string, id: string, color: string) {
    const mesh = new THREE.Mesh(this.laserGeo, new THREE.MeshBasicMaterial({ color, toneMapped: false }));
    mesh.position.copy(origin); mesh.quaternion.setFromUnitVectors(new THREE.Vector3(0, 0, -1), dir);
    this.scene.add(mesh);
    this.lasers.push({ mesh, vel: dir.clone().multiplyScalar(LASER_SPEED), life: LASER_LIFE, owner, id });
  }

  sparks(at: THREE.Vector3, color: string, count: number) { this.burst(at, color, count, 30, 0.45, 1.6); }
  explode(at: THREE.Vector3, color: string) {
    this.burst(at, color, 220, 70, 1.6, 3.2); this.burst(at, "#ffd27a", 120, 45, 1.1, 2.6);
    const light = new THREE.PointLight(0xffb060, 60, 160, 1.6); light.position.copy(at); this.scene.add(light);
    this.bursts[this.bursts.length - 1].light = light;
    const near = at.distanceTo(this.me.pos) < 160; if (near) this.shake = 1;
    this.events.onExplosion?.(near);
  }
  private burst(at: THREE.Vector3, color: string, count: number, speed: number, life: number, size: number) {
    const pos = new Float32Array(count * 3), vels = new Float32Array(count * 3);
    for (let i = 0; i < count; i++) {
      pos.set([at.x, at.y, at.z], i * 3);
      const v = new THREE.Vector3(Math.random() - 0.5, Math.random() - 0.5, Math.random() - 0.5).normalize().multiplyScalar(speed * (0.3 + Math.random()));
      vels.set([v.x, v.y, v.z], i * 3);
    }
    const g = new THREE.BufferGeometry(); g.setAttribute("position", new THREE.BufferAttribute(pos, 3));
    const pts = new THREE.Points(g, new THREE.PointsMaterial({ color, size: size * 1.6, map: dot(), transparent: true, opacity: 1, blending: THREE.AdditiveBlending, depthWrite: false, toneMapped: false }));
    this.scene.add(pts); this.bursts.push({ points: pts, vels, life, max: life });
  }

  damageMe(amount: number, by: string | null) {
    const m = this.me;
    if (!m.alive || m.shield > 0) return;
    m.hp = Math.max(0, m.hp - amount); this.flash = 1; this.shake = Math.max(this.shake, 0.5);
    if (by) this.events.onHitBy?.(by, m.hp);
    if (m.hp <= 0) {
      m.alive = false; this.explode(m.pos, m.look.color); this.deadUntil = this.now() + 3000;
      this.events.onDied?.(by);
    }
  }

  private step(dt: number) {
    const m = this.me, c = this.controls;
    if (!m.alive && this.now() > this.deadUntil) this.respawn();
    if (!m.alive) { /* wait for respawn */ }
    if (m.alive) {
      // orientation: yaw (Y), pitch (X), roll (Z) in the ship's own frame
      this.tmpQ.setFromEuler(new THREE.Euler(c.pitch * TURN.pitch * dt, -c.yaw * TURN.yaw * dt, -c.roll * TURN.roll * dt, "YXZ"));
      m.quat.multiply(this.tmpQ).normalize();
      const forward = new THREE.Vector3(0, 0, -1).applyQuaternion(m.quat);
      m.thr = this.thrOverride ?? c.throttle;
      const max = c.boost && c.throttle > 0 ? BOOST_SPEED : MAX_SPEED;
      m.vel.addScaledVector(forward, c.throttle * ACCEL * (c.boost ? 1.8 : 1) * dt);
      m.vel.multiplyScalar(Math.exp(-DAMPING * dt * (c.throttle === 0 ? 1.4 : 0.6)));
      if (m.vel.length() > max) m.vel.setLength(max);
      m.pos.addScaledVector(m.vel, dt);
      // fire
      m.cooldown -= dt;
      if (c.fire && m.cooldown <= 0) {
        m.cooldown = FIRE_COOLDOWN;
        const right = new THREE.Vector3(1, 0, 0).applyQuaternion(m.quat);
        for (const side of [-1, 1]) {
          const origin = m.pos.clone().addScaledVector(forward, 5).addScaledVector(right, side * 2.2);
          const dir = forward.clone().add(m.vel.clone().multiplyScalar(1 / LASER_SPEED)).normalize();
          const id = `${this.myId}:${++this.laserSeq}`;
          this.spawnLaser(origin, dir, this.myId, id, m.look.color);
          this.events.onFire?.([origin.x, origin.y, origin.z], [dir.x, dir.y, dir.z], id);
        }
      }
      // arena boundary: push back in
      const dist = m.pos.length();
      if (dist > ARENA_RADIUS - SHIP_RADIUS) {
        const n = m.pos.clone().normalize();
        m.pos.setLength(ARENA_RADIUS - SHIP_RADIUS);
        const vn = m.vel.dot(n); if (vn > 0) m.vel.addScaledVector(n, -1.6 * vn);
      }
      // asteroids: bounce, damage
      for (const a of this.asteroids) {
        const d = this.tmp.subVectors(m.pos, a.mesh.position); const min = a.r + SHIP_RADIUS;
        if (d.lengthSq() < min * min) {
          const n = d.normalize(); m.pos.copy(a.mesh.position).addScaledVector(n, min);
          const vn = m.vel.dot(n); if (vn < 0) { m.vel.addScaledVector(n, -1.7 * vn); if (-vn > 12) { this.damageMe(CRASH_DAMAGE, null); this.events.onCrash?.(); } }
        }
      }
      // other ships: each side moves only itself apart
      for (const o of this.others.values()) {
        if (!o.alive) continue;
        const d = this.tmp.subVectors(m.pos, o.pos); const min = SHIP_RADIUS * 2;
        if (d.lengthSq() < min * min && d.lengthSq() > 1e-6) {
          const n = d.normalize(); m.pos.copy(o.pos).addScaledVector(n, min);
          const vn = m.vel.dot(n); if (vn < 0) { m.vel.addScaledVector(n, -1.5 * vn); this.damageMe(CRASH_DAMAGE, null); this.events.onCrash?.(); }
        }
      }
      if (m.shield > 0) m.shield -= dt;
    }
    // remote ships: dead reckoning toward the latest state, smoothed
    for (const o of this.others.values()) {
      if (!o.target) continue;
      const age = (this.now() - o.target.at) / 1000;
      const predicted = o.target.p.clone().addScaledVector(o.target.v, Math.min(age, 0.25));
      o.pos.lerp(predicted, 1 - Math.exp(-14 * dt));
      o.quat.slerp(o.target.q, 1 - Math.exp(-14 * dt));
    }
    // lasers
    for (let i = this.lasers.length - 1; i >= 0; i--) {
      const L = this.lasers[i];
      const prev = L.mesh.position.clone();
      L.mesh.position.addScaledVector(L.vel, dt); L.life -= dt;
      let hit = false;
      // vs asteroids
      for (const a of this.asteroids) if (segmentHitsSphere(prev, L.mesh.position, a.mesh.position, a.r)) { hit = true; this.sparks(L.mesh.position, "#ffb070", 10); if (L.owner === this.myId) this.events.onSpark?.(); break; }
      // remote lasers vs me (I decide my own damage)
      if (!hit && L.owner !== this.myId && m.alive && segmentHitsSphere(prev, L.mesh.position, m.pos, SHIP_RADIUS)) {
        hit = true; this.sparks(m.pos, m.look.color, 16); this.damageMe(LASER_DAMAGE, L.owner);
      }
      // my lasers vs remote ships: visual only (they decide their damage and tell us)
      if (!hit && L.owner === this.myId) for (const o of this.others.values())
        if (o.alive && segmentHitsSphere(prev, L.mesh.position, o.pos, SHIP_RADIUS)) { hit = true; this.sparks(o.pos, o.look.color, 16); this.events.onSpark?.(); break; }
      if (hit || L.life <= 0) { this.scene.remove(L.mesh); (L.mesh.material as THREE.Material).dispose(); this.lasers.splice(i, 1); }
    }
    // particles
    for (let i = this.bursts.length - 1; i >= 0; i--) {
      const b = this.bursts[i]; b.life -= dt;
      const pos = b.points.geometry.attributes.position as THREE.BufferAttribute;
      for (let k = 0; k < pos.count; k++) pos.setXYZ(k, pos.getX(k) + b.vels[k * 3] * dt, pos.getY(k) + b.vels[k * 3 + 1] * dt, pos.getZ(k) + b.vels[k * 3 + 2] * dt);
      pos.needsUpdate = true; (b.points.material as THREE.PointsMaterial).opacity = Math.max(0, b.life / b.max);
      if (b.light) b.light.intensity = 60 * Math.pow(Math.max(0, b.life / b.max), 3);
      if (b.life <= 0) { if (b.light) this.scene.remove(b.light); this.scene.remove(b.points); b.points.geometry.dispose(); (b.points.material as THREE.Material).dispose(); this.bursts.splice(i, 1); }
    }
    for (const a of this.asteroids) { a.mesh.rotation.x += a.spin.x * dt; a.mesh.rotation.y += a.spin.y * dt; }
  }

  private frame(fixed?: number) {
    const now = this.now(); const dt = fixed ?? Math.min(0.05, (now - this.last) / 1000); this.last = now;
    this.frameTimes.push(now); while (this.frameTimes.length && now - this.frameTimes[0] > 1000) this.frameTimes.shift();
    this.frames++; if (now - this.fpsAt >= 500) { this.fps = Math.round((this.frames * 1000) / (now - this.fpsAt)); this.frames = 0; this.fpsAt = now; if (this.simTime === null) this.adapt(); }
    this.step(dt);
    this.me.sync(); for (const o of this.others.values()) o.sync();
    this.me.group.visible = this.me.alive && !(this.me.shield > 0 && Math.floor(now / 90) % 2 === 0);
    const m = this.me;
    if (this.cinema && this.cinema(this.camera, now / 1000)) { this.flash = Math.max(0, this.flash - dt * 3); if (this.renderEnabled) this.renderer.render(this.scene, this.camera); return; }
    // chase camera
    const back = new THREE.Vector3(0, 4.5, 16).applyQuaternion(m.quat);
    const want = m.pos.clone().add(back);
    this.camera.position.lerp(want, 1 - Math.exp(-10 * dt));
    const up = new THREE.Vector3(0, 1, 0).applyQuaternion(m.quat);
    this.camera.up.lerp(up, 1 - Math.exp(-8 * dt));
    this.camera.lookAt(m.pos.clone().add(new THREE.Vector3(0, 0, -30).applyQuaternion(m.quat)));
    if (this.shake > 0) { this.camera.position.add(new THREE.Vector3((Math.random() - 0.5) * this.shake * 2, (Math.random() - 0.5) * this.shake * 2, 0)); this.shake = Math.max(0, this.shake - dt * 2.5); }
    this.flash = Math.max(0, this.flash - dt * 3);
    if (this.renderEnabled) this.renderer.render(this.scene, this.camera);
  }
}

function segmentHitsSphere(a: THREE.Vector3, b: THREE.Vector3, c: THREE.Vector3, r: number): boolean {
  const ab = new THREE.Vector3().subVectors(b, a); const ac = new THREE.Vector3().subVectors(c, a);
  const t = Math.max(0, Math.min(1, ac.dot(ab) / Math.max(ab.lengthSq(), 1e-9)));
  const closest = a.clone().addScaledVector(ab, t);
  return closest.distanceToSquared(c) <= r * r;
}
