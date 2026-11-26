import React, { useEffect, useState } from "react";
import {
  Autocomplete, Badge, Box, Button, Chip, Collapse, Dialog, DialogActions, DialogContent, DialogTitle, Divider, IconButton, ListItemText,
  Menu, MenuItem, TextField, Tooltip,
} from "@mui/material";
import FilterListIcon from "@mui/icons-material/FilterList";
import BookmarkBorderIcon from "@mui/icons-material/BookmarkBorder";
import DeleteOutline from "@mui/icons-material/DeleteOutlineOutlined";
import type { ResolvedField, ResolvedTable } from "./types";
import type { RefOption } from "./useReferences";
import { activeCount, filterFields, type Filters, type FilterValue, isRange, loadViews, type SavedView, saveViews } from "./listView";
import { formatDate, t } from "./i18n";

interface Props {
  table?: ResolvedTable;
  filters: Filters;
  onChange: (f: Filters) => void;
  refs: Record<string, RefOption[]>;
  /** current search and sort, saved with a view */
  q: string;
  sort?: string;
  onApplyView: (v: SavedView) => void;
}

const yesNo = (v?: string) => (v === "1" || v === "true" ? t("Yes") : v === "0" || v === "false" ? t("No") : v || "");

/** Readable value of one filter, for chips. */
export function filterText(field: ResolvedField, refs: Record<string, RefOption[]>, v: string): string {
  if (field.widget === "reference") {
    const hit = (refs[field.column.references!.table] || []).find((o) => String(o.id) === String(v));
    return hit ? hit.label : `#${v}`;
  }
  if (field.widget === "checkbox") return yesNo(v);
  if (field.options?.length || field.widget === "select") return field.optionLabel(v);
  if (isRange(field)) return /^\d{4}-\d{2}-\d{2}$/.test(v) ? formatDate(new Date(v + "T00:00:00")) : v;
  return v;
}

