import {
  AppSchema, ColumnSchema, FieldConfig, MenuGroup, ResolvedField, ResolvedTable, TableConfig, TableSchema, UiConfig, Widget,
} from "./types";

export const AUDIT_FIELDS = ["createdBy", "modifiedBy", "createdAt", "modifiedAt", "isActive", "created_by_user", "modified_by_user"];

/** "trial_opportunities" -> "Trial Opportunities"; "nct_number" -> "NCT Number" */
export function humanize(name: string): string {
  const acronyms: Record<string, string> = { id: "ID", nct: "NCT", url: "URL", api: "API", fte: "FTE", gcp: "GCP", icf: "ICF", sop: "SOP", soa: "SoA" };
  return name
    .replace(/_id$/, "")
    .replace(/([a-z0-9])([A-Z])/g, "$1 $2")
    .split(/[_\s]+/)
    .filter(Boolean)
    .map((w) => acronyms[w.toLowerCase()] || w.charAt(0).toUpperCase() + w.slice(1))
    .join(" ");
}

export function singularize(label: string): string {
  if (/ies$/i.test(label)) return label.replace(/ies$/i, "y");
  if (/(ss|us)$/i.test(label)) return label;
  if (/(ches|shes|xes|ses)$/i.test(label)) return label.replace(/es$/i, "");
  return label.replace(/s$/i, "");
}

const ICON_HINTS: [RegExp, string][] = [
  [/user|member|staff|people|person|patient|contact|customer|client|employee/, "people"],
  [/document|file|form|report|note/, "description"],
  [/visit|appointment|event|schedule|calendar|booking/, "event"],
  [/task|todo|assessment|checklist|assignment/, "assignment"],
  [/opportunit|lead|deal|prospect/, "travel_explore"],
  [/study|studies|trial|lab|experiment/, "science"],
  [/sponsor|company|organi|vendor|account|supplier/, "business"],
  [/chat|message|comment|thread|conversation/, "chat"],
  [/citation|link|reference|connector|integration/, "link"],
  [/run|automation|workflow|job|pipeline/, "bolt"],
  [/search/, "travel_explore"],
  [/order|invoice|payment|budget|expense|transaction/, "receipt"],
  [/product|item|inventory|stock/, "inventory"],
  [/project|board/, "folder"],
];

/** Label for a foreign-key field: the referenced table's singular name ("studies" -> "Study"). */
function referenceLabel(table: string, all: Record<string, TableConfig>): string {
  const cfg = all[table] || {};
  return cfg.singular || singularize(cfg.label || humanize(table));
}

function guessIcon(table: string): string {
  const hit = ICON_HINTS.find(([re]) => re.test(table.toLowerCase()));
  return hit ? hit[1] : "table";
}

function enumOptions(col: ColumnSchema): string[] | undefined {
  const m = /^enum\((.*)\)$/i.exec(col.columnType || "");
  if (!m) return undefined;
  return m[1].split(",").map((s) => s.trim().replace(/^'(.*)'$/, "$1"));
}

function defaultWidget(col: ColumnSchema): Widget {
  const t = (col.dataType || "").toLowerCase();
  const n = col.name.toLowerCase();
  if (col.references) return "reference";
  if (enumOptions(col)) return "select";
  if (t === "tinyint" && /tinyint\(1\)/i.test(col.columnType)) return "checkbox";
  if (t === "bit" || t === "boolean") return "checkbox";
  if (t === "date") return "date";
  if (t === "datetime" || t === "timestamp") return "datetime";
  if (["decimal", "float", "double"].includes(t)) return /amount|price|cost|budget|total|fee|salary/.test(n) ? "currency" : "number";
  if (["int", "bigint", "smallint", "mediumint", "tinyint", "year"].includes(t)) return "number";
  if (["text", "mediumtext", "longtext", "json"].includes(t)) return "textarea";
  if (/email/.test(n)) return "email";
  if (/url|link|website/.test(n)) return "url";
  if (/colou?r/.test(n)) return "color";
  return "text";
}

function defaultDisplayField(t: TableSchema): string {
  const cols = t.columns.filter((c) => !c.isPrimaryKey && !c.references && !AUDIT_FIELDS.includes(c.name));
  const prefer = ["name", "title", "full_name", "display_name", "label", "username", "email", "code"];
  for (const p of prefer) if (cols.find((c) => c.name === p)) return p;
  const named = cols.find((c) => /(_name|_title|name|title)$/.test(c.name));
  if (named) return named.name;
  const str = cols.find((c) => ["varchar", "char"].includes(c.dataType));
  return str ? str.name : t.primaryKey;
}

