import { listRecords } from "./api";
import { parseApiDate } from "./format";
import { ResolvedTable } from "./types";

const PAGE = 1000; // the API's default Api:MaxPageSize

/** Cell text for spreadsheets: reference labels, ISO dates, and a leading ' on anything a spreadsheet would run as a formula. */
function cell(v: any): string {
  if (v === null || v === undefined) return "";
  let s = typeof v === "object" ? JSON.stringify(v) : String(v);
  if (/^[=+\-@\t\r]/.test(s)) s = "'" + s;
  return /[",\r\n]/.test(s) ? `"${s.replace(/"/g, '""')}"` : s;
}

/**
 * Downloads every row of the current list (same search and sort) as CSV. Rows come through the normal list API, so
 * row scopes, tenancy and hidden fields apply exactly as on screen.
 */
export async function exportCsv(table: ResolvedTable, orderBy: string | undefined, search: string | undefined,
  labelFor: (field: string, id: any) => string | undefined): Promise<number> {
  const rows: Record<string, any>[] = [];
  for (let page = 1; ; page++) {
    const res = await listRecords(table.name, page, PAGE, orderBy, search);
    rows.push(...res.records);
    if (res.records.length < PAGE || rows.length >= res.total) break;
  }
  const present = new Set(rows.flatMap((r) => Object.keys(r)));
  const columns = Array.from(new Set([table.primaryKey, table.displayField, ...table.formFields, ...table.listColumns, "createdAt", "modifiedAt"]))
    .filter((c) => present.has(c) && !table.fields[c]?.hidden);
  const header = columns.map((c) => cell(c === table.primaryKey ? "ID" : table.fields[c]?.label || c));
  const lines = rows.map((r) => columns.map((c) => {
    const f = table.fields[c];
    const v = r[c];
    if (f?.column.references && v != null) return cell(labelFor(c, v) ?? v);
    if (f && (f.widget === "date" || f.widget === "datetime")) {
      const d = parseApiDate(v);
      return cell(d ? (f.widget === "date" ? d.toISOString().slice(0, 10) : d.toISOString()) : v);
    }
    return cell(v);
  }).join(","));
  const csv = "﻿" + [header.join(","), ...lines].join("\r\n"); // BOM so Excel reads UTF-8
  const url = URL.createObjectURL(new Blob([csv], { type: "text/csv;charset=utf-8" }));
  const a = document.createElement("a");
  a.href = url;
  a.download = `${table.name}-${new Date().toISOString().slice(0, 10)}.csv`;
  document.body.appendChild(a);
  a.click();
  a.remove();
  setTimeout(() => URL.revokeObjectURL(url), 1000);
  return rows.length;
}
