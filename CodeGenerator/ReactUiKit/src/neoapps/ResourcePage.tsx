import React, { useCallback, useEffect, useMemo, useState } from "react";
import { useNavigate, useParams, useSearchParams } from "react-router-dom";
import {
  Alert, Autocomplete, Box, Button, Dialog, DialogActions, DialogContent, DialogContentText, DialogTitle, IconButton, InputAdornment, Paper, Skeleton,
  Snackbar, TextField, Typography,
} from "@mui/material";
import ClearIcon from "@mui/icons-material/Clear";
import AddIcon from "@mui/icons-material/Add";
import SearchIcon from "@mui/icons-material/Search";
import DownloadIcon from "@mui/icons-material/FileDownloadOutlined";
import { countWhere, deleteRecord, listRecords, queryRecords } from "./api";
import { useApp } from "./AppContext";
import { WorkflowButtons } from "./ai/Workflows";
import { DataTable, PAGE_SIZES } from "./DataTable";
import { useFilterBar } from "./FilterBar";
import { Cond, defaultSortOf, type Filters, parseSort, readFilters, toWhere, writeFilters } from "./listView";
import { exportCsv } from "./exportCsv";
import { ImportButton } from "./ImportCsv";
import { ShareDialog } from "./ShareDialog";
import ShareOutlined from "@mui/icons-material/ShareOutlined";
import { Icon } from "./icons";
import { RecordForm } from "./RecordForm";
import { invalidateReferences, useReferences } from "./useReferences";

import { t as tr, lower } from "./i18n";
/** Confirm + delete, shared by the list and record pages. */
export function useDelete(tableName: string, onDone: () => void) {
  const { tables } = useApp();
  const [target, setTarget] = useState<Record<string, any> | null>(null);
  const [error, setError] = useState<string>();
  const t = tables[tableName];
  const [blockers, setBlockers] = useState<string[] | null>(null);
  /** Deletes are soft (isActive = 0), so the database can't stop orphans: check related records first. */
  const ask = async (row: Record<string, any> | null) => {
    if (!row) return setTarget(null);
    const id = row[t.primaryKey];
    const refs = Object.values(tables).flatMap((c) => c.schema.columns
      .filter((col) => col.references && col.references.table === tableName)
      .map((col) => ({ child: c, column: col.name })));
    const counts = await Promise.all(refs.map(async (r) => ({ ...r, n: await countWhere(r.child.name, r.column, id) })));
    const used = counts.filter((c) => c.n > 0).map((c) => tr("{items}: {count}", { items: c.n === 1 ? c.child.singular : c.child.label, count: c.n }));
    if (used.length) setBlockers(used);
    else setTarget(row);
  };
  const confirm = async () => {
    if (!target) return;
    try {
      const res = await deleteRecord(tableName, target[t.primaryKey]);
      if (res && res.code === 0) throw new Error(res.message);
      invalidateReferences(tableName);
      setTarget(null);
      onDone();
    } catch (e: any) {
      setError(/foreign key|constraint/i.test(e.message) ? tr("This {item} is still used by other records.", { item: lower(t.singular) }) : e.message);
      setTarget(null);
    }
  };
  const dialog = (
    <>
      <Dialog open={!!target} onClose={() => setTarget(null)}>
        <DialogTitle>{tr("Delete {item}?", { item: lower(t?.singular) })}</DialogTitle>
        <DialogContent><DialogContentText>{tr("\"{name}\" will be removed. This can't be undone.", { name: target ? String(target[t.displayField] ?? target[t.primaryKey]) : "" })}</DialogContentText></DialogContent>
        <DialogActions>
          <Button onClick={() => setTarget(null)}>{tr("Cancel")}</Button>
          <Button color="error" variant="contained" onClick={confirm}>{tr("Delete")}</Button>
        </DialogActions>
      </Dialog>
      <Dialog open={!!blockers} onClose={() => setBlockers(null)}>
        <DialogTitle>{tr("Can't delete this {item} yet", { item: lower(t?.singular) })}</DialogTitle>
        <DialogContent><DialogContentText>{tr("It is still used by {items}. Delete or reassign those first.", { items: blockers?.join(", ") })}</DialogContentText></DialogContent>
        <DialogActions><Button variant="contained" onClick={() => setBlockers(null)}>{tr("OK")}</Button></DialogActions>
      </Dialog>
      <Snackbar open={!!error} autoHideDuration={6000} onClose={() => setError(undefined)}>
        <Alert severity="error" onClose={() => setError(undefined)}>{error}</Alert>
      </Snackbar>
    </>
  );
  return { ask, dialog };
}

