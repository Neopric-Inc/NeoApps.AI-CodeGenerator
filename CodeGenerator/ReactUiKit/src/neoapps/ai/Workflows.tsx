import React, { useMemo, useState } from "react";
import {
  Alert, Box, Button, Checkbox, CircularProgress, Dialog, DialogActions, DialogContent, DialogTitle, Table, TableBody, TableCell,
  TableHead, TableRow, TextField, Typography,
} from "@mui/material";
import AutoAwesome from "@mui/icons-material/AutoAwesome";
import { useApp } from "../AppContext";
import { AiWorkflow, applyWorkflow, previewWorkflow, ProposedRow, WorkflowPreview } from "./aiApi";

/** Rows whose foreign keys point (directly or not) at an excluded row can't be created either. */
function withDependents(rows: ProposedRow[], excluded: Set<string>): Set<string> {
  const out = new Set(excluded);
  let changed = true;
  while (changed) {
    changed = false;
    for (const r of rows)
      if (!out.has(r.ref) && Object.values(r.values).some((v) => typeof v === "string" && v.startsWith("@") && out.has(v.slice(1)))) {
        out.add(r.ref);
        changed = true;
      }
  }
  return out;
}

const WorkflowDialog: React.FC<{ wf: AiWorkflow; recordId?: string; onClose: () => void; onApplied?: () => void }> = ({ wf, recordId, onClose, onApplied }) => {
  const { tables } = useApp();
  const [note, setNote] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string>();
  const [preview, setPreview] = useState<WorkflowPreview>();
  const [excluded, setExcluded] = useState<Set<string>>(new Set());
  const [done, setDone] = useState<string>();

  const skipped = useMemo(() => withDependents(preview?.records || [], excluded), [preview, excluded]);
  const byTable = useMemo(() => {
    const g: Record<string, ProposedRow[]> = {};
    (preview?.records || []).forEach((r) => (g[r.table] = g[r.table] || []).push(r));
    return g;
  }, [preview]);
  const refLabel = (ref: string) => {
    const row = preview?.records.find((r) => r.ref === ref);
    if (!row) return ref;
    const t = tables[row.table];
    return String((t && row.values[t.displayField]) ?? Object.values(row.values).find((v) => typeof v === "string" && !v.startsWith("@")) ?? ref);
  };

  const run = async () => {
    setBusy(true);
    setError(undefined);
    try {
      setPreview(await previewWorkflow(wf.id, recordId, note || undefined));
      setExcluded(new Set());
    } catch (e: any) {
      setError(e.message);
    } finally {
      setBusy(false);
    }
  };
  const apply = async () => {
    if (!preview) return;
    setBusy(true);
    setError(undefined);
    try {
      const res = await applyWorkflow(wf.id, preview.records.filter((r) => !skipped.has(r.ref)), recordId);
      setDone(res.summary);
      onApplied?.();
    } catch (e: any) {
      setError(e.message);
    } finally {
      setBusy(false);
    }
  };
  const toggle = (ref: string) => setExcluded((s) => {
    const n = new Set(s);
    if (n.has(ref)) n.delete(ref);
    else n.add(ref);
    return n;
  });
  const count = (preview?.records.length || 0) - skipped.size;

  return (
    <Dialog open onClose={busy ? undefined : onClose} maxWidth={preview ? "lg" : "sm"} fullWidth>
      <DialogTitle sx={{ display: "flex", alignItems: "center", gap: 1 }}><AutoAwesome color="primary" fontSize="small" />{wf.label}</DialogTitle>
      <DialogContent dividers>
        {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
        {done ? (
          <Alert severity="success">{done}</Alert>
        ) : !preview ? (
          <>
            <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
              {wf.description || "The assistant reads the source and proposes new records."} Nothing is saved until you review the proposal and apply it.
            </Typography>
            <TextField fullWidth multiline minRows={2} label="Anything to focus on? (optional)" value={note} onChange={(e) => setNote(e.target.value)} disabled={busy} />
            {busy && (
              <Box sx={{ display: "flex", alignItems: "center", gap: 1.5, mt: 3, color: "text.secondary" }}>
                <CircularProgress size={20} /><Typography variant="body2">Working… this can take a minute for long documents.</Typography>
              </Box>
            )}
          </>
        ) : (
          <>
            <Alert severity="info" icon={false} sx={{ mb: 2 }}>{preview.summary}</Alert>
            {preview.warnings.map((w) => <Alert key={w} severity="warning" sx={{ mb: 1 }}>{w}</Alert>)}
            {preview.records.length === 0 && <Typography color="text.secondary">Nothing to create.</Typography>}
            {Object.entries(byTable).map(([table, rows]) => {
              const t = tables[table];
              const cols = Array.from(new Set(rows.flatMap((r) => Object.keys(r.values))));
              return (
                <Box key={table} sx={{ mb: 3 }}>
                  <Typography variant="subtitle2" sx={{ mb: 0.5 }}>{t?.label || table} <Typography component="span" variant="caption" color="text.secondary">({rows.filter((r) => !skipped.has(r.ref)).length} of {rows.length})</Typography></Typography>
                  <Box sx={{ overflowX: "auto", border: 1, borderColor: "divider", borderRadius: 1 }}>
                    <Table size="small">
                      <TableHead>
                        <TableRow>
                          <TableCell padding="checkbox" />
                          {cols.map((c) => <TableCell key={c} sx={{ fontWeight: 600, whiteSpace: "nowrap" }}>{t?.fields[c]?.label || c}</TableCell>)}
                        </TableRow>
                      </TableHead>
                      <TableBody>
                        {rows.map((r) => (
                          <TableRow key={r.ref} sx={{ opacity: skipped.has(r.ref) ? 0.4 : 1 }}>
                            <TableCell padding="checkbox">
                              <Checkbox size="small" checked={!skipped.has(r.ref)} disabled={!excluded.has(r.ref) && skipped.has(r.ref)} onChange={() => toggle(r.ref)} />
                            </TableCell>
                            {cols.map((c) => {
                              const v = r.values[c];
                              return (
                                <TableCell key={c} sx={{ maxWidth: 320, fontSize: 13 }}>
                                  {typeof v === "string" && v.startsWith("@") ? <i>→ {refLabel(v.slice(1))}</i> : v == null ? "—" : String(v)}
                                </TableCell>
                              );
                            })}
                          </TableRow>
                        ))}
                      </TableBody>
                    </Table>
                  </Box>
                </Box>
              );
            })}
          </>
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} disabled={busy}>{done ? "Close" : "Cancel"}</Button>
        {!done && !preview && <Button variant="contained" onClick={run} disabled={busy} startIcon={<AutoAwesome />}>Generate proposal</Button>}
        {!done && preview && (
          <>
            <Button onClick={run} disabled={busy}>Regenerate</Button>
            <Button variant="contained" onClick={apply} disabled={busy || count === 0}>
              {busy ? <CircularProgress size={18} color="inherit" /> : `Create ${count} record${count === 1 ? "" : "s"}`}
            </Button>
          </>
        )}
      </DialogActions>
    </Dialog>
  );
};

/** One button per workflow the user may run (needs create permission on every target table). */
export const WorkflowButtons: React.FC<{ workflows: AiWorkflow[]; recordId?: string; record?: Record<string, any>; vertical?: boolean; onApplied?: () => void }> = ({
  workflows, recordId, record, vertical, onApplied,
}) => {
  const { can } = useApp();
  const [active, setActive] = useState<AiWorkflow | null>(null);
  const visible = workflows.filter((w) =>
    w.targets.every((t) => can(t, "create")) &&
    Object.entries(w.when || {}).every(([k, v]) => !record || String(record[k] ?? "").toLowerCase() === String(v).toLowerCase())
  );
  if (!visible.length) return null;
  return (
    <>
      <Box sx={{ display: "flex", flexDirection: vertical ? "column" : "row", gap: 1, mt: vertical ? 1 : 0, flexWrap: "wrap" }}>
        {visible.map((w) => (
          <Button key={w.id} variant="outlined" size={vertical ? "small" : "medium"} startIcon={<AutoAwesome />} onClick={() => setActive(w)}
            sx={{ justifyContent: vertical ? "flex-start" : undefined }}>{w.label}</Button>
        ))}
      </Box>
      {active && <WorkflowDialog wf={active} recordId={recordId} onClose={() => setActive(null)} onApplied={onApplied} />}
    </>
  );
};
