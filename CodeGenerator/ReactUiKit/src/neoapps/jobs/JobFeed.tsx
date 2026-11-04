import React, { useEffect, useRef } from "react";
import { Alert, Box, Chip, LinearProgress, Typography } from "@mui/material";
import CheckCircle from "@mui/icons-material/CheckCircle";
import ErrorOutline from "@mui/icons-material/ErrorOutline";
import HourglassTop from "@mui/icons-material/HourglassTop";
import PanTool from "@mui/icons-material/PanTool";
import WarningAmber from "@mui/icons-material/WarningAmber";
import FiberManualRecord from "@mui/icons-material/FiberManualRecord";
import ImageOutlined from "@mui/icons-material/ImageOutlined";
import { JobFile } from "./JobFile";
import { FINISHED, Job, JobEvent, JobStatus } from "./jobsApi";

const STATUS: Record<JobStatus, { label: string; color: "default" | "primary" | "success" | "warning" | "error" | "info" }> = {
  queued: { label: "Queued", color: "default" },
  running: { label: "Running", color: "primary" },
  awaiting_approval: { label: "Needs approval", color: "warning" },
  approved: { label: "Approved", color: "info" },
  completed: { label: "Done", color: "success" },
  failed: { label: "Failed", color: "error" },
  cancelled: { label: "Cancelled", color: "default" },
  rejected: { label: "Rejected", color: "default" },
};

export const JobStatusChip: React.FC<{ status: JobStatus }> = ({ status }) => {
  const s = STATUS[status] || { label: status, color: "default" as const };
  return <Chip size="small" label={s.label} color={s.color} variant={status === "running" ? "filled" : "outlined"} />;
};

const icon = (e: JobEvent) => {
  const sx = { fontSize: 16, mt: "2px" };
  switch (e.level) {
    case "done": return <CheckCircle color="success" sx={sx} />;
    case "error": return <ErrorOutline color="error" sx={sx} />;
    case "warn": return <WarningAmber color="warning" sx={sx} />;
    case "approval": return <PanTool color="warning" sx={sx} />;
    case "file": return <ImageOutlined color="action" sx={sx} />;
    default: return <FiberManualRecord color="disabled" sx={{ ...sx, fontSize: 10, mt: "5px", mx: "3px" }} />;
  }
};

const time = (iso: string) => new Date(iso).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit", second: "2-digit" });

/** Live timeline of a job: a progress bar while it works and one line per step as it happens. */
export const JobFeed: React.FC<{ job?: Job; events: JobEvent[]; error?: string; maxHeight?: number }> = ({ job, events, error, maxHeight = 260 }) => {
  const end = useRef<HTMLDivElement>(null);
  useEffect(() => end.current?.scrollIntoView({ block: "nearest" }), [events.length]);
  const working = job && (job.status === "queued" || job.status === "running" || job.status === "approved");
  return (
    <Box>
      {error && <Alert severity="warning" sx={{ mb: 1 }}>{error}</Alert>}
      {working && (
        <Box sx={{ display: "flex", alignItems: "center", gap: 1, mb: 1 }}>
          <HourglassTop fontSize="small" color="primary" />
          <Box sx={{ flex: 1 }}>
            <LinearProgress variant={job?.progress ? "determinate" : "indeterminate"} value={job?.progress || 0} />
          </Box>
        </Box>
      )}
      <Box sx={{ maxHeight, overflowY: "auto", border: 1, borderColor: "divider", borderRadius: 1, p: 1, bgcolor: "action.hover" }}>
        {!events.length && <Typography variant="body2" color="text.secondary">Starting…</Typography>}
        {events.map((e) => (
          <Box key={e.id} sx={{ display: "flex", gap: 1, alignItems: "flex-start", py: 0.25 }}>
            {icon(e)}
            <Box sx={{ flex: 1, minWidth: 0 }}>
              <Typography variant="body2" sx={{ fontWeight: e.level === "approval" || e.level === "done" ? 600 : 400 }}>{e.message}</Typography>
              {e.level === "file" && e.data?.file && job && <JobFile jobId={job.job_id} info={e.data} caption={e.message} height={64} />}
            </Box>
            <Typography variant="caption" color="text.secondary" sx={{ whiteSpace: "nowrap" }}>{time(e.at)}</Typography>
          </Box>
        ))}
        <div ref={end} />
      </Box>
      {job && FINISHED.includes(job.status) && job.status === "failed" && job.error && <Alert severity="error" sx={{ mt: 1 }}>{job.error}</Alert>}
    </Box>
  );
};
