// Procedural sound with Web Audio: thruster, lasers, hits, explosions, and music that follows combat intensity.
// No audio files. Everything is synthesized when the page runs.

export class GameAudio {
  ctx: AudioContext | null = null;
  private master!: GainNode; private sfx!: GainNode; private musicBus!: GainNode;
  private thrGain!: GainNode; private thrFilter!: BiquadFilterNode;
  private noise!: AudioBuffer;
  private layers: { gain: GainNode; level: (i: number) => number }[] = [];
  private nextBeat = 0; private beat = 0; private timer = 0;
  muted = false;
  /** 0 calm … 1 heavy combat; rises on shots/hits, decays over time. */
  intensity = 0;
  stats = { lasers: 0, explosions: 0, hits: 0 };

  /** Must be called from a user gesture (browsers block audio until then). */
  /** Film soundtrack: when set, sounds are scheduled at this time instead of "now". */
  atTime: number | null = null;
  private t() { return this.atTime ?? this.ctx!.currentTime; }

  start(offline?: BaseAudioContext) {
    if (this.ctx) { (this.ctx as AudioContext).resume?.(); return; }
    const C = (window.AudioContext || (window as any).webkitAudioContext) as typeof AudioContext;
    const ctx = (offline ?? new C()) as AudioContext; this.ctx = ctx;
    this.master = ctx.createGain(); this.master.gain.value = this.muted ? 0 : 0.8;
    const comp = ctx.createDynamicsCompressor(); comp.threshold.value = -14; comp.ratio.value = 4;
    this.master.connect(comp).connect(ctx.destination);
    this.sfx = ctx.createGain(); this.sfx.gain.value = 0.9; this.sfx.connect(this.master);
    this.musicBus = ctx.createGain(); this.musicBus.gain.value = 0.35; this.musicBus.connect(this.master);
    // white noise buffer, reused
    this.noise = ctx.createBuffer(1, ctx.sampleRate * 2, ctx.sampleRate);
    const d = this.noise.getChannelData(0); for (let i = 0; i < d.length; i++) d[i] = Math.random() * 2 - 1;
    // thruster: looping noise + low saw through a lowpass, volume from throttle
    const n = ctx.createBufferSource(); n.buffer = this.noise; n.loop = true;
    const saw = ctx.createOscillator(); saw.type = "sawtooth"; saw.frequency.value = 46;
    const sawG = ctx.createGain(); sawG.gain.value = 0.25;
    this.thrFilter = ctx.createBiquadFilter(); this.thrFilter.type = "lowpass"; this.thrFilter.frequency.value = 300; this.thrFilter.Q.value = 2;
    this.thrGain = ctx.createGain(); this.thrGain.gain.value = 0;
    n.connect(this.thrFilter); saw.connect(sawG).connect(this.thrFilter); this.thrFilter.connect(this.thrGain).connect(this.sfx);
    n.start(); saw.start();
    // music layers: pad (always), bass (intensity > .2), drums (> .45), lead arpeggio (> .7)
    const layer = (level: (i: number) => number) => { const g = ctx.createGain(); g.gain.value = 0; g.connect(this.musicBus); this.layers.push({ gain: g, level }); return g; };
    this.padBus = layer(() => 0.55);
    this.bassBus = layer(i => (i > 0.2 ? Math.min(1, (i - 0.2) * 3) : 0));
    this.drumBus = layer(i => (i > 0.45 ? Math.min(1, (i - 0.45) * 3) : 0));
    this.leadBus = layer(i => (i > 0.7 ? Math.min(1, (i - 0.7) * 4) : 0));
    this.nextBeat = ctx.currentTime + 0.1;
    if (!offline) this.timer = window.setInterval(() => this.schedule(), 50);
  }
  private padBus!: GainNode; private bassBus!: GainNode; private drumBus!: GainNode; private leadBus!: GainNode;

  setMuted(m: boolean) {
    this.muted = m;
    if (this.ctx) this.master.gain.setTargetAtTime(m ? 0 : 0.8, this.ctx.currentTime, 0.03);
  }
  /** Current output level of the master bus (0 when muted). */
  get masterLevel() { return this.ctx ? this.master.gain.value : 0; }
  stop() { clearInterval(this.timer); (this.ctx as AudioContext)?.close?.(); this.ctx = null; }

