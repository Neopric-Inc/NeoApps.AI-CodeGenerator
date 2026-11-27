import React, { useEffect, useMemo, useState } from "react";
import {
  Alert, Autocomplete, Box, Button, Checkbox, CircularProgress, Dialog, DialogActions, DialogContent, DialogTitle,
  FormControlLabel, InputAdornment, MenuItem, TextField,
} from "@mui/material";
import { api, createRecord, putRecord } from "./api";
import { currencySymbol, renderValue, toApiDate, toApiDateTime, toDateInput, toDateTimeInput } from "./format";
import { FieldRule, ResolvedField, ResolvedTable } from "./types";
import { useApp } from "./AppContext";
import { invalidateReferences, useReferences } from "./useReferences";

import { t, lower, tx } from "./i18n";
interface Props {
  table: ResolvedTable;
  record?: Record<string, any> | null; // null/undefined = create
  defaults?: Record<string, any>; // e.g. parent FK when adding from a record page
  open: boolean;
  onClose: () => void;
  onSaved: () => void;
  /** Share links: only these fields, sent with onSubmit instead of the table API, shown on the page instead of a dialog. */
  fieldNames?: string[];
  onSubmit?: (values: Record<string, any>) => Promise<void>;
  inline?: boolean;
  title?: string;
  submitLabel?: string;
  /** Extra content under the fields (e.g. file pickers on a shared form). */
  extra?: React.ReactNode;
}

/** MySQL column default as a form value ('pharma' -> pharma); expressions like CURRENT_TIMESTAMP are left to the server. */
export function columnDefault(f: ResolvedField): any {
  const raw = f.column.defaultValue;
  if (raw === undefined || raw === null || /^(NULL|CURRENT_TIMESTAMP.*|now\(\).*)$/i.test(raw)) return undefined;
  return raw.replace(/^'(.*)'$/, "$1");
}

function initialValue(f: ResolvedField, record?: Record<string, any> | null, defaults?: Record<string, any>) {
  const v = record ? record[f.name] : defaults && f.name in defaults ? defaults[f.name] : f.defaultValue ?? columnDefault(f);
  switch (f.widget) {
    case "checkbox": return v === undefined || v === null ? 0 : Number(v) === 1 || v === true ? 1 : 0;
    case "date": return toDateInput(v);
    case "datetime": return toDateTimeInput(v);
    default: return v === undefined || v === null ? "" : v;
  }
}