// A fresh page per table: paging, search and sort never carry over from the previous list (a stale sort column made the
// API reject the first request of the next table).
export const ResourcePage: React.FC = () => {
  const { table = "" } = useParams();
  return <TablePage key={table} />;
};

const PAGE_SIZE_KEY = "neoapps.pageSize";
const storedPageSize = () => { try { const n = Number(localStorage.getItem(PAGE_SIZE_KEY)); return PAGE_SIZES.includes(n) ? n : 25; } catch { return 25; } };

const TablePage: React.FC = () => {
  const { table: name = "" } = useParams();
  const { tables, can, canOpen, canChangeRecord, ai, config } = useApp();
  const navigate = useNavigate();
  const table = tables[name];
  const [rows, setRows] = useState<Record<string, any>[]>([]);
  const [total, setTotal] = useState(0);
  const [loading, setLoading] = useState(true);
  const [loadedOnce, setLoadedOnce] = useState(false);
  const [error, setError] = useState<string>();
  const [exporting, setExporting] = useState(false);

  // Search, filters, sort and page live in the URL (shareable, bookmarkable, kept on back/forward).
  const [params, setParams] = useSearchParams();
  const update = useCallback((change: (p: URLSearchParams) => void, keepPage = false) => {
    setParams((prev) => {
      const next = new URLSearchParams(prev);
      change(next);
      if (!keepPage) next.delete("page");
      return next;
    }, { replace: true });
  }, [setParams]);
  const query = params.get("q") || "";
  const page = Math.max(0, (parseInt(params.get("page") || "1", 10) || 1) - 1);
  const pageSize = (() => { const n = parseInt(params.get("size") || "", 10); return PAGE_SIZES.includes(n) ? n : storedPageSize(); })();
  const sortText = params.get("sort") || (table ? defaultSortOf(table) : "");
  const sort = parseSort(sortText);
  const paramKey = params.toString();
  const filters = useMemo(() => readFilters(new URLSearchParams(paramKey)), [paramKey]);
  const setFilters = (f: Filters) => update((p) => { const next = writeFilters(p, f); Array.from(p.keys()).forEach((k) => p.delete(k)); next.forEach((v, k) => p.append(k, v)); });

  // the search box filters as you type (after a short pause); Enter searches at once
  const [search, setSearch] = useState(query);
  useEffect(() => { setSearch(query); }, [query]);
  useEffect(() => {
    if (search.trim() === query.trim()) return;
    const timer = setTimeout(() => update((p) => { if (search.trim()) p.set("q", search.trim()); else p.delete("q"); }), 350);
    return () => clearTimeout(timer);
  }, [search, query, update]);

  // Optional parent picker (ui.config "scopePicker": {"table": "cases"}): lists of tables with a foreign key to that table
  // can be limited to one parent record; the choice is kept in the URL (?scope=ID) and remembered per parent table.
  const scopeName = config.scopePicker?.table;
  const scopeTable = scopeName ? tables[scopeName] : undefined;
  const scopeCol = scopeTable && name !== scopeName ? table?.schema.columns.find((c) => c.references?.table === scopeName)?.name : undefined;
  const scopeKey = "neoapps.scope." + scopeName;
  const [scopeId, setScopeIdState] = useState<string>(() => params.get("scope") ?? (() => { try { return localStorage.getItem(scopeKey) || ""; } catch { return ""; } })());
  const [scopeOptions, setScopeOptions] = useState<Record<string, any>[]>([]);
  const setScopeId = (v: string) => {
    setScopeIdState(v);
    try { if (v) localStorage.setItem(scopeKey, v); else localStorage.removeItem(scopeKey); } catch { /* storage unavailable */ }
    update((p) => { if (v) p.set("scope", v); else p.delete("scope"); });
  };
  useEffect(() => {
    if (!scopeCol || !scopeTable) return;
    listRecords(scopeTable.name, 1, 1000, scopeTable.defaultSort || undefined).then((p) => setScopeOptions(p.records)).catch(() => setScopeOptions([]));
  }, [scopeCol, scopeTable]);
  const scopeLabel = (r: Record<string, any>) => String(r[scopeTable?.displayField || ""] ?? `#${r[scopeTable?.primaryKey || "id"]}`);
  // memoised: RecordForm re-initialises its fields whenever this object changes
  const scopeDefaults = useMemo(() => {
    const d: Record<string, any> = {};
    if (scopeCol && scopeId) d[scopeCol] = Number(scopeId) || scopeId;
    // a list filtered to one parent (e.g. ?f.project_id=7) creates new records for that parent
    for (const [col, f] of Object.entries(filters)) if (f.eq && table?.fields[col]?.widget === "reference") d[col] = Number(f.eq) || f.eq;
    return Object.keys(d).length ? d : undefined;
  }, [scopeCol, scopeId, filters, table]);
  const [editing, setEditing] = useState<Record<string, any> | null | undefined>(undefined);
  const [reload, setReload] = useState(0);
  const [sharing, setSharing] = useState(false);
  const { refs, labelFor } = useReferences(table, reload);

  const where = useMemo(() => {
    if (!table) return [];
    const w = toWhere(table, filters);
    if (scopeCol && scopeId) w.push({ columnName: scopeCol, columnCondition: Cond.Equals, columnValue: String(scopeId) });
    return w;
  }, [table, filters, scopeCol, scopeId]);

  const load = useCallback(async () => {
    if (!table || !canOpen(table.name)) return;
    setLoading(true);
    setError(undefined);
    try {
      const res = await queryRecords(table.name, page + 1, pageSize, sortText || undefined, query, where);
      setRows(res.records);
      setTotal(res.total);
      // a page past the end (fewer rows than before): go to the last page
      if (!res.records.length && res.total > 0 && page > 0) update((p) => p.set("page", String(Math.ceil(res.total / pageSize))), true);
    } catch (e: any) {
      setError(e.message);
      setRows([]);
    } finally {
      setLoading(false);
      setLoadedOnce(true);
    }
  }, [table, page, pageSize, sortText, query, where, canOpen, update]);

  useEffect(() => {
    load();
  }, [load, reload]);

  const del = useDelete(name, () => setReload((r) => r + 1));
  const filterBar = useFilterBar({
    table, filters, onChange: setFilters, refs, q: query, sort: params.get("sort") || undefined,
    onApplyView: (v) => update((p) => {
      Array.from(p.keys()).filter((k) => k.startsWith("f.") || k === "q" || k === "sort").forEach((k) => p.delete(k));
      writeFilters(new URLSearchParams(), v.filters || {}).forEach((val, k) => p.set(k, val));
      if (v.q) p.set("q", v.q);
      if (v.sort) p.set("sort", v.sort);
    }),
  });

  // the app's tables arrive just after sign-in: show nothing until then instead of "Unknown table"
  if (!table) return Object.keys(tables).length === 0 ? null : <Alert severity="warning">{tr("Unknown table \"{name}\".", { name })}</Alert>;
  if (!canOpen(name)) return <Alert severity="info">{tr("This page isn't available for your role.")}</Alert>;
  if (!can(name, "read")) return <Alert severity="info">{tr("You don't have access to {items}. Ask an administrator.", { items: table.label })}</Alert>;

  const filtered = where.length > 0 || !!query;
  return (
    <Box>
      <Box sx={{ display: "flex", alignItems: { sm: "center" }, flexDirection: { xs: "column", sm: "row" }, flexWrap: "wrap", gap: 2, mb: 2.5 }}>
        <Box sx={{ display: "flex", alignItems: "center", gap: 1.5, flex: 1, minWidth: 220 }}>
          <Box sx={{ width: 40, height: 40, borderRadius: 2, bgcolor: "primary.main", color: "primary.contrastText", display: "grid", placeItems: "center" }}>
            <Icon name={table.icon} />
          </Box>
          <Box>
            <Typography variant="h5" sx={{
              fontWeight: 700
            }}>{table.label}</Typography>
            <Typography variant="body2" component="div" sx={{
              color: "text.secondary"
            }}>{table.description || (!loadedOnce ? <Skeleton width={90} /> : filtered
              ? tr("{count} matching", { count: total })
              : tr("{items}: {count}", { items: total === 1 ? table.singular : table.label, count: total }))}</Typography>
          </Box>
        </Box>
        <form onSubmit={(e) => { e.preventDefault(); update((p) => { if (search.trim()) p.set("q", search.trim()); else p.delete("q"); }); }}
          style={{ display: "flex", gap: 8, flexWrap: "wrap" }}>
          {scopeCol && scopeTable && (
            <Autocomplete
              size="small" sx={{ width: { xs: "100%", sm: 280 }, bgcolor: "background.paper" }}
              options={scopeOptions}
              value={scopeOptions.find((o) => String(o[scopeTable.primaryKey]) === String(scopeId)) || null}
              onChange={(_, v) => setScopeId(v ? String(v[scopeTable.primaryKey]) : "")}
              getOptionLabel={scopeLabel}
              isOptionEqualToValue={(a, b) => String(a[scopeTable.primaryKey]) === String(b[scopeTable.primaryKey])}
              renderInput={(p) => <TextField {...p} label={scopeTable.singular} placeholder={tr("All {items}", { items: lower(scopeTable.label) })} />}
            />
          )}
          <TextField size="small" placeholder={tr("Search {items}", { items: lower(table.label) })} value={search} onChange={(e) => setSearch(e.target.value)}
            sx={{ width: { xs: "100%", sm: 260 }, bgcolor: "background.paper" }} slotProps={{
            input: {
              startAdornment: <InputAdornment position="start"><SearchIcon fontSize="small" /></InputAdornment>,
              endAdornment: search ? <InputAdornment position="end"><IconButton size="small" aria-label={tr("Clear")} onClick={() => setSearch("")}><ClearIcon fontSize="small" /></IconButton></InputAdornment> : undefined,
            }
          }} />
        </form>
        {filterBar.buttons}
        {ai?.enabled && <WorkflowButtons workflows={ai.workflows.filter((w) => !w.table && w.targets[0] === name)} onApplied={() => setReload((r) => r + 1)} />}
        {total > 0 && (
          <Button variant="outlined" startIcon={<DownloadIcon />} disabled={exporting} onClick={async () => {
            setExporting(true);
            try { await exportCsv(table, sortText || undefined, query, labelFor, where); }
            catch (e: any) { setError(e.message || tr("Export failed")); }
            finally { setExporting(false); }
          }}>{exporting ? tr("Exporting…") : tr("Export")}</Button>
        )}
        {can(name, "create") && (config as any).sharing !== false && (
          <Button variant="outlined" startIcon={<ShareOutlined />} onClick={() => setSharing(true)}>{tr("Share form")}</Button>
        )}
        {can(name, "create") && <ImportButton table={table} onDone={() => setReload((r) => r + 1)} />}
        {can(name, "create") && (
          <Button variant="contained" startIcon={<AddIcon />} onClick={() => setEditing(null)}>{tr("New {item}", { item: lower(table.singular) })}</Button>
        )}
      </Box>
      {filterBar.panel}
      {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
      <Paper variant="outlined" sx={{ borderRadius: 2, overflow: "hidden" }}>
        <DataTable
          table={table} columns={table.listColumns} rows={rows} total={total} loading={loading} page={page} pageSize={pageSize} sort={sort}
          labelFor={labelFor}
          onPage={(p, size) => {
            if (size !== pageSize) { try { localStorage.setItem(PAGE_SIZE_KEY, String(size)); } catch { /* storage unavailable */ } }
            update((q) => { if (p > 0) q.set("page", String(p + 1)); else q.delete("page"); if (size !== 25) q.set("size", String(size)); else q.delete("size"); }, true);
            window.scrollTo({ top: 0 });
          }}
          onSort={(c) => update((p) => p.set("sort", `${c}|${sort?.column === c && sort.dir === "ASC" ? "DESC" : "ASC"}`))}
          onOpen={(r) => navigate(`/r/${name}/${r[table.primaryKey]}`)}
          onEdit={can(name, "update") ? (r) => setEditing(r) : undefined}
          onDelete={can(name, "delete") ? (r) => del.ask(r) : undefined}
          canEdit={(r) => canChangeRecord(name, r, "update")}
          canDelete={(r) => canChangeRecord(name, r, "delete")}
          emptyText={query ? tr("No {items} match \"{query}\".", { items: lower(table.label), query })
            : where.length ? tr("No {items} match these filters.", { items: lower(table.label) }) : undefined}
        />
      </Paper>
      <ShareDialog table={table} open={sharing} onClose={() => setSharing(false)} />
      <RecordForm table={table} record={editing || null} defaults={scopeDefaults} open={editing !== undefined} onClose={() => setEditing(undefined)}
        onSaved={() => { setEditing(undefined); setReload((r) => r + 1); }} />
      {del.dialog}
    </Box>
  );
};