  /** Film soundtrack: schedule the music for `seconds`, with combat intensity given per second. */
  scoreOffline(seconds: number, intensityAt: (t: number) => number) {
    const c = this.ctx!; let t = 0.05, i = 0;
    for (let s = 0; s < seconds; s += 0.25) for (const l of this.layers) l.gain.gain.setTargetAtTime(l.level(intensityAt(s)), s, 0.4);
    while (t < seconds) { const bpm = 96 + intensityAt(t) * 44, step = 60 / bpm / 2; this.playStep(i, t, step); i = (i + 1) % 32; t += step; }
    this.thrGain.gain.setValueAtTime(0.12, 0); this.thrFilter.frequency.setValueAtTime(700, 0);
    void c;
  }

  thruster(throttle: number, boost: boolean) {
    if (!this.ctx) return; const t = this.ctx.currentTime; const a = Math.max(0, throttle);
    this.thrGain.gain.setTargetAtTime(0.04 + a * (boost ? 0.5 : 0.32), t, 0.08);
    this.thrFilter.frequency.setTargetAtTime(220 + a * (boost ? 1800 : 1000), t, 0.1);
  }

  laser(pan = 0) {
    if (!this.ctx) return; const c = this.ctx, t = this.t(); this.stats.lasers++; this.bump(0.05);
    const o = c.createOscillator(); o.type = "square";
    o.frequency.setValueAtTime(1500 + Math.random() * 200, t); o.frequency.exponentialRampToValueAtTime(180, t + 0.16);
    const g = c.createGain(); g.gain.setValueAtTime(0.18, t); g.gain.exponentialRampToValueAtTime(0.001, t + 0.18);
    const f = c.createBiquadFilter(); f.type = "bandpass"; f.frequency.value = 1400; f.Q.value = 0.8;
    const p = c.createStereoPanner(); p.pan.value = pan;
    o.connect(f).connect(g).connect(p).connect(this.sfx); o.start(t); o.stop(t + 0.2);
  }

  hit() {
    if (!this.ctx) return; const c = this.ctx, t = this.t(); this.stats.hits++; this.bump(0.12);
    const s = c.createBufferSource(); s.buffer = this.noise;
    const f = c.createBiquadFilter(); f.type = "highpass"; f.frequency.value = 1800;
    const g = c.createGain(); g.gain.setValueAtTime(0.35, t); g.gain.exponentialRampToValueAtTime(0.001, t + 0.12);
    s.connect(f).connect(g).connect(this.sfx); s.start(t, Math.random()); s.stop(t + 0.14);
    const o = c.createOscillator(); o.frequency.setValueAtTime(220, t); o.frequency.exponentialRampToValueAtTime(60, t + 0.12);
    const og = c.createGain(); og.gain.setValueAtTime(0.25, t); og.gain.exponentialRampToValueAtTime(0.001, t + 0.14);
    o.connect(og).connect(this.sfx); o.start(t); o.stop(t + 0.15);
  }

  explosion(near: boolean) {
    if (!this.ctx) return; const c = this.ctx, t = this.t(); this.stats.explosions++; this.bump(0.35);
    const vol = near ? 0.9 : 0.35;
    const s = c.createBufferSource(); s.buffer = this.noise;
    const f = c.createBiquadFilter(); f.type = "lowpass"; f.frequency.setValueAtTime(3000, t); f.frequency.exponentialRampToValueAtTime(80, t + 1.4);
    const g = c.createGain(); g.gain.setValueAtTime(vol, t); g.gain.exponentialRampToValueAtTime(0.001, t + 1.6);
    s.connect(f).connect(g).connect(this.sfx); s.start(t); s.stop(t + 1.7);
    const o = c.createOscillator(); o.type = "sine"; o.frequency.setValueAtTime(120, t); o.frequency.exponentialRampToValueAtTime(28, t + 0.9);
    const og = c.createGain(); og.gain.setValueAtTime(vol, t); og.gain.exponentialRampToValueAtTime(0.001, t + 1);
    o.connect(og).connect(this.sfx); o.start(t); o.stop(t + 1.05);
  }

