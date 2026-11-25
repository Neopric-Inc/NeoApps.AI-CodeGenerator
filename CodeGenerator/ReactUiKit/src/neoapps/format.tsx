import React from "react";
import { Chip } from "@mui/material";
import CheckCircleIcon from "@mui/icons-material/CheckCircle";
import RadioButtonUncheckedIcon from "@mui/icons-material/RadioButtonUnchecked";
import { ResolvedField } from "./types";

import { language } from "./i18n";
/** The API returns dates as "MM/dd/yyyy HH:mm:ss". Returns a Date or null. */
/**
 * Reads a date from the API. The server stores times in UTC without a zone suffix, so pass utc=true for date-and-time
 * values: they are then shown in the viewer's own time zone. Plain dates (utc=false) stay on their calendar day.
 */
export function parseApiDate(v: any, utc = false): Date | null {
  if (!v) return null;
  if (v instanceof Date) return v;
  const s = String(v);
  const m = /^(\d{1,2})\/(\d{1,2})\/(\d{4})(?:\s+(\d{1,2}):(\d{2})(?::(\d{2}))?)?/.exec(s);
  if (m) {
    const parts = [+m[3], +m[1] - 1, +m[2], +(m[4] || 0), +(m[5] || 0), +(m[6] || 0)] as const;
    return utc ? new Date(Date.UTC(...parts)) : new Date(...parts);
  }
  const naive = /^\d{4}-\d{2}-\d{2}[T ]\d{2}:\d{2}(:\d{2}(\.\d+)?)?$/.test(s);
  const d = new Date(utc && naive ? s.replace(" ", "T") + "Z" : s);
  return isNaN(d.getTime()) ? null : d;
}

const pad = (n: number) => String(n).padStart(2, "0");
export const toDateInput = (v: any) => {
  const d = parseApiDate(v);
  return d ? `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}` : "";
};
export const toDateTimeInput = (v: any) => {
  const d = parseApiDate(v, true);
  return d ? `${toDateInput(d)}T${pad(d.getHours())}:${pad(d.getMinutes())}` : "";
};
/** Value the API accepts for DATE/DATETIME columns. */
export const toApiDate = (v: string) => (v ? (v.length === 10 ? `${v}T00:00:00` : v.length === 16 ? `${v}:00` : v) : null);
/** A date-and-time the user typed (their local time, "YYYY-MM-DDTHH:MM") as the UTC value the API stores. */
export const toApiDateTime = (v: string) => {
  if (!v) return null;
  const d = new Date(v.length === 16 ? `${v}:00` : v);
  return isNaN(d.getTime()) ? toApiDate(v) : d.toISOString().slice(0, 19);
};

const STATUS_COLORS: Record<string, any> = {
  active: "success", enrolling: "success", completed: "success", done: "success", approved: "success", connected: "success", current: "success", open: "info",
  startup: "info", draft: "default", new: "info", queued: "default", running: "warning", pending: "warning", review: "warning", "in progress": "warning",
  failed: "error", closed: "default", cancelled: "error", rejected: "error", expired: "error", disconnected: "error", superseded: "default",
};

export function formatMoney(value: any, code?: string): string {
  try { return Number(value).toLocaleString(language(), { style: "currency", currency: code || "USD", minimumFractionDigits: 2, maximumFractionDigits: 2 }); }
  catch { return `${code || ""} ${Number(value).toFixed(2)}`; }
}
export function currencySymbol(code?: string): string {
  try { return (0).toLocaleString(language(), { style: "currency", currency: code || "USD" }).replace(/[\d.,\s]/g, "") || "$"; } catch { return "$"; }
}

export function renderValue(field: ResolvedField | undefined, value: any, refLabel?: string): React.ReactNode {
  if (value === null || value === undefined || value === "") return <span style={{ color: "#9aa3af" }}>—</span>;
  if (!field) return String(value);
  switch (field.widget) {
    case "checkbox":
      return Number(value) === 1 || value === true
        ? <CheckCircleIcon fontSize="small" color="success" />
        : <RadioButtonUncheckedIcon fontSize="small" sx={{ color: "#c4cbd4" }} />;
    case "reference":
      return refLabel ?? `#${value}`;
    case "date": {
      const d = parseApiDate(value);
      return d ? d.toLocaleDateString(language()) : String(value);
    }
    case "datetime": {
      const d = parseApiDate(value, true);
      return d ? d.toLocaleString(language(), { dateStyle: "medium", timeStyle: "short" } as any) : String(value);
    }
    case "currency":
      return formatMoney(value, field.currencyCode);
    case "percent":
      return `${value}%`;
    case "number":
      return Number(value).toLocaleString(language());
    case "url":
      return <a href={String(value)} target="_blank" rel="noreferrer" onClick={(e) => e.stopPropagation()}>{String(value)}</a>;
    case "color":
      return <span style={{ display: "inline-flex", alignItems: "center", gap: 6 }}><span style={{ width: 12, height: 12, borderRadius: 3, background: String(value), border: "1px solid #ddd" }} />{String(value)}</span>;
    case "select": {
      const s = String(value);
      const color = (field.colors && field.colors[s]) || STATUS_COLORS[s.toLowerCase()] || "default";
      return <Chip size="small" label={field.optionLabel ? field.optionLabel(s) : s} color={color} variant={color === "default" ? "outlined" : "filled"} />;
    }
    default: {
      const s = String(value);
      if (/status|state|stage/i.test(field.name) && s.length < 30) {
        const color = (field.colors && field.colors[s]) || STATUS_COLORS[s.toLowerCase()] || "default";
        return <Chip size="small" label={s} color={color} variant={color === "default" ? "outlined" : "filled"} />;
      }
      return s.length > 80 ? s.slice(0, 80) + "…" : s;
    }
  }
}