/** Client-side copy of the API's "validations" (the API stays the authority; "equals" is only checked there). */
function ruleError(rule: FieldRule, name: string, row: Record<string, any>): string | undefined {
  const v = row[name];
  const empty = v === "" || v === null || v === undefined;
  const label = name.replace(/_/g, " ");
  const msg = (m: string) => tx(rule.message) || m;
  if (empty) {
    if (rule.required) return msg(t("{field} is required", { field: label }));
    const when = rule.requiredWhen;
    if (when && Object.entries(when).every(([k, want]) => ([] as string[]).concat(want).map(String).includes(String(row[k] ?? ""))))
      return msg(t("{field} is required here", { field: label }));
    return undefined;
  }
  const s = String(v);
  if (rule.format === "email" && !/^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(s)) return msg(t("Enter a valid email address"));
  if (rule.format === "url" && !/^https?:\/\/\S+$/i.test(s)) return msg(t("Enter a valid web address (https://...)"));
  if (rule.format === "phone" && !/^\+?[0-9 ()./-]{6,20}$/.test(s)) return msg(t("Enter a valid phone number"));
  if (rule.pattern) { try { if (!new RegExp(rule.pattern).test(s)) return msg(t("{field} has the wrong format", { field: label })); } catch { /* checked by the API */ } }
  if (rule.minLength !== undefined && s.length < rule.minLength) return msg(t("At least {n} characters", { n: rule.minLength }));
  if (rule.maxLength !== undefined && s.length > rule.maxLength) return msg(t("At most {n} characters", { n: rule.maxLength }));
  if (rule.min !== undefined && !(Number(s) >= rule.min)) return msg(t("Must be at least {n}", { n: rule.min }));
  if (rule.max !== undefined && !(Number(s) <= rule.max)) return msg(t("Must be at most {n}", { n: rule.max }));
  const cmp = (a: any, b: any) => {
    const na = Number(a), nb = Number(b);
    if (a !== "" && b !== "" && !isNaN(na) && !isNaN(nb)) return na - nb;
    const da = Date.parse(a), db = Date.parse(b);
    if (!isNaN(da) && !isNaN(db)) return da - db;
    return String(a).localeCompare(String(b));
  };
  const ops: [keyof FieldRule, (c: number) => boolean, string][] = [
    ["gt", (c) => c > 0, "Must be after / greater than {field}"], ["gte", (c) => c >= 0, "Must be on or after / at least {field}"],
    ["lt", (c) => c < 0, "Must be before / less than {field}"], ["lte", (c) => c <= 0, "Must be on or before / at most {field}"], ["ne", (c) => c !== 0, "Must be different from {field}"],
  ];
  for (const [op, ok, word] of ops) {
    const other = rule[op] as string | undefined;
    if (other && row[other] !== "" && row[other] !== null && row[other] !== undefined && !ok(cmp(v, row[other])))
      return msg(t(word, { field: other.replace(/_/g, " ") }));
  }
  return undefined;
}

