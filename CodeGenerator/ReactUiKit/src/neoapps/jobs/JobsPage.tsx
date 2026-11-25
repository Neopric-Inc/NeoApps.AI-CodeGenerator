import React, { useCallback, useEffect, useState } from "react";
import {
  Alert, Box, Button, Dialog, DialogActions, DialogContent, DialogTitle, Paper, Table, TableBody, TableCell, TableHead, TableRow,
  Typography,
} from "@mui/material";
import { useApp } from "../AppContext";
import { WorkflowDialog } from "../ai/Workflows";
import { approveJob, cancelJob, Job, JobSchedule, listJobs, listSchedules, rejectJob, runSchedules, useJobFeed } from "./jobsApi";
import { JobFeed, JobStatusChip } from "./JobFeed";
import { JobFiles } from "./JobFile";

import { t, language, tx } from "../i18n";
const when = (iso: string) => new Date(iso).toLocaleString(language(), { month: "short", day: "numeric", hour: "2-digit", minute: "2-digit" });

/** Any job kind: live feed, its result, and approve / reject / cancel. Approving sends the proposal back as the decision. */
const JobDialog: React.FC<{ jobId: string; onClose: () => void }> = ({ jobId, onClose }) => {
  const { job, events, error } = useJobFeed(jobId);
  const [busy, setBusy] = useState(false);
  const [actionError, setActionError] = useState<string>();
  const act = async (fn: () => Promise<any>) => {
    setBusy(true);
    setActionError(undefined);
    try {
      await fn();
    } catch (e: any) {
      setActionError(e.message);
    } finally {
      setBusy(false);
    }
  };
  const status = job?.status;
  const working = status === "queued" || status === "running" || status === "approved";
  return (
    <Dialog open onClose={onClose} maxWidth="md" fullWidth>
      <DialogTitle sx={{ display: "flex", alignItems: "center", gap: 1 }}>
        {tx(job?.title) || t("Job")}{status && <Box sx={{ ml: "auto" }}><JobStatusChip status={status} /></Box>}
      </DialogTitle>
      <DialogContent dividers>
        {actionError && <Alert severity="error" sx={{ mb: 2 }}>{actionError}</Alert>}
        {events.some((e) => e.level === "file") && (
          <Box sx={{ mb: 2 }}>
            <Typography variant="subtitle2" sx={{ mb: 1 }}>{t("Evidence")}</Typography>
            <JobFiles jobId={jobId} events={events} height={220} />
          </Box>
        )}
        <Typography variant="subtitle2" sx={{ mb: 1 }}>{t("Steps")}</Typography>
        <JobFeed job={job} events={events} error={error} />
        {job?.result != null && (
          <Box component="pre" sx={{ mt: 2, p: 1.5, bgcolor: "action.hover", borderRadius: 1, fontSize: 12, overflow: "auto", maxHeight: 260 }}>
            {JSON.stringify(job.result, null, 2)}
          </Box>
        )}
      </DialogContent>
      <DialogActions>
        {working && <Button color="inherit" disabled={busy} onClick={() => act(() => cancelJob(jobId))}>{t("Cancel job")}</Button>}
        <Button onClick={onClose}>{t("Close")}</Button>
        {status === "awaiting_approval" && (
          <>
            <Button color="inherit" disabled={busy} onClick={() => act(() => rejectJob(jobId))}>{t("Reject")}</Button>
            <Button variant="contained" disabled={busy} onClick={() => act(() => approveJob(jobId, job?.result || {}))}>{t("Approve")}</Button>
          </>
        )}
      </DialogActions>
    </Dialog>
  );
};