function defaultListColumns(t: TableSchema, display: string): string[] {
  const cols = t.columns
    .filter((c) => !c.isPrimaryKey && !AUDIT_FIELDS.includes(c.name) && !["text", "mediumtext", "longtext", "json"].includes(c.dataType))
    .map((c) => c.name);
  const ordered = [display, ...cols.filter((c) => c !== display)].filter((c) => c !== t.primaryKey);
  return ordered.slice(0, 6);
}

export function resolveTable(schema: AppSchema, t: TableSchema, cfg: TableConfig = {}, all: Record<string, TableConfig> = {}): ResolvedTable {
  const label = cfg.label || humanize(t.name);
  const displayField = cfg.displayField && t.columns.find((c) => c.name === cfg.displayField) ? cfg.displayField : defaultDisplayField(t);
  const fields: Record<string, ResolvedField> = {};
  for (const col of t.columns) {
    const fc: FieldConfig = (cfg.fields || {})[col.name] || {};
    const isAudit = AUDIT_FIELDS.includes(col.name);
    const widget: Widget = fc.widget || defaultWidget(col);
    fields[col.name] = {
      ...fc,
      name: col.name,
      column: col,
      label: fc.label || (col.references ? referenceLabel(col.references.table, all) : humanize(col.name)),
      widget,
      options: fc.options || enumOptions(col),
      hideInForm: fc.hideInForm ?? (col.isPrimaryKey || col.isAutoIncrement || isAudit),
      required: fc.required ?? (!col.nullable && !col.hasDefault && !col.isAutoIncrement && !isAudit),
    };
  }
  const children = cfg.children || schema.tables
    .filter((o) => o.name !== t.name && o.columns.some((c) => c.references && c.references.table === t.name))
    .map((o) => o.name);
  const formFields = (cfg.formFields || t.columns.map((c) => c.name)).filter((n) => fields[n] && !fields[n].hideInForm && !fields[n].hidden);
  return {
    name: t.name,
    primaryKey: t.primaryKey,
    label,
    singular: cfg.singular || singularize(label),
    icon: cfg.icon || guessIcon(t.name),
    description: cfg.description,
    displayField,
    listColumns: (cfg.listColumns || defaultListColumns(t, displayField)).filter((n) => fields[n] && !fields[n].hidden),
    formFields,
    defaultSort: cfg.defaultSort,
    children,
    hidden: !!cfg.hidden,
    fields,
    schema: t,
  };
}

export function resolveAll(schema: AppSchema, ui: UiConfig): Record<string, ResolvedTable> {
  const out: Record<string, ResolvedTable> = {};
  for (const t of schema.tables) out[t.name] = resolveTable(schema, t, (ui.tables || {})[t.name], ui.tables || {});
  return out;
}

export function resolveMenu(tables: Record<string, ResolvedTable>, ui: UiConfig): MenuGroup[] {
  const visible = Object.values(tables).filter((t) => !t.hidden).map((t) => t.name);
  if (ui.menu && ui.menu.length) {
    const listed = new Set(ui.menu.flatMap((g) => g.items));
    const groups = ui.menu.map((g) => ({ group: g.group, items: g.items.filter((i) => tables[i] && !tables[i].hidden) }));
    const rest = visible.filter((v) => !listed.has(v));
    if (rest.length) groups.push({ group: "More", items: rest.sort() });
    return groups.filter((g) => g.items.length);
  }
  return [{ group: "Data", items: visible.sort((a, b) => tables[a].label.localeCompare(tables[b].label)) }];
}

/** Deep-ish merge: later configs override earlier ones; table/field maps merge per key. */
export function mergeConfig(...configs: (UiConfig | null | undefined)[]): UiConfig {
  const out: UiConfig = { tables: {} };
  for (const c of configs) {
    if (!c) continue;
    const { tables, ...rest } = c;
    Object.assign(out, rest);
    for (const [name, t] of Object.entries(tables || {})) {
      const prev = out.tables![name] || {};
      out.tables![name] = { ...prev, ...t, fields: { ...(prev.fields || {}), ...((t && t.fields) || {}) } };
    }
  }
  return out;
}
