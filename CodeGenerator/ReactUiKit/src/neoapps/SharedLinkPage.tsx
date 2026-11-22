import React, { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import { Alert, Box, Button, CircularProgress, Paper, Typography } from "@mui/material";
import CheckCircleOutline from "@mui/icons-material/CheckCircleOutlineOutlined";
import { apiBase, apiFetch } from "./api";
import { renderValue } from "./format";
import { useApp } from "./AppContext";
import { RecordForm } from "./RecordForm";
import { Login } from "./Login";

interface Shared {
  title?: string; table: string; kind: "form" | "record"; fields: string[]; values?: Record<string, any>; access: string; allowFiles?: boolean;
  context?: { table: string; fields: string[]; values: Record<string, any>; files: { id: number; name: string; size: number }[] }[];
}

async function call(method: string, token: string, body?: any) {
  const res = await apiFetch(`public/share/${encodeURIComponent(token)}`, {
    method, headers: body ? { "Content-Type": "application/json" } : undefined, body: body ? JSON.stringify(body) : undefined,
    cache: "no-store", // always the current state (closed, full, used), never a copy the browser kept
  });
  let data: any = null;
  try { data = await res.json(); } catch { /* no body */ }
  if (!res.ok) throw Object.assign(new Error(data?.message || (res.status === 429 ? "Too many tries. Please wait a minute." : "Something went wrong. Please try again.")), { status: res.status });
  return data;
}

/** /f/:token — a shared form or record link, opened without signing in (or as a member for "members only" links). */
export const SharedLinkPage: React.FC = () => {
  const { token = "" } = useParams();
  const { tables, config, me } = useApp();
  const [shared, setShared] = useState<Shared>();
  const [error, setError] = useState<{ message: string; status?: number }>();
  const [done, setDone] = useState<string>();
  const [round, setRound] = useState(0);
  const [honeypot, setHoneypot] = useState("");
  const [files, setFiles] = useState<File[]>([]);
  const [fileError, setFileError] = useState<string>();
  const okTypes = /\.(pdf|docx?|txt|rtf|odt|png|jpe?g)$/i;
  const pickFiles = (list: FileList | null) => {
    setFileError(undefined);
    const chosen = Array.from(list || []);
    const bad = chosen.find((f) => !okTypes.test(f.name) || f.size > 10 * 1024 * 1024);
    if (bad) { setFileError(`${bad.name}: attach a PDF, Word, text or image file up to 10 MB.`); return; }
    setFiles((cur) => [...cur, ...chosen].slice(0, 3));
  };

  useEffect(() => {
    setError(undefined);
    call("GET", token).then(setShared).catch((e) => setError({ message: e.message, status: e.status }));
  }, [token, round, me]);

  const table = shared ? tables[shared.table] : undefined;
  const name = (config as any)?.appName;

  let body: React.ReactNode;
  if (error) body = (
    <>
      <Alert severity={error.status === 410 ? "info" : "warning"}>{error.message}</Alert>
      {error.status === 401 && !me && <Box sx={{ mt: 2 }}><Login /></Box>}
    </>
  );
  else if (done) body = (
    <Box sx={{ textAlign: "center", py: 3 }}>
      <CheckCircleOutline color="success" sx={{ fontSize: 48 }} />
      <Typography sx={{ mt: 1, mb: 2 }}>{done}</Typography>
      {shared?.kind === "form" && <Button onClick={() => { setDone(undefined); setRound((r) => r + 1); }}>Send another response</Button>}
    </Box>
  );
  else if (!shared || !table) body = <Box sx={{ display: "grid", placeItems: "center", py: 4 }}><CircularProgress /></Box>;
  else body = (
    <>
      {(shared.context || []).map((ctx, i) => {
        const t = tables[ctx.table];
        return (
          <Box key={i} sx={{ mb: 3, pb: 3, borderBottom: 1, borderColor: "divider" }}>
            {ctx.fields.map((n) => {
              const f = t?.fields[n];
              const v = ctx.values[n];
              if (v === null || v === undefined || v === "") return null;
              const long = f?.widget === "textarea" || String(v).length > 120;
              return (
                <Box key={n} sx={{ mb: 1.5 }}>
                  <Typography variant="caption" color="text.secondary">{f?.label || n}</Typography>
                  {/* the whole text: the list formatter shortens long text */}
                  <Typography variant="body1" sx={{ whiteSpace: long ? "pre-wrap" : undefined }}>
                    {f && !long && !["textarea", "text", "email"].includes(f.widget) ? renderValue(f, v) : String(v)}
                  </Typography>
                </Box>
              );
            })}
            {ctx.files.map((file) => (
              <Button key={file.id} size="small" variant="outlined" sx={{ mr: 1, mt: 0.5 }}
                href={`${apiBase()}public/share/${encodeURIComponent(token)}/file/${file.id}`} target="_blank" rel="noopener">
                {file.name}
              </Button>
            ))}
          </Box>
        );
      })}
      {/* bots fill in every box; people never see this one */}
      <input name="website" value={honeypot} onChange={(e) => setHoneypot(e.target.value)} tabIndex={-1} autoComplete="off"
        style={{ position: "absolute", left: "-10000px", width: 1, height: 1, opacity: 0 }} aria-hidden="true" />
      <RecordForm
        key={round} inline open table={table} record={shared.kind === "record" ? shared.values || {} : null} fieldNames={shared.fields}
        submitLabel={shared.kind === "record" ? "Save" : "Send"} onClose={() => {}} onSaved={() => {}}
        extra={shared.allowFiles ? (
          <Box sx={{ mt: 2.5 }}>
            <Typography variant="subtitle2" sx={{ mb: 0.5 }}>Files</Typography>
            <Button variant="outlined" component="label" size="small" disabled={files.length >= 3}>
              Attach a file
              <input hidden type="file" multiple accept=".pdf,.doc,.docx,.txt,.rtf,.odt,.png,.jpg,.jpeg" onChange={(e) => { pickFiles(e.target.files); e.target.value = ""; }} />
            </Button>
            <Typography variant="caption" color="text.secondary" sx={{ ml: 1 }}>PDF, Word, text or images, up to 10 MB, 3 files</Typography>
            {files.map((f, i) => (
              <Box key={i} sx={{ display: "flex", alignItems: "center", gap: 1, mt: 0.5 }}>
                <Typography variant="body2" sx={{ flex: 1 }}>{f.name}</Typography>
                <Button size="small" onClick={() => setFiles((cur) => cur.filter((_, j) => j !== i))}>Remove</Button>
              </Box>
            ))}
            {fileError && <Alert severity="warning" sx={{ mt: 1 }}>{fileError}</Alert>}
          </Box>
        ) : undefined}
        onSubmit={async (values) => {
          const res = await call("POST", token, { ...values, website: honeypot });
          const failed: string[] = [];
          for (const f of files) {
            if (!res?.uploadKey) break;
            const body = new FormData();
            body.append("file", f);
            body.append("key", res.uploadKey);
            const up = await apiFetch(`public/share/${encodeURIComponent(token)}/files`, { method: "POST", body }).catch(() => null);
            if (!up || !up.ok) failed.push(f.name);
          }
          setFiles([]);
          setDone((res?.message || "Thank you.") + (failed.length ? ` But ${failed.join(", ")} couldn't be attached; please send it to the person who shared this form.` : ""));
        }}
      />
    </>
  );

  return (
    <Box sx={{ minHeight: "100vh", bgcolor: "background.default", py: { xs: 2, sm: 6 }, px: 2 }}>
      <Paper variant="outlined" sx={{ maxWidth: 640, mx: "auto", p: { xs: 2.5, sm: 4 }, borderRadius: 3 }}>
        {name && <Typography variant="overline" color="text.secondary">{name}</Typography>}
        <Typography variant="h5" sx={{ fontWeight: 700, mb: 3 }}>{shared?.title || table?.singular || "Form"}</Typography>
        {body}
      </Paper>
    </Box>
  );
};
