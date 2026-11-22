import React, { useCallback, useEffect, useMemo, useState } from "react";
import { useNavigate, useParams, useSearchParams } from "react-router-dom";
import {
  Alert, Autocomplete, Box, Button, Dialog, DialogActions, DialogContent, DialogContentText, DialogTitle, InputAdornment, Paper, Snackbar,
  TextField, Typography,
} from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import SearchIcon from "@mui/icons-material/Search";
import DownloadIcon from "@mui/icons-material/FileDownloadOutlined";
import { api, countWhere, deleteRecord, listRecords } from "./api";
import { useApp } from "./AppContext";
import { WorkflowButtons } from "./ai/Workflows";
import { DataTable } from "./DataTable";
import { exportCsv } from "./exportCsv";
import { ImportButton } from "./ImportCsv";
import { ShareDialog } from "./ShareDialog";
import ShareOutlined from "@mui/icons-material/ShareOutlined";
import { Icon } from "./icons";
import { RecordForm } from "./RecordForm";
import { invalidateReferences, useReferences } from "./useReferences";

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
    const used = counts.filter((c) => c.n > 0).map((c) => `${c.n} ${c.n === 1 ? c.child.singular.toLowerCase() : c.child.label.toLowerCase()}`);
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
      setError(/foreign key|constraint/i.test(e.message) ? `This ${t.singular.toLowerCase()} is still used by other records.` : e.message);
      setTarget(null);
    }
  };
  const dialog = (
    <>
      <Dialog open={!!target} onClose={() => setTarget(null)}>
        <DialogTitle>Delete {t?.singular.toLowerCase()}?</DialogTitle>
        <DialogContent><DialogContentText>"{target ? String(target[t.displayField] ?? target[t.primaryKey]) : ""}" will be removed. This can't be undone.</DialogContentText></DialogContent>
        <DialogActions>
          <Button onClick={() => setTarget(null)}>Cancel</Button>
          <Button color="error" variant="contained" onClick={confirm}>Delete</Button>
        </DialogActions>
      </Dialog>
      <Dialog open={!!blockers} onClose={() => setBlockers(null)}>
        <DialogTitle>Can't delete this {t?.singular.toLowerCase()} yet</DialogTitle>
        <DialogContent><DialogContentText>It is still used by {blockers?.join(", ")}. Delete or reassign those first.</DialogContentText></DialogContent>
        <DialogActions><Button variant="contained" onClick={() => setBlockers(null)}>OK</Button></DialogActions>
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

const TablePage: React.FC = () => {
  const { table: name = "" } = useParams();
  const { tables, can, canOpen, canChangeRecord, ai, config } = useApp();
  const navigate = useNavigate();
  const table = tables[name];
  const [rows, setRows] = useState<Record<string, any>[]>([]);
  const [total, setTotal] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string>();
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(25);
  const [search, setSearch] = useState("");
  const [exporting, setExporting] = useState(false);
  const [query, setQuery] = useState("");
  // Optional parent picker (ui.config "scopePicker": {"table": "cases"}): lists of tables with a foreign key to that table
  // can be limited to one parent record; the choice is kept in the URL (?scope=ID) and remembered per parent table.
  const scopeName = config.scopePicker?.table;
  const scopeTable = scopeName ? tables[scopeName] : undefined;
  const scopeCol = scopeTable && name !== scopeName ? table?.schema.columns.find((c) => c.references?.table === scopeName)?.name : undefined;
  const scopeKey = "neoapps.scope." + scopeName;
  const [params, setParams] = useSearchParams();
  const [scopeId, setScopeIdState] = useState<string>(() => params.get("scope") ?? (() => { try { return localStorage.getItem(scopeKey) || ""; } catch { return ""; } })());
  const [scopeOptions, setScopeOptions] = useState<Record<string, any>[]>([]);
  const setScopeId = (v: string) => {
    setScopeIdState(v);
    try { if (v) localStorage.setItem(scopeKey, v); else localStorage.removeItem(scopeKey); } catch { /* storage unavailable */ }
    setPage(0);
    const next = new URLSearchParams(params);
    if (v) next.set("scope", v); else next.delete("scope");
    setParams(next, { replace: true });
  };
  useEffect(() => {
    if (!scopeCol || !scopeTable) return;
    listRecords(scopeTable.name, 1, 1000, scopeTable.defaultSort || undefined).then((p) => setScopeOptions(p.records)).catch(() => setScopeOptions([]));
  }, [scopeCol, scopeTable]);
  const scopeLabel = (r: Record<string, any>) => String(r[scopeTable?.displayField || ""] ?? `#${r[scopeTable?.primaryKey || "id"]}`);
  // memoised: RecordForm re-initialises its fields whenever this object changes
  const scopeDefaults = useMemo(() => (scopeCol && scopeId ? { [scopeCol]: Number(scopeId) || scopeId } : undefined), [scopeCol, scopeId]);
  const [sort, setSort] = useState<{ column: string; dir: "ASC" | "DESC" } | undefined>();
  const [editing, setEditing] = useState<Record<string, any> | null | undefined>(undefined);
  const [reload, setReload] = useState(0);
  const [sharing, setSharing] = useState(false);
  const { labelFor } = useReferences(table, reload);

  useEffect(() => {
    setPage(0);
    setQuery("");
    setSearch("");
    if (table?.defaultSort) {
      const [c, d] = table.defaultSort.split("|");
      setSort({ column: c, dir: (d || "ASC").toUpperCase() === "DESC" ? "DESC" : "ASC" });
    } else setSort(undefined);
  }, [name, table?.defaultSort]);

  const load = useCallback(async () => {
    if (!table || !canOpen(table.name)) return;
    setLoading(true);
    setError(undefined);
    try {
      const orderBy = sort ? `${sort.column}|${sort.dir}` : undefined;
      if (scopeCol && scopeId) {
        const where = [{ columnName: scopeCol, columnCondition: 5, columnValue: String(scopeId) }];
        const q = query.trim().toLowerCase();
        const qs = new URLSearchParams({ page: String(q ? 1 : page + 1), itemsPerPage: String(q ? 1000 : pageSize) });
        if (orderBy) qs.set("orderBy", orderBy);
        const res = await api<any>("POST", `${table.name}/filter?${qs}`, where);
        const doc = res && res.code === 1 ? res.document : null;
        let recs: Record<string, any>[] = doc?.records || [];
        if (q) {
          recs = recs.filter((r) => Object.values(r).some((v) => v !== null && v !== undefined && String(v).toLowerCase().includes(q)));
          setTotal(recs.length);
          setRows(recs.slice(page * pageSize, page * pageSize + pageSize));
        } else {
          setRows(recs);
          setTotal(doc?.totalRecords || 0);
        }
      } else {
        const res = await listRecords(table.name, page + 1, pageSize, orderBy, query);
        setRows(res.records);
        setTotal(res.total);
      }
    } catch (e: any) {
      setError(e.message);
      setRows([]);
    } finally {
      setLoading(false);
    }
  }, [table, page, pageSize, sort, query, canOpen, scopeCol, scopeId]);

  useEffect(() => {
    load();
  }, [load, reload]);

  const del = useDelete(name, () => setReload((r) => r + 1));

  if (!table) return <Alert severity="warning">Unknown table "{name}".</Alert>;
  if (!canOpen(name)) return <Alert severity="info">This page isn't available for your role.</Alert>;
  if (!can(name, "read")) return <Alert severity="info">You don't have access to {table.label}. Ask an administrator.</Alert>;

  return (
    <Box>
      <Box sx={{ display: "flex", alignItems: { sm: "center" }, flexDirection: { xs: "column", sm: "row" }, gap: 2, mb: 2.5 }}>
        <Box sx={{ display: "flex", alignItems: "center", gap: 1.5, flex: 1 }}>
          <Box sx={{ width: 40, height: 40, borderRadius: 2, bgcolor: "primary.main", color: "primary.contrastText", display: "grid", placeItems: "center" }}>
            <Icon name={table.icon} />
          </Box>
          <Box>
            <Typography variant="h5" sx={{
              fontWeight: 700
            }}>{table.label}</Typography>
            <Typography variant="body2" sx={{
              color: "text.secondary"
            }}>{table.description || `${total} ${total === 1 ? table.singular.toLowerCase() : table.label.toLowerCase()}`}</Typography>
          </Box>
        </Box>
        <form onSubmit={(e) => { e.preventDefault(); setPage(0); setQuery(search); }}>
          {scopeCol && scopeTable && (
            <Autocomplete
              size="small" sx={{ width: { xs: "100%", sm: 280 }, bgcolor: "background.paper" }}
              options={scopeOptions}
              value={scopeOptions.find((o) => String(o[scopeTable.primaryKey]) === String(scopeId)) || null}
              onChange={(_, v) => setScopeId(v ? String(v[scopeTable.primaryKey]) : "")}
              getOptionLabel={scopeLabel}
              isOptionEqualToValue={(a, b) => String(a[scopeTable.primaryKey]) === String(b[scopeTable.primaryKey])}
              renderInput={(p) => <TextField {...p} label={scopeTable.singular} placeholder={`All ${scopeTable.label.toLowerCase()}`} />}
            />
          )}
          <TextField size="small" placeholder={`Search ${table.label.toLowerCase()}`} value={search} onChange={(e) => setSearch(e.target.value)}
            sx={{ width: { xs: "100%", sm: 260 }, bgcolor: "background.paper" }} slotProps={{
            input: { startAdornment: <InputAdornment position="start"><SearchIcon fontSize="small" /></InputAdornment> }
          }} />
        </form>
        {ai?.enabled && <WorkflowButtons workflows={ai.workflows.filter((w) => !w.table && w.targets[0] === name)} onApplied={() => setReload((r) => r + 1)} />}
        {total > 0 && (
          <Button variant="outlined" startIcon={<DownloadIcon />} disabled={exporting} onClick={async () => {
            setExporting(true);
            try { await exportCsv(table, sort ? `${sort.column}|${sort.dir}` : undefined, query, labelFor); }
            catch (e: any) { setError(e.message || "Export failed"); }
            finally { setExporting(false); }
          }}>{exporting ? "Exporting…" : "Export"}</Button>
        )}
        {can(name, "create") && (config as any).sharing !== false && (
          <Button variant="outlined" startIcon={<ShareOutlined />} onClick={() => setSharing(true)}>Share form</Button>
        )}
        {can(name, "create") && <ImportButton table={table} onDone={() => setReload((r) => r + 1)} />}
        {can(name, "create") && (
          <Button variant="contained" startIcon={<AddIcon />} onClick={() => setEditing(null)}>New {table.singular.toLowerCase()}</Button>
        )}
      </Box>
      {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
      <Paper variant="outlined" sx={{ borderRadius: 2, overflow: "hidden" }}>
        <DataTable
          table={table} columns={table.listColumns} rows={rows} total={total} loading={loading} page={page} pageSize={pageSize} sort={sort}
          labelFor={labelFor}
          onPage={(p, s) => { setPage(p); setPageSize(s); }}
          onSort={(c) => setSort((s) => ({ column: c, dir: s?.column === c && s.dir === "ASC" ? "DESC" : "ASC" }))}
          onOpen={(r) => navigate(`/r/${name}/${r[table.primaryKey]}`)}
          onEdit={can(name, "update") ? (r) => setEditing(r) : undefined}
          onDelete={can(name, "delete") ? (r) => del.ask(r) : undefined}
          canEdit={(r) => canChangeRecord(name, r, "update")}
          canDelete={(r) => canChangeRecord(name, r, "delete")}
          emptyText={query ? `No ${table.label.toLowerCase()} match "${query}".` : undefined}
        />
      </Paper>
      <ShareDialog table={table} open={sharing} onClose={() => setSharing(false)} />
      <RecordForm table={table} record={editing || null} defaults={scopeDefaults} open={editing !== undefined} onClose={() => setEditing(undefined)}
        onSaved={() => { setEditing(undefined); setReload((r) => r + 1); }} />
      {del.dialog}
    </Box>
  );
};
