import React, { useEffect, useState } from "react";
import { Box, Button, Dialog, Link, Typography } from "@mui/material";
import { jobFileUrl } from "./jobsApi";

export interface JobFileInfo {
  file: string;
  type?: string;
  size?: number;
}

/** A job's attached file: images and videos inline (click an image to enlarge), anything else as a download link. */
export const JobFile: React.FC<{ jobId: string; info: JobFileInfo; caption?: string; height?: number }> = ({ jobId, info, caption, height = 160 }) => {
  const [url, setUrl] = useState<string>();
  const [error, setError] = useState<string>();
  const [zoom, setZoom] = useState(false);
  const kind = info.type?.startsWith("image/") ? "image" : info.type?.startsWith("video/") ? "video" : "other";

  useEffect(() => {
    if (kind === "other") return;
    let made: string | undefined;
    let live = true;
    jobFileUrl(jobId, info.file).then((u) => { made = u; if (live) setUrl(u); else URL.revokeObjectURL(u); }).catch((e) => setError(e.message));
    return () => { live = false; if (made) URL.revokeObjectURL(made); };
  }, [jobId, info.file, kind]);

  const download = async () => {
    try {
      const u = await jobFileUrl(jobId, info.file);
      const a = document.createElement("a");
      a.href = u;
      a.download = info.file;
      a.click();
      setTimeout(() => URL.revokeObjectURL(u), 60_000);
    } catch (e: any) {
      setError(e.message);
    }
  };

  if (error) return <Typography variant="caption" color="error">{error}</Typography>;
  if (kind === "other") return <Link component="button" variant="body2" onClick={download}>{info.file}{info.size ? ` (${Math.ceil(info.size / 1024)} KB)` : ""}</Link>;
  if (!url) return <Box sx={{ height, width: height * 1.6, bgcolor: "action.selected", borderRadius: 1 }} />;
  return (
    <Box sx={{ mt: 0.5 }}>
      {kind === "image" ? (
        <Box component="img" src={url} alt={caption || info.file} onClick={() => setZoom(true)}
          sx={{ maxHeight: height, maxWidth: "100%", borderRadius: 1, border: 1, borderColor: "divider", cursor: "zoom-in", display: "block" }} />
      ) : (
        <Box component="video" src={url} controls sx={{ maxHeight: height * 1.5, maxWidth: "100%", borderRadius: 1, border: 1, borderColor: "divider", display: "block" }} />
      )}
      {zoom && (
        <Dialog open onClose={() => setZoom(false)} maxWidth="xl">
          <Box component="img" src={url} alt={caption || info.file} sx={{ display: "block", maxWidth: "90vw", maxHeight: "85vh" }} />
          <Box sx={{ display: "flex", justifyContent: "space-between", alignItems: "center", px: 2, py: 1 }}>
            <Typography variant="body2" sx={{
              color: "text.secondary"
            }}>{caption || info.file}</Typography>
            <Button onClick={download}>Download</Button>
          </Box>
        </Dialog>
      )}
    </Box>
  );
};

/** Every file a job attached, newest last: a gallery for custom screens (e.g. a test run's evidence). */
export const JobFiles: React.FC<{ jobId: string; events: { level: string; message: string; data?: any }[]; height?: number }> = ({ jobId, events, height }) => (
  <Box sx={{ display: "flex", flexWrap: "wrap", gap: 2 }}>
    {events.filter((e) => e.level === "file" && e.data?.file).map((e) => (
      <Box key={e.data.file} sx={{ maxWidth: 420 }}>
        <JobFile jobId={jobId} info={e.data} caption={e.message} height={height} />
        <Typography variant="caption" sx={{
          color: "text.secondary"
        }}>{e.message}</Typography>
      </Box>
    ))}
  </Box>
);