/** Background jobs the user started (administrators: everyone's), refreshed every few seconds. */
export const JobsPage: React.FC = () => {
  const { ai, me } = useApp();
  const [jobs, setJobs] = useState<Job[]>([]);
  const [schedules, setSchedules] = useState<JobSchedule[]>([]);
  const [running, setRunning] = useState(false);
  const [error, setError] = useState<string>();
  const [open, setOpen] = useState<Job | null>(null);

  const load = useCallback(() => {
    listJobs().then((j) => { setJobs(j); setError(undefined); }).catch((e) => setError(e.message));
    if (me?.is_admin) listSchedules().then((s) => setSchedules(s.filter((x) => !x.id?.startsWith("__")))).catch(() => setSchedules([]));
  }, [me?.is_admin]);
  const runNow = () => {
    setRunning(true);
    runSchedules().then((s) => setSchedules(s.filter((x) => !x.id?.startsWith("__")))).catch((e) => setError(e.message)).finally(() => { setRunning(false); load(); });
  };
  useEffect(() => {
    load();
    const t = setInterval(load, 5000);
    return () => clearInterval(t);
  }, [load]);

  const wfOf = (j: Job) => (j.kind === "ai.workflow" ? ai?.workflows.find((w) => w.id === j.input?.workflow) : undefined);
  const waiting = jobs.filter((j) => j.status === "awaiting_approval").length;

  return (
    <Box>
      <Typography variant="h5" sx={{
        fontWeight: 700
      }}>{t("Background jobs")}</Typography>
      <Typography
        sx={{
          color: "text.secondary",
          mb: 2
        }}>
        {t("Work that runs on the server, such as AI workflows. Close the window while a job runs and come back here to review it.")}{waiting > 0 && <> <b>{t("{count} waiting for your approval.", { count: waiting })}</b></>}
      </Typography>
      {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
      <Paper variant="outlined">
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell sx={{ fontWeight: 600 }}>{t("Job")}</TableCell>
              <TableCell sx={{ fontWeight: 600 }}>{t("Status")}</TableCell>
              <TableCell sx={{ fontWeight: 600 }}>{t("Started by")}</TableCell>
              <TableCell sx={{ fontWeight: 600 }}>{t("Started")}</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {!jobs.length && (
              <TableRow><TableCell colSpan={4}><Typography
                sx={{
                  color: "text.secondary",
                  py: 2
                }}>{t("No jobs yet.")}</Typography></TableCell></TableRow>
            )}
            {jobs.map((j) => (
              <TableRow key={j.job_id} hover sx={{ cursor: "pointer" }} onClick={() => setOpen(j)}>
                <TableCell>{j.title || j.kind}{j.status === "failed" && j.error && <Typography variant="caption" color="error" sx={{
                  display: "block"
                }}>{j.error}</Typography>}</TableCell>
                <TableCell><JobStatusChip status={j.status} /></TableCell>
                <TableCell>{j.username}</TableCell>
                <TableCell sx={{ whiteSpace: "nowrap" }}>{when(j.createdAt)}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </Paper>
      {schedules.length > 0 && (
        <Paper variant="outlined" sx={{ mt: 3 }}>
          <Box sx={{ display: "flex", alignItems: "center", px: 2, pt: 1.5 }}>
            <Typography variant="subtitle1" sx={{ fontWeight: 600 }}>{t("Schedules")}</Typography>
            <Button size="small" sx={{ ml: "auto" }} disabled={running} onClick={runNow}>{t("Run what's due now")}</Button>
          </Box>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell sx={{ fontWeight: 600 }}>{t("Schedule")}</TableCell>
                <TableCell sx={{ fontWeight: 600 }}>{t("Next run")}</TableCell>
                <TableCell sx={{ fontWeight: 600 }}>{t("Last run")}</TableCell>
                <TableCell sx={{ fontWeight: 600 }}>{t("Last result")}</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {schedules.map((s) => (
                <TableRow key={s.id}>
                  <TableCell>{s.id}</TableCell>
                  <TableCell sx={{ whiteSpace: "nowrap" }}>{s.nextRun ? when(s.nextRun) : "-"}</TableCell>
                  <TableCell sx={{ whiteSpace: "nowrap" }}>{s.lastRun ? when(s.lastRun) : "-"}</TableCell>
                  <TableCell>
                    <Typography variant="body2" color={s.error || s.status === "failed" || s.status === "invalid" ? "error" : "text.secondary"}>
                      {s.error || s.lastMessage || s.status || "-"}
                    </Typography>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </Paper>
      )}
      {open && wfOf(open) && (
        <WorkflowDialog wf={wfOf(open)!} jobId={open.job_id} recordId={open.input?.recordId} onClose={() => { setOpen(null); load(); }} />
      )}
      {open && !wfOf(open) && <JobDialog jobId={open.job_id} onClose={() => { setOpen(null); load(); }} />}
    </Box>
  );
};