/** Filters and saved views for a list: `buttons` go in the page toolbar, `panel` (filter controls and active chips) under it. */
export function useFilterBar({ table, filters, onChange, refs, q, sort, onApplyView }: Props): { buttons: React.ReactNode; panel: React.ReactNode } {
  const [open, setOpen] = useState(false);
  const [views, setViews] = useState<SavedView[]>([]);
  const [viewsAnchor, setViewsAnchor] = useState<HTMLElement | null>(null);
  const [naming, setNaming] = useState(false);
  const [viewName, setViewName] = useState("");
  const [viewError, setViewError] = useState<string>();
  const tableName = table?.name;
  useEffect(() => { if (tableName) loadViews().then((all) => setViews(all[tableName] || [])); }, [tableName]);
  if (!table) return { buttons: null, panel: null };
  const fields = filterFields(table);
  const count = activeCount(filters);

  const set = (col: string, patch: Partial<FilterValue>) => {
    const next: Filters = { ...filters, [col]: { ...(filters[col] || {}), ...patch } };
    const f = next[col];
    (Object.keys(f) as (keyof FilterValue)[]).forEach((k) => { if (!f[k]) delete f[k]; });
    if (!Object.keys(f).length) delete next[col];
    onChange(next);
  };

  const chips = Object.entries(filters).flatMap(([col, f]) => {
    const field = table.fields[col];
    if (!field) return [];
    const out: { key: string; label: string; clear: Partial<FilterValue> }[] = [];
    if (f.eq) out.push({ key: col + ".eq", label: `${field.label}: ${filterText(field, refs, f.eq)}`, clear: { eq: undefined } });
    if (f.ne) out.push({ key: col + ".ne", label: t("{field}: not {value}", { field: field.label, value: filterText(field, refs, f.ne) }), clear: { ne: undefined } });
    if (f.from || f.to) {
      const label = f.from && f.to ? `${filterText(field, refs, f.from)} – ${filterText(field, refs, f.to)}`
        : f.from ? t("from {date}", { date: filterText(field, refs, f.from) }) : t("until {date}", { date: filterText(field, refs, f.to!) });
      out.push({ key: col + ".range", label: `${field.label}: ${label}`, clear: { from: undefined, to: undefined } });
    }
    return out;
  });

  const persist = async (next: SavedView[]) => {
    setViewError(undefined);
    try { await saveViews(table.name, next); setViews(next); return true; } catch (e: any) { setViewError(e.message); return false; }
  };
  const saveView = async () => {
    const name = viewName.trim().slice(0, 80);
    if (name && await persist([...views.filter((v) => v.name !== name), { name, q: q || undefined, filters, sort }])) setNaming(false);
  };

  const control = (field: ResolvedField) => {
    const f = filters[field.name] || {};
    const common = { size: "small" as const, sx: { width: { xs: "100%", sm: 220 } } };
    if (field.widget === "reference") {
      const options = refs[field.column.references!.table] || [];
      return (
        <Autocomplete key={field.name} {...common} options={options}
          value={options.find((o) => String(o.id) === String(f.eq)) || null}
          onChange={(_, v) => set(field.name, { eq: v ? String(v.id) : undefined })}
          getOptionLabel={(o) => o.label} isOptionEqualToValue={(a, b) => String(a.id) === String(b.id)}
          renderInput={(p) => <TextField {...p} label={field.label} placeholder={t("Any")} />} />
      );
    }
    if (isRange(field)) {
      return (
        <Box key={field.name} sx={{ display: "flex", gap: 1, width: { xs: "100%", sm: "auto" } }}>
          <TextField size="small" type="date" label={t("{field} from", { field: field.label })} value={f.from || ""} sx={{ flex: 1, minWidth: 150 }}
            onChange={(e) => set(field.name, { from: e.target.value })} slotProps={{ inputLabel: { shrink: true } }} />
          <TextField size="small" type="date" label={t("to")} value={f.to || ""} sx={{ flex: 1, minWidth: 150 }}
            onChange={(e) => set(field.name, { to: e.target.value })} slotProps={{ inputLabel: { shrink: true } }} />
        </Box>
      );
    }
    const choices: { value: string; label: string }[] = field.widget === "checkbox"
      ? [{ value: "1", label: t("Yes") }, { value: "0", label: t("No") }]
      : (field.options || []).map((o) => ({ value: o, label: field.optionLabel(o) }));
    return (
      <TextField key={field.name} {...common} select label={field.label} value={f.eq || ""} onChange={(e) => set(field.name, { eq: e.target.value })}>
        <MenuItem value=""><em>{t("Any")}</em></MenuItem>
        {choices.map((c) => <MenuItem key={c.value} value={c.value}>{c.label}</MenuItem>)}
      </TextField>
    );
  };

  const buttons = (
    <>
      <Box sx={{ display: "flex", gap: 1 }}>
        {fields.length > 0 && (
          <Button variant={open || count ? "contained" : "outlined"} color={count ? "primary" : "inherit"} onClick={() => setOpen((o) => !o)}
            startIcon={<Badge badgeContent={count} color="secondary"><FilterListIcon /></Badge>} aria-expanded={open}>
            {t("Filters")}
          </Button>
        )}
        <Tooltip title={t("Saved views")}>
          <IconButton onClick={(e) => setViewsAnchor(e.currentTarget)} aria-label={t("Saved views")}><BookmarkBorderIcon /></IconButton>
        </Tooltip>
      </Box>
      <Menu anchorEl={viewsAnchor} open={!!viewsAnchor} onClose={() => setViewsAnchor(null)}>
        {views.map((v) => (
          <MenuItem key={v.name} onClick={() => { setViewsAnchor(null); onApplyView(v); }} sx={{ minWidth: 240 }}>
            <ListItemText primary={v.name} />
            <IconButton size="small" edge="end" aria-label={t("Delete")}
              onClick={(e) => { e.stopPropagation(); persist(views.filter((x) => x.name !== v.name)); }}><DeleteOutline fontSize="small" /></IconButton>
          </MenuItem>
        ))}
        {views.length > 0 && <Divider />}
        <MenuItem onClick={() => { setViewsAnchor(null); setViewName(""); setNaming(true); }}>{t("Save current view…")}</MenuItem>
      </Menu>
      <Dialog open={naming} onClose={() => setNaming(false)} maxWidth="xs" fullWidth>
        <DialogTitle>{t("Save current view")}</DialogTitle>
        <DialogContent>
          <TextField autoFocus fullWidth margin="dense" label={t("Name")} value={viewName} onChange={(e) => setViewName(e.target.value)}
            helperText={viewError || t("Saves the filters, search and sort of this list for you.")} error={!!viewError}
            onKeyDown={(e) => { if (e.key === "Enter" && viewName.trim()) saveView(); }} />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setNaming(false)}>{t("Cancel")}</Button>
          <Button variant="contained" disabled={!viewName.trim()} onClick={saveView}>{t("Save")}</Button>
        </DialogActions>
      </Dialog>
    </>
  );
  const panel = (
    <>
      <Collapse in={open && fields.length > 0} unmountOnExit>
        <Box sx={{ display: "flex", flexWrap: "wrap", gap: 1.5, p: 2, mb: 2, border: 1, borderColor: "divider", borderRadius: 2, bgcolor: "background.paper" }}>
          {fields.map(control)}
        </Box>
      </Collapse>
      {chips.length > 0 && (
          <Box sx={{ display: "flex", flexWrap: "wrap", alignItems: "center", gap: 1, mb: 2 }}>
            {chips.map((c) => <Chip key={c.key} label={c.label} onDelete={() => set(c.key.split(".")[0], c.clear)} />)}
            <Button size="small" onClick={() => onChange({})}>{t("Clear all")}</Button>
          </Box>
      )}
    </>
  );
  return { buttons, panel };
}
