import React from "react";
import { Chip } from "@mui/material";
import CheckCircleIcon from "@mui/icons-material/CheckCircle";
import RadioButtonUncheckedIcon from "@mui/icons-material/RadioButtonUnchecked";
import { ResolvedField } from "./types";

/** The API returns dates as "MM/dd/yyyy HH:mm:ss". Returns a Date or null. */
export function parseApiDate(v: any): Date | null {
  if (!v) return null;
  if (v instanceof Date) return v;
  const s = String(v);
  const m = /^(\d{1,2})\/(\d{1,2})\/(\d{4})(?:\s+(\d{1,2}):(\d{2})(?::(\d{2}))?)?/.exec(s);
  if (m) return new Date(+m[3], +m[1] - 1, +m[2], +(m[4] || 0), +(m[5] || 0), +(m[6] || 0));
  const d = new Date(s);
  return isNaN(d.getTime()) ? null : d;
}

const pad = (n: number) => String(n).padStart(2, "0");
export const toDateInput = (v: any) => {
  const d = parseApiDate(v);
  return d ? `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}` : "";
};
export const toDateTimeInput = (v: any) => {
  const d = parseApiDate(v);
  return d ? `${toDateInput(d)}T${pad(d.getHours())}:${pad(d.getMinutes())}` : "";
};
/** Value the API accepts for DATE/DATETIME columns. */
export const toApiDate = (v: string) => (v ? (v.length === 10 ? `${v}T00:00:00` : v.length === 16 ? `${v}:00` : v) : null);

const STATUS_COLORS: Record<string, any> = {
  active: "success", enrolling: "success", completed: "success", done: "success", approved: "success", connected: "success", current: "success", open: "info",
  startup: "info", draft: "default", new: "info", queued: "default", running: "warning", pending: "warning", review: "warning", "in progress": "warning",
  failed: "error", closed: "default", cancelled: "error", rejected: "error", expired: "error", disconnected: "error", superseded: "default",
};

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
      return d ? d.toLocaleDateString() : String(value);
    }
    case "datetime": {
      const d = parseApiDate(value);
      return d ? d.toLocaleString([], { dateStyle: "medium", timeStyle: "short" } as any) : String(value);
    }
    case "currency":
      return Number(value).toLocaleString(undefined, { style: "currency", currency: "USD", maximumFractionDigits: 0 });
    case "percent":
      return `${value}%`;
    case "number":
      return Number(value).toLocaleString();
    case "url":
      return <a href={String(value)} target="_blank" rel="noreferrer" onClick={(e) => e.stopPropagation()}>{String(value)}</a>;
    case "color":
      return <span style={{ display: "inline-flex", alignItems: "center", gap: 6 }}><span style={{ width: 12, height: 12, borderRadius: 3, background: String(value), border: "1px solid #ddd" }} />{String(value)}</span>;
    case "select": {
      const s = String(value);
      const color = (field.colors && field.colors[s]) || STATUS_COLORS[s.toLowerCase()] || "default";
      return <Chip size="small" label={s} color={color} variant={color === "default" ? "outlined" : "filled"} />;
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
