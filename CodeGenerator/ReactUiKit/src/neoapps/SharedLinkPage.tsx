import React, { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import { Alert, Box, Button, CircularProgress, Paper, Typography } from "@mui/material";
import CheckCircleOutline from "@mui/icons-material/CheckCircleOutlineOutlined";
import { apiFetch } from "./api";
import { useApp } from "./AppContext";
import { RecordForm } from "./RecordForm";
import { Login } from "./Login";

interface Shared { title?: string; table: string; kind: "form" | "record"; fields: string[]; values?: Record<string, any>; access: string }

async function call(method: string, token: string, body?: any) {
  const res = await apiFetch(`public/share/${encodeURIComponent(token)}`, {
    method, headers: body ? { "Content-Type": "application/json" } : undefined, body: body ? JSON.stringify(body) : undefined,
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
      {/* bots fill in every box; people never see this one */}
      <input name="website" value={honeypot} onChange={(e) => setHoneypot(e.target.value)} tabIndex={-1} autoComplete="off"
        style={{ position: "absolute", left: "-10000px", width: 1, height: 1, opacity: 0 }} aria-hidden="true" />
      <RecordForm
        key={round} inline open table={table} record={shared.kind === "record" ? shared.values || {} : null} fieldNames={shared.fields}
        submitLabel={shared.kind === "record" ? "Save" : "Send"} onClose={() => {}} onSaved={() => {}}
        onSubmit={async (values) => {
          const res = await call("POST", token, { ...values, website: honeypot });
          setDone(res?.message || "Thank you.");
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