  /** Raise combat intensity (clamped to 1). */
  bump(x: number) { this.intensity = Math.min(1, this.intensity + x); }
  /** Which music layers are currently audible (for the HUD and tests). */
  activeLayers(): string[] {
    const names = ["pad", "bass", "drums", "lead"];
    return this.layers.map((l, i) => (l.level(this.intensity) > 0.05 ? names[i] : "")).filter(Boolean);
  }

  // ---------------------------------------------------------------- music sequencer
  private schedule() {
    const c = this.ctx; if (!c) return;
    this.intensity = Math.max(0, this.intensity - 0.012); // decays ~0.24/s
    const now = c.currentTime;
    for (const l of this.layers) l.gain.gain.setTargetAtTime(l.level(this.intensity), now, 0.4);
    const bpm = 96 + this.intensity * 44; const step = 60 / bpm / 2; // eighth notes
    while (this.nextBeat < now + 0.15) { this.playStep(this.beat, this.nextBeat, step); this.beat = (this.beat + 1) % 32; this.nextBeat += step; }
  }
  private note(freq: number, t: number, dur: number, type: OscillatorType, vol: number, bus: GainNode) {
    const c = this.ctx!; const o = c.createOscillator(); o.type = type; o.frequency.value = freq;
    const g = c.createGain(); g.gain.setValueAtTime(0.0001, t); g.gain.exponentialRampToValueAtTime(vol, t + 0.01); g.gain.exponentialRampToValueAtTime(0.0001, t + dur);
    o.connect(g).connect(bus); o.start(t); o.stop(t + dur + 0.02);
  }
  private playStep(i: number, t: number, step: number) {
    // A minor progression: Am – F – C – G, 8 steps each
    const roots = [110, 87.31, 130.81, 98];
    const chord = Math.floor(i / 8); const r = roots[chord];
    if (i % 8 === 0) for (const m of [1, 1.5, 2.0, 2.52]) this.note(r * 2 * m, t, step * 8, "triangle", 0.05, this.padBus);
    if (i % 2 === 0) this.note(r, t, step * 1.6, "sawtooth", 0.16, this.bassBus);
    if (i % 4 === 0) this.kick(t);
    if (i % 4 === 2) this.snare(t);
    this.hat(t);
    const arp = [1, 1.2, 1.5, 2, 2.4, 2, 1.5, 1.2];
    this.note(r * 4 * arp[i % 8], t, step * 0.9, "square", 0.05, this.leadBus);
  }
  private kick(t: number) {
    const c = this.ctx!; const o = c.createOscillator(); o.frequency.setValueAtTime(150, t); o.frequency.exponentialRampToValueAtTime(40, t + 0.12);
    const g = c.createGain(); g.gain.setValueAtTime(0.7, t); g.gain.exponentialRampToValueAtTime(0.001, t + 0.18);
    o.connect(g).connect(this.drumBus); o.start(t); o.stop(t + 0.2);
  }
  private snare(t: number) {
    const c = this.ctx!; const s = c.createBufferSource(); s.buffer = this.noise;
    const f = c.createBiquadFilter(); f.type = "bandpass"; f.frequency.value = 1800;
    const g = c.createGain(); g.gain.setValueAtTime(0.35, t); g.gain.exponentialRampToValueAtTime(0.001, t + 0.14);
    s.connect(f).connect(g).connect(this.drumBus); s.start(t, Math.random()); s.stop(t + 0.15);
  }
  private hat(t: number) {
    const c = this.ctx!; const s = c.createBufferSource(); s.buffer = this.noise;
    const f = c.createBiquadFilter(); f.type = "highpass"; f.frequency.value = 7000;
    const g = c.createGain(); g.gain.setValueAtTime(0.08, t); g.gain.exponentialRampToValueAtTime(0.001, t + 0.04);
    s.connect(f).connect(g).connect(this.drumBus); s.start(t, Math.random()); s.stop(t + 0.05);
  }
}