export const RecordForm: React.FC<Props> = ({ table, record, defaults, open, onClose, onSaved, fieldNames, onSubmit, inline, title, submitLabel, extra }) => {
  const { config } = useApp();
  const rules = config?.validations?.[table.name] || {};
  const { refs } = useReferences(table);
  const { config: appConfig, me: appMe } = useApp();
  // "editableFields": when this role edits an existing record it may only change the listed columns (the API keeps the rest)
  const editableOnly: string[] | undefined = record && appMe?.role_name && !appMe.is_admin ? (appConfig as any)?.editableFields?.[appMe.role_name]?.[table.name] : undefined;
  const fields = useMemo(() => (fieldNames || table.formFields).map((n) => table.fields[n]).filter(Boolean)
    .map((f) => (editableOnly && !editableOnly.includes(f.name) ? { ...f, readOnly: true } : f)), [table, fieldNames, editableOnly]);
  const [values, setValues] = useState<Record<string, any>>({});
  // "allowedReferences": pickers only offer the records this person may choose (the API refuses others)
  const [allowed, setAllowed] = useState<Record<string, Set<string>>>({});
  useEffect(() => {
    const rules: any[] = ((appConfig as any)?.allowedReferences || []).filter((r: any) => r.table === table.name);
    if (!open || !rules.length) return;
    let alive = true;
    Promise.all(Array.from(new Set(rules.map((r) => r.column as string))).map((col) =>
      api<any>("GET", `Access/choices/${table.name}/${col}`).then((r) => [col, r?.document] as const).catch(() => [col, null] as const)))
      .then((pairs) => {
        if (!alive) return;
        const next: Record<string, Set<string>> = {};
        for (const [col, d] of pairs) if (d?.restricted) next[col] = new Set((d.ids || []).map(String));
        setAllowed(next);
      });
    return () => { alive = false; };
  }, [open, table.name, appConfig]);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string>();
  const editing = !!record;

  useEffect(() => {
    if (!open) return;
    const v: Record<string, any> = {};
    fields.forEach((f) => (v[f.name] = initialValue(f, record, defaults)));
    setValues(v);
    setErrors({});
    setError(undefined);
  }, [open, record, defaults, fields]);

  const set = (name: string, value: any) => {
    setValues((s) => ({ ...s, [name]: value }));
    setErrors((e) => ({ ...e, [name]: "" }));
  };

  const validate = () => {
    const e: Record<string, string> = {};
    for (const f of fields) {
      const v = values[f.name];
      if (f.required && f.widget !== "checkbox" && (v === "" || v === null || v === undefined)) e[f.name] = t("{field} is required", { field: f.label });
      else if (f.widget === "email" && v && !/^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(v)) e[f.name] = t("Enter a valid email");
      else if (["number", "currency", "percent"].includes(f.widget) && v !== "" && isNaN(Number(v))) e[f.name] = t("Enter a number");
      else if (f.column.maxLength && typeof v === "string" && v.length > f.column.maxLength) e[f.name] = t("At most {n} characters", { n: f.column.maxLength });
    }
    const row = { ...(record || {}), ...values };
    for (const [name, rule] of Object.entries(rules)) {
      if (e[name] || !(name in values)) continue;
      const problem = ruleError(rule, name, row);
      if (problem) e[name] = problem;
    }
    setErrors(e);
    return Object.keys(e).length === 0;
  };

  const toPayload = () => {
    const out: Record<string, any> = onSubmit ? {} : editing ? { ...record } : { isActive: 1 };
    for (const f of fields) {
      const v = values[f.name];
      const numeric = ["int", "bigint", "smallint", "mediumint", "tinyint", "decimal", "float", "double", "year"].includes(f.column.dataType);
      if (f.widget === "date") out[f.name] = toApiDate(v);
      else if (f.widget === "datetime") out[f.name] = toApiDateTime(v);
      else if (f.widget === "checkbox") out[f.name] = v ? 1 : 0;
      else if (v === "" || v === undefined) {
        const d = columnDefault(f);
        out[f.name] = d !== undefined ? (numeric ? Number(d) : d) : f.column.nullable ? null : numeric ? 0 : "";
      }
      else out[f.name] = numeric ? Number(v) : v;
    }
    // Dates the user didn't touch are sent back in API format, not the display format.
    for (const [k, f] of Object.entries(table.fields)) {
      if ((f.widget === "date" || f.widget === "datetime") && editing && !onSubmit && !table.formFields.includes(k)) {
        out[k] = f.widget === "date" ? toApiDate(toDateInput(out[k])) : toApiDateTime(toDateTimeInput(out[k]));
      }
    }
    return out;
  };

  const save = async () => {
    if (!validate()) return;
    setBusy(true);
    setError(undefined);
    try {
      const payload = toPayload();
      if (onSubmit) { await onSubmit(payload); onSaved(); return; }
      const res = editing ? await putRecord(table.name, record![table.primaryKey], payload) : await createRecord(table.name, payload);
      if (res && res.code === 0) throw new Error(res.message || t("Save failed"));
      invalidateReferences(table.name);
      onSaved();
    } catch (e: any) {
      if (e.fieldErrors) {
        const fe: Record<string, string> = {};
        for (const [k, msgs] of Object.entries<any>(e.fieldErrors)) {
          const f = table.fields[k];
          if (f) fe[k] = `${f.label}: ${tx(String(msgs[0] || t("invalid"))).replace(/^The \S+ field /, "")}`;
        }
        setErrors(fe);
      }
      setError(/foreign key/i.test(e.message) ? t("One of the selected related records no longer exists.") : e.message);
    } finally {
      setBusy(false);
    }
  };

  const fieldInput = (f: ResolvedField) => {
    const common = {
      fullWidth: true, size: "small" as const, label: f.label + (f.required ? " *" : ""), value: values[f.name] ?? "",
      error: !!errors[f.name], helperText: errors[f.name] || f.help, disabled: f.readOnly, placeholder: f.placeholder,
      onChange: (e: any) => set(f.name, e.target.value), name: f.name,
    };
    switch (f.widget) {
      case "checkbox":
        return <FormControlLabel control={<Checkbox name={f.name} checked={!!values[f.name]} onChange={(e) => set(f.name, e.target.checked ? 1 : 0)} disabled={f.readOnly} />} label={f.label} />;
      case "textarea":
        return <TextField {...common} multiline minRows={3} />;
      case "date":
        return (
          <TextField {...common} type="date" slotProps={{
            inputLabel: { shrink: true }
          }} />
        );
      case "datetime":
        return (
          <TextField {...common} type="datetime-local" slotProps={{
            inputLabel: { shrink: true }
          }} />
        );
      case "number":
      case "percent":
        return (
          <TextField {...common} type="number" slotProps={{
            input: f.widget === "percent" ? { endAdornment: <InputAdornment position="end">%</InputAdornment> } : undefined
          }} />
        );
      case "currency":
        return (
          <TextField {...common} type="number" slotProps={{
            input: { startAdornment: <InputAdornment position="start">{currencySymbol(f.currencyCode)}</InputAdornment> }
          }} />
        );
      case "email":
        return <TextField {...common} type="email" />;
      case "color":
        return (
          <TextField {...common} type="color" slotProps={{
            inputLabel: { shrink: true }
          }} />
        );
      case "select":
        return (
          <TextField {...common} select slotProps={{
            select: { renderValue: (v: any) => (v === "" || v == null ? "" : renderValue(f, v)) as any }
          }}>
            {!f.required && <MenuItem value=""><em>{t("None")}</em></MenuItem>}
            {(f.options || []).map((o) => <MenuItem key={o} value={o}>{renderValue(f, o)}</MenuItem>)}
          </TextField>
        );
      case "reference": {
        const all = refs[f.column.references!.table] || [];
        const only = allowed[f.name];
        const opts = only ? all.filter((o) => only.has(String(o.id)) || String(o.id) === String(values[f.name])) : all;
        const selected = opts.find((o) => String(o.id) === String(values[f.name])) || null;
        return (
          <Autocomplete
            options={opts}
            value={selected}
            disabled={f.readOnly}
            getOptionLabel={(o) => o.label}
            isOptionEqualToValue={(a, b) => String(a.id) === String(b.id)}
            onChange={(_, o) => set(f.name, o ? o.id : "")}
            renderInput={(p) => <TextField {...p} name={f.name} size="small" label={common.label} error={common.error} helperText={common.helperText} />}
          />
        );
      }
      default:
        return <TextField {...common} />;
    }
  };

  const grid = (
    <>
      <Box sx={{ display: "grid", gridTemplateColumns: { xs: "1fr", sm: "1fr 1fr" }, gap: 2, pt: 0.5 }}>
        {fields.map((f) => (
          <Box key={f.name} sx={{ gridColumn: f.widget === "textarea" || f.widget === "reference" ? "1 / -1" : undefined }}>
            {fieldInput(f)}
          </Box>
        ))}
      </Box>
      {error && <Alert severity="error" sx={{ mt: 2 }}>{error}</Alert>}
    </>
  );
  const saveButton = (
    <Button variant="contained" onClick={save} disabled={busy} data-testid="save-record">
      {busy ? <CircularProgress size={20} color="inherit" /> : submitLabel || (editing ? t("Save changes") : t("Create {item}", { item: lower(table.singular) }))}
    </Button>
  );
  if (inline) return <Box>{grid}{extra}<Box sx={{ mt: 3, display: "flex", justifyContent: "flex-end" }}>{saveButton}</Box></Box>;

  return (
    <Dialog open={open} onClose={busy ? undefined : onClose} fullWidth maxWidth="sm">
      <DialogTitle>{title || (editing ? t("Edit {item}", { item: lower(table.singular) }) : t("New {item}", { item: lower(table.singular) }))}</DialogTitle>
      <DialogContent dividers>{grid}</DialogContent>
      <DialogActions>
        <Button onClick={onClose} disabled={busy}>{t("Cancel")}</Button>
        {saveButton}
      </DialogActions>
    </Dialog>
  );
};
