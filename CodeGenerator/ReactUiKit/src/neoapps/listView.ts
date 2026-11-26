// List state kept in the URL, so a filtered list can be bookmarked, shared and opened from a dashboard card:
//   ?q=text            search (any text column)
//   ?f.status=open     equals          ?f.status.ne=done   not equal
//   ?f.due.from=2026-01-01&f.due.to=2026-01-31            date range (inclusive)
//   ?sort=createdAt|DESC   ?page=3 (1-based)   ?size=50
import type { ResolvedField, ResolvedTable } from "./types";
import { api } from "./api";

export interface FilterValue { eq?: string; ne?: string; from?: string; to?: string }
export type Filters = Record<string, FilterValue>;
export interface FilterModel { columnName: string; columnCondition: number; columnValue: string }

/** Conditions of the generated /filter endpoint. */
export const Cond = { Contains: 1, GreaterThan: 3, LessThan: 4, Equals: 5, GreaterThanEqual: 6, LessThanEqual: 7, NotEqual: 8 } as const;

const AUDIT = ["createdBy", "modifiedBy", "createdAt", "modifiedAt", "isActive", "created_by_user", "modified_by_user"];

/** Fields offered as filters: ui.config tables.X.filters, else relations, options, yes/no and dates (up to 6). */
export function filterFields(table: ResolvedTable): ResolvedField[] {
  const configured = table.filters;
  if (configured?.length) return configured.map((n) => table.fields[n]).filter((f) => f && !f.hidden);
  return Object.values(table.fields)
    .filter((f) => !f.hidden && !f.column.isPrimaryKey && !AUDIT.includes(f.name))
    .filter((f) => ["reference", "select", "checkbox", "date", "datetime"].includes(f.widget) || (f.options && f.options.length > 0))
    .sort((a, b) => rank(a) - rank(b))
    .slice(0, 6);
}
const rank = (f: ResolvedField) => (f.widget === "reference" ? 0 : f.options?.length || f.widget === "select" ? 1 : f.widget === "checkbox" ? 2 : 3);

export const isRange = (f?: ResolvedField) => !!f && (f.widget === "date" || f.widget === "datetime");

export function readFilters(params: URLSearchParams): Filters {
  const out: Filters = {};
  params.forEach((value, key) => {
    if (!key.startsWith("f.") || value === "") return;
    const rest = key.slice(2);
    const m = /^(.*)\.(ne|from|to)$/.exec(rest);
    const [col, op] = m ? [m[1], m[2] as keyof FilterValue] : [rest, "eq" as const];
    (out[col] ||= {})[op] = value;
  });
  return out;
}

export function writeFilters(params: URLSearchParams, filters: Filters): URLSearchParams {
  const next = new URLSearchParams();
  params.forEach((v, k) => { if (!k.startsWith("f.")) next.append(k, v); });
  for (const [col, f] of Object.entries(filters)) {
    if (f.eq) next.set(`f.${col}`, f.eq);
    if (f.ne) next.set(`f.${col}.ne`, f.ne);
    if (f.from) next.set(`f.${col}.from`, f.from);
    if (f.to) next.set(`f.${col}.to`, f.to);
  }
  return next;
}

export const activeCount = (filters: Filters) => Object.values(filters).filter((f) => f.eq || f.ne || f.from || f.to).length;

/** URL filters -> conditions for POST {table}/filter (only known columns). */
export function toWhere(table: ResolvedTable, filters: Filters): FilterModel[] {
  const where: FilterModel[] = [];
  for (const [col, f] of Object.entries(filters)) {
    const field = table.fields[col];
    if (!field) continue;
    const add = (c: number, v: string) => where.push({ columnName: col, columnCondition: c, columnValue: v });
    if (f.eq !== undefined && f.eq !== "") add(Cond.Equals, f.eq);
    if (f.ne !== undefined && f.ne !== "") add(Cond.NotEqual, f.ne);
    if (f.from) add(Cond.GreaterThanEqual, f.from);
    // a "to" date includes that whole day
    if (f.to) field.widget === "datetime" && /^\d{4}-\d{2}-\d{2}$/.test(f.to) ? add(Cond.LessThan, nextDay(f.to)) : add(Cond.LessThanEqual, f.to);
  }
  return where;
}

const nextDay = (d: string) => {
  const t = new Date(d + "T00:00:00Z");
  t.setUTCDate(t.getUTCDate() + 1);
  return t.toISOString().slice(0, 10);
};

/** Newest first unless ui.config sets defaultSort. */
export function defaultSortOf(table: ResolvedTable): string {
  if (table.defaultSort) return table.defaultSort;
  const has = (c: string) => table.schema.columns.some((x) => x.name === c);
  return has("createdAt") ? "createdAt|DESC" : `${table.primaryKey}|DESC`;
}

export function parseSort(s?: string | null): { column: string; dir: "ASC" | "DESC" } | undefined {
  if (!s) return undefined;
  const [column, d] = s.split("|");
  return column ? { column, dir: (d || "ASC").toUpperCase() === "DESC" ? "DESC" : "ASC" } : undefined;
}

// ------------------------------------------------------------------ saved views (per user, kept by the API)

export interface SavedView { name: string; q?: string; filters?: Filters; sort?: string }
type AllViews = Record<string, SavedView[]>;
let viewsCache: Promise<AllViews> | null = null;

export function loadViews(): Promise<AllViews> {
  viewsCache ||= api<any>("GET", "Access/me/views")
    .then((r) => { try { return (typeof r?.document === "string" ? JSON.parse(r.document) : r?.document) || {}; } catch { return {}; } })
    .catch(() => ({}));
  return viewsCache;
}

export async function saveViews(table: string, views: SavedView[]): Promise<void> {
  const all = { ...(await loadViews()), [table]: views };
  if (!views.length) delete all[table];
  await api("PUT", "Access/me/views", { views: all });
  viewsCache = Promise.resolve(all);
}

export const resetViewsCache = () => { viewsCache = null; };
