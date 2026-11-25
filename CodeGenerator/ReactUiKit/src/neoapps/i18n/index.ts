// Translation for the whole UI: built-in screens, the app's own words (ui.config "i18n") and custom screens.
//
//   t("Save")                          -> "Enregistrer" (French)
//   t("Hello {name}", { name })        -> placeholders in braces
//   t("{count} rows", { count: 3 })    -> plural forms: a locale value may be { "one": "...", "other": "..." }
//
// Keys are the English text itself, so untranslated text simply shows in English. Built-in translations live in
// locales/<code>.json (loaded on demand); the app's own words come from ui.config "i18n.translations" and win over
// the built-in ones. Changing the language re-renders the app (NeoApp keys its routes on the language), so plain
// t() calls are enough in components; useT() is there for components that live outside that tree.
import { createElement, Fragment, isValidElement, ReactNode, useSyncExternalStore } from "react";
import { LANGUAGES, matchLanguage } from "./languages";

export { LANGUAGES, matchLanguage } from "./languages";
export type { Language } from "./languages";

type Msg = string | Partial<Record<Intl.LDMLPluralRule, string>>;
type Dict = Record<string, Msg>;

const STORAGE_KEY = "neoapps.language";
const loaders = import.meta.glob<{ default: Dict }>("./locales/*.json");

let current = "en";
let builtIn: Dict = {};
let appWords: Record<string, Dict> = {};
let appLanguages: string[] | null = null;
let appDefault = "en";
let version = 0;
const listeners = new Set<() => void>();
const notify = () => { version++; listeners.forEach((l) => l()); };

export const language = () => current;
/** Changes whenever the language or the app's translations change (for memo dependencies). */
export const i18nVersion = () => version;
export const isRtl = (code = current) => LANGUAGES.find((l) => l.code === code)?.dir === "rtl";
/** BCP 47 locale for Intl (dates, numbers, currency). */
export const locale = () => current;

/** Languages this app offers: ui.config i18n.languages, else every built-in language. */
export function availableLanguages() {
  const codes = appLanguages && appLanguages.length ? appLanguages : LANGUAGES.map((l) => l.code);
  return codes.map((c) => LANGUAGES.find((l) => l.code === c) || { code: c, name: c, english: c }).filter(Boolean);
}

function pick(msg: Msg | undefined, vars?: Record<string, any>): string | undefined {
  if (msg === undefined || msg === null) return undefined;
  if (typeof msg === "string") return msg;
  const n = Number(vars?.count ?? vars?.n);
  const rule = isNaN(n) ? "other" : new Intl.PluralRules(current).select(n);
  return msg[rule] ?? msg.other ?? Object.values(msg)[0];
}

function fill(s: string, vars?: Record<string, any>): string {
  if (!vars) return s;
  return s.replace(/\{(\w+)\}/g, (m, k) => (k in vars && vars[k] !== undefined && vars[k] !== null ? formatValue(vars[k]) : m));
}

function formatValue(v: any): string {
  if (typeof v === "number") return v.toLocaleString(current);
  return String(v);
}

/** Translate English UI text into the current language. Unknown text comes back unchanged (English). */
export function t(text: string, vars?: Record<string, any>): string {
  if (text === undefined || text === null) return text as any;
  const s = String(text);
  if (current === "en" && !appWords.en && !builtIn[s]) return fill(s, vars);
  const msg = appWords[current]?.[s] ?? builtIn[s];
  return fill(pick(msg, vars) ?? s, vars);
}

/** Like t(), but placeholders may be React elements: tn("Signed in as {name}", { name: <b>{user}</b> }). */
export function tn(text: string, vars: Record<string, ReactNode>): ReactNode {
  const plain: Record<string, any> = {};
  for (const [k, v] of Object.entries(vars)) if (!isValidElement(v) && typeof v !== "object") plain[k] = v;
  const msg = appWords[current]?.[text] ?? builtIn[text];
  const s = pick(msg, plain) ?? text;
  const parts = s.split(/(\{\w+\})/g);
  return createElement(Fragment, null, ...parts.map((p, i) => {
    const m = /^\{(\w+)\}$/.exec(p);
    if (!m || !(m[1] in vars)) return p;
    const v = vars[m[1]];
    return isValidElement(v) ? createElement(Fragment, { key: i }, v) : formatValue(v ?? "");
  }));
}

