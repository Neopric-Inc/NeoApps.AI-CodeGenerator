import React, { useEffect, useMemo, useState } from "react";
import {
  Alert, Autocomplete, Box, Button, Checkbox, CircularProgress, Dialog, DialogActions, DialogContent, DialogTitle,
  FormControlLabel, InputAdornment, MenuItem, TextField,
} from "@mui/material";
import { createRecord, updateRecord } from "./api";
import { toApiDate, toDateInput, toDateTimeInput } from "./format";
import { ResolvedField, ResolvedTable } from "./types";
import { invalidateReferences, useReferences } from "./useReferences";

interface Props {
  table: ResolvedTable;
  record?: Record<string, any> | null; // null/undefined = create
  defaults?: Record<string, any>; // e.g. parent FK when adding from a record page
  open: boolean;
  onClose: () => void;
  onSaved: () => void;
}

function initialValue(f: ResolvedField, record?: Record<string, any> | null, defaults?: Record<string, any>) {
  const v = record ? record[f.name] : defaults && f.name in defaults ? defaults[f.name] : f.defaultValue;
  switch (f.widget) {
    case "checkbox": return v === undefined || v === null ? 0 : Number(v) === 1 || v === true ? 1 : 0;
    case "date": return toDateInput(v);
    case "datetime": return toDateTimeInput(v);
    default: return v === undefined || v === null ? "" : v;
  }
}

export const RecordForm: React.FC<Props> = ({ table, record, defaults, open, onClose, onSaved }) => {
  const { refs } = useReferences(table);
  const fields = useMemo(() => table.formFields.map((n) => table.fields[n]), [table]);
  const [values, setValues] = useState<Record<string, any>>({});
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
      if (f.required && f.widget !== "checkbox" && (v === "" || v === null || v === undefined)) e[f.name] = `${f.label} is required`;
      else if (f.widget === "email" && v && !/^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(v)) e[f.name] = "Enter a valid email";
      else if (["number", "currency", "percent"].includes(f.widget) && v !== "" && isNaN(Number(v))) e[f.name] = "Enter a number";
      else if (f.column.maxLength && typeof v === "string" && v.length > f.column.maxLength) e[f.name] = `Max ${f.column.maxLength} characters`;
    }
    setErrors(e);
    return Object.keys(e).length === 0;
  };

  const toPayload = () => {
    const out: Record<string, any> = editing ? { ...record } : { isActive: 1 };
    for (const f of fields) {
      const v = values[f.name];
      const numeric = ["int", "bigint", "smallint", "mediumint", "tinyint", "decimal", "float", "double", "year"].includes(f.column.dataType);
      if (f.widget === "date" || f.widget === "datetime") out[f.name] = toApiDate(v);
      else if (f.widget === "checkbox") out[f.name] = v ? 1 : 0;
      else if (v === "" || v === undefined) out[f.name] = f.column.nullable ? null : numeric ? 0 : "";
      else out[f.name] = numeric ? Number(v) : v;
    }
    // Dates the user didn't touch are sent back in API format, not the display format.
    for (const [k, f] of Object.entries(table.fields)) {
      if ((f.widget === "date" || f.widget === "datetime") && editing && !table.formFields.includes(k)) {
        out[k] = toApiDate(f.widget === "date" ? toDateInput(out[k]) : toDateTimeInput(out[k]));
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
      const res = editing ? await updateRecord(table.name, record![table.primaryKey], payload) : await createRecord(table.name, payload);
      if (res && res.code === 0) throw new Error(res.message || "Save failed");
      invalidateReferences(table.name);
      onSaved();
    } catch (e: any) {
      setError(/foreign key/i.test(e.message) ? "One of the selected related records no longer exists." : e.message);
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
        return <TextField {...common} type="date" InputLabelProps={{ shrink: true }} />;
      case "datetime":
        return <TextField {...common} type="datetime-local" InputLabelProps={{ shrink: true }} />;
      case "number":
      case "percent":
        return <TextField {...common} type="number" InputProps={f.widget === "percent" ? { endAdornment: <InputAdornment position="end">%</InputAdornment> } : undefined} />;
      case "currency":
        return <TextField {...common} type="number" InputProps={{ startAdornment: <InputAdornment position="start">$</InputAdornment> }} />;
      case "email":
        return <TextField {...common} type="email" />;
      case "color":
        return <TextField {...common} type="color" InputLabelProps={{ shrink: true }} />;
      case "select":
        return (
          <TextField {...common} select>
            {!f.required && <MenuItem value=""><em>None</em></MenuItem>}
            {(f.options || []).map((o) => <MenuItem key={o} value={o}>{o}</MenuItem>)}
          </TextField>
        );
      case "reference": {
        const opts = refs[f.column.references!.table] || [];
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

  return (
    <Dialog open={open} onClose={busy ? undefined : onClose} fullWidth maxWidth="sm">
      <DialogTitle>{editing ? `Edit ${table.singular}` : `New ${table.singular}`}</DialogTitle>
      <DialogContent dividers>
        <Box sx={{ display: "grid", gridTemplateColumns: { xs: "1fr", sm: "1fr 1fr" }, gap: 2, pt: 0.5 }}>
          {fields.map((f) => (
            <Box key={f.name} sx={{ gridColumn: f.widget === "textarea" || f.widget === "reference" ? "1 / -1" : undefined }}>
              {fieldInput(f)}
            </Box>
          ))}
        </Box>
        {error && <Alert severity="error" sx={{ mt: 2 }}>{error}</Alert>}
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} disabled={busy}>Cancel</Button>
        <Button variant="contained" onClick={save} disabled={busy} data-testid="save-record">
          {busy ? <CircularProgress size={20} color="inherit" /> : editing ? "Save changes" : `Create ${table.singular}`}
        </Button>
      </DialogActions>
    </Dialog>
  );
};
