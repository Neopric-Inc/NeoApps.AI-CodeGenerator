import React, { useEffect, useMemo, useState } from "react";
import {
  Alert, Box, Button, Checkbox, CircularProgress, Dialog, DialogActions, DialogContent, DialogTitle, Table, TableBody, TableCell,
  TableHead, TableRow, TextField, Typography,
} from "@mui/material";
import AutoAwesome from "@mui/icons-material/AutoAwesome";
import { useApp } from "../AppContext";
import { AiWorkflow, ProposedRow, WorkflowPreview } from "./aiApi";
import { approveJob, cancelJob, FINISHED, rejectJob, startJob, useJobFeed } from "../jobs/jobsApi";
import { JobFeed, JobStatusChip } from "../jobs/JobFeed";

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

export const WorkflowDialog: React.FC<{ wf: AiWorkflow; recordId?: string; jobId?: string; onClose: () => void; onApplied?: () => void }> = ({
  wf, recordId, jobId: existingJob, onClose, onApplied,
}) => {
  const { tables } = useApp();
  const [note, setNote] = useState("");
  const [jobId, setJobId] = useState<string | undefined>(existingJob);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string>();
  const [preview, setPreview] = useState<WorkflowPreview>();
  const [excluded, setExcluded] = useState<Set<string>>(new Set());
  const { job, events, error: feedError } = useJobFeed(jobId);
  const status = job?.status;

  // The proposal arrives as the job's result when it pauses for approval; keep it while the rows are being created.
  useEffect(() => {
    if (status === "awaiting_approval" && job?.result) setPreview(job.result as WorkflowPreview);
  }, [status, job?.result]);
  const appliedOnce = React.useRef(false);
  useEffect(() => {
    if (status === "completed" && job?.result?.created && !appliedOnce.current) {
      appliedOnce.current = true;
      onApplied?.();
    }
  }, [status, job?.result, onApplied]);

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

  const act = async (fn: () => Promise<any>) => {
    setBusy(true);
    setError(undefined);
    try {
      await fn();
    } catch (e: any) {
      setError(e.message);
    } finally {
      setBusy(false);
    }
  };
  const run = () => act(async () => {
    const j = await startJob("ai.workflow", { workflow: wf.id, recordId, note: note || undefined });
    setPreview(undefined);
    setExcluded(new Set());
    appliedOnce.current = false;
    setJobId(j.job_id);
  });
  const apply = () => act(async () => {
    if (!preview || !jobId) return;
    await approveJob(jobId, { records: preview.records.filter((r) => !skipped.has(r.ref)) });
  });
  const reject = () => act(async () => { if (jobId) await rejectJob(jobId); });
  const cancel = () => act(async () => { if (jobId) await cancelJob(jobId); });
  const toggle = (ref: string) => setExcluded((s) => {
    const n = new Set(s);
    if (n.has(ref)) n.delete(ref);
    else n.add(ref);
    return n;
  });
  const count = (preview?.records.length || 0) - skipped.size;
  const reviewing = status === "awaiting_approval" && !!preview;
  const working = !!jobId && (!status || status === "queued" || status === "running" || status === "approved");
  const finished = !!status && FINISHED.includes(status);

  return (
    <Dialog open onClose={onClose} maxWidth={reviewing ? "lg" : "sm"} fullWidth>
      <DialogTitle sx={{ display: "flex", alignItems: "center", gap: 1 }}>
        <AutoAwesome color="primary" fontSize="small" />{wf.label}
        {status && <Box sx={{ ml: "auto" }}><JobStatusChip status={status} /></Box>}
      </DialogTitle>
      <DialogContent dividers>
        {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
        {!jobId && (
          <>
            <Typography
              variant="body2"
              sx={{
                color: "text.secondary",
                mb: 2
              }}>
              {wf.description || "The assistant reads the source and proposes new records."} It runs in the background and nothing is saved until you review the proposal and approve it.
            </Typography>
            <TextField fullWidth multiline minRows={2} label="Anything to focus on? (optional)" value={note} onChange={(e) => setNote(e.target.value)} disabled={busy} />
          </>
        )}
        {jobId && !reviewing && (
          <>
            {status === "completed" && <Alert severity="success" sx={{ mb: 2 }}>{job?.result?.summary || "Done."}</Alert>}
            {(status === "cancelled" || status === "rejected") && <Alert severity="info" sx={{ mb: 2 }}>{status === "cancelled" ? "Cancelled." : "Rejected: nothing was created."}</Alert>}
            <JobFeed job={job} events={events} error={feedError} />
            {working && <Typography
              variant="caption"
              sx={{
                color: "text.secondary",
                display: "block",
                mt: 1
              }}>You can close this window: the job keeps running, and you'll find it under Background jobs.</Typography>}
          </>
        )}
        {reviewing && preview && (
          <>
            <Alert severity="info" icon={false} sx={{ mb: 2 }}>{preview.summary}</Alert>
            {preview.warnings.map((w) => <Alert key={w} severity="warning" sx={{ mb: 1 }}>{w}</Alert>)}
            {preview.records.length === 0 && <Typography sx={{
              color: "text.secondary"
            }}>Nothing to create.</Typography>}
            {Object.entries(byTable).map(([table, rows]) => {
              const t = tables[table];
              const cols = Array.from(new Set(rows.flatMap((r) => Object.keys(r.values))));
              return (
                <Box key={table} sx={{ mb: 3 }}>
                  <Typography variant="subtitle2" sx={{ mb: 0.5 }}>{t?.label || table} <Typography component="span" variant="caption" sx={{
                    color: "text.secondary"
                  }}>({rows.filter((r) => !skipped.has(r.ref)).length} of {rows.length})</Typography></Typography>
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
            <Typography
              variant="subtitle2"
              sx={{
                color: "text.secondary",
                mb: 0.5
              }}>How it got here</Typography>
            <JobFeed job={job} events={events} error={feedError} maxHeight={140} />
          </>
        )}
      </DialogContent>
      <DialogActions>
        {working && <Button color="inherit" onClick={cancel} disabled={busy}>Cancel job</Button>}
        <Button onClick={onClose}>{working ? "Run in background" : finished || reviewing ? "Close" : "Cancel"}</Button>
        {!jobId && <Button variant="contained" onClick={run} disabled={busy} startIcon={<AutoAwesome />}>Generate proposal</Button>}
        {finished && status !== "completed" && <Button variant="contained" onClick={run} disabled={busy} startIcon={<AutoAwesome />}>Try again</Button>}
        {reviewing && (
          <>
            <Button color="inherit" onClick={reject} disabled={busy}>Reject</Button>
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