/** Text that may or may not be English UI text (a label from config, a message from the API): translated when known. */
export const tx = (text: any, vars?: Record<string, any>) => (typeof text === "string" && text ? t(text, vars) : text);

async function loadBuiltIn(code: string): Promise<Dict> {
  const loader = loaders[`./locales/${code}.json`];
  if (!loader) return {};
  try { return (await loader()).default || {}; } catch { return {}; }
}

function applyDocument() {
  const el = document.documentElement;
  el.lang = current;
  el.dir = isRtl() ? "rtl" : "ltr";
  // browser page translators (Google, Edge) rewrite text the app manages and break the page: the app translates itself
  el.setAttribute("translate", "no");
  el.classList.add("notranslate");
}

/** Switch language (persisted in this browser). Returns when its translations are loaded. */
export async function setLanguage(code: string, persist = true): Promise<void> {
  const match = matchLanguage(code, availableLanguages().map((l) => l.code)) || appDefault;
  const dict = match === "en" ? await loadBuiltIn("en") : await loadBuiltIn(match);
  current = match;
  builtIn = dict;
  if (persist) try { localStorage.setItem(STORAGE_KEY, match); } catch { /* private mode */ }
  applyDocument();
  notify();
}

/** The language to start with: saved choice, else the browser's languages, else the app default. */
export function initialLanguage(): string {
  const avail = availableLanguages().map((l) => l.code);
  let saved: string | null = null;
  try { saved = localStorage.getItem(STORAGE_KEY); } catch { /* ignore */ }
  const fromSaved = matchLanguage(saved, avail);
  if (fromSaved) return fromSaved;
  for (const l of (navigator.languages || [navigator.language])) {
    const m = matchLanguage(l, avail);
    if (m) return m;
  }
  return appDefault;
}

export function savedLanguage(): string | null {
  try { return localStorage.getItem(STORAGE_KEY); } catch { return null; }
}

/** The app's own words and language list (ui.config "i18n"): { default, languages, translations: { fr: { "Tasks": "Tâches" } } }. */
export function setAppI18n(cfg: { default?: string; languages?: string[]; translations?: Record<string, Dict> } | undefined) {
  appWords = cfg?.translations || {};
  appLanguages = cfg?.languages?.length ? cfg.languages : null;
  appDefault = matchLanguage(cfg?.default, LANGUAGES.map((l) => l.code)) || "en";
  notify();
}

export function subscribe(l: () => void) { listeners.add(l); return () => { listeners.delete(l); }; }

/** Re-renders when the language changes. */
export function useT() {
  const v = useSyncExternalStore(subscribe, () => version);
  return { t, tx, language: current, version: v, rtl: isRtl(), setLanguage, languages: availableLanguages() };
}

// ---------------------------------------------------------------- formatting in the current language

export const formatDate = (d: Date) => d.toLocaleDateString(current);
export const formatDateTime = (d: Date) => d.toLocaleString(current, { dateStyle: "medium", timeStyle: "short" } as any);
export const formatNumber = (n: number, opts?: Intl.NumberFormatOptions) => n.toLocaleString(current, opts);

/** A label inside a sentence ("New {item}"): lower-cased where the language does that (not German nouns, not scripts without case). */
export function lower(label: string | undefined | null): string {
  if (!label) return "";
  if (["de", "lb"].includes(current.split("-")[0])) return label;
  return label.toLocaleLowerCase(current);
}

/** "in_progress" -> "In progress": how a stored option value is shown when the config gives it no label. */
export function optionText(value: string): string {
  if (!/^[a-z0-9]+(?:[_-][a-z0-9]+)*$/.test(value)) return value;
  const s = value.replace(/[_-]+/g, " ");
  return s.charAt(0).toUpperCase() + s.slice(1);
}
