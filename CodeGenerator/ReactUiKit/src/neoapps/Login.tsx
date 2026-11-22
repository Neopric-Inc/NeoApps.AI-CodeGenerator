import React, { useEffect, useState } from "react";
import { Alert, Box, Button, CircularProgress, Paper, TextField, Typography } from "@mui/material";
import LockOutlined from "@mui/icons-material/LockOutlined";
import { apiFetch, login } from "./api";

async function publicCall(path: string, body?: any) {
  const res = await apiFetch(path, { method: body ? "POST" : "GET", headers: body ? { "Content-Type": "application/json" } : undefined, body: body ? JSON.stringify(body) : undefined });
  let data: any = null;
  try { data = await res.json(); } catch { /* no body */ }
  if (!res.ok) throw new Error(data?.message || (res.status === 429 ? "Too many tries. Please wait a minute." : "Something went wrong. Please try again."));
  return data;
}
import { useApp } from "./AppContext";

export const Login: React.FC = () => {
  const { config, schema, refreshSession } = useApp();
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string>();
  const appName = config.appName || schema.app || "App";
  // sign in with an emailed code (when the app can send email)
  const [codeLogin, setCodeLogin] = useState(false);
  const [mode, setMode] = useState<"password" | "email" | "code">("password");
  const [email, setEmail] = useState("");
  const [code, setCode] = useState("");
  const [info, setInfo] = useState<string>();
  useEffect(() => { publicCall("public/email-code/available").then((d) => setCodeLogin(!!d?.enabled)).catch(() => setCodeLogin(false)); }, []);

  const sendCode = async (e: React.FormEvent) => {
    e.preventDefault();
    setBusy(true); setError(undefined);
    try { const d = await publicCall("public/email-code", { email: email.trim() }); setInfo(d?.message); setMode("code"); }
    catch (err: any) { setError(err.message); } finally { setBusy(false); }
  };
  const verify = async (e: React.FormEvent) => {
    e.preventDefault();
    setBusy(true); setError(undefined);
    try { await publicCall("public/email-code/verify", { email: email.trim(), code: code.trim() }); await refreshSession(); }
    catch (err: any) { setError(err.message); } finally { setBusy(false); }
  };

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    setBusy(true);
    setError(undefined);
    try {
      await login(username.trim(), password);
      await refreshSession();
    } catch (err: any) {
      setError(err.status === 401 ? "Invalid username or password." : err.message);
    } finally {
      setBusy(false);
    }
  };

  return (
    <Box sx={{ minHeight: "100vh", display: "grid", placeItems: "center", bgcolor: "grey.100", p: 2 }}>
      <Paper elevation={0} sx={{ p: 4, width: "100%", maxWidth: 380, border: 1, borderColor: "divider", borderRadius: 3 }}>
        <Box sx={{ display: "flex", alignItems: "center", gap: 1.5, mb: 3 }}>
          <Box sx={{ width: 40, height: 40, borderRadius: 2, bgcolor: "primary.main", color: "primary.contrastText", display: "grid", placeItems: "center" }}>
            <LockOutlined fontSize="small" />
          </Box>
          <Box>
            <Typography
              variant="h6"
              sx={{
                fontWeight: 700,
                lineHeight: 1.2
              }}>{appName}</Typography>
            <Typography variant="body2" sx={{
              color: "text.secondary"
            }}>{config.tagline || "Sign in to continue"}</Typography>
          </Box>
        </Box>
        {mode === "email" && (
          <form onSubmit={sendCode}>
            <TextField label="Email" name="email" type="email" fullWidth margin="dense" autoFocus value={email} onChange={(e) => setEmail(e.target.value)} />
            {error && <Alert severity="error" sx={{ mt: 1.5 }}>{error}</Alert>}
            <Button type="submit" variant="contained" fullWidth size="large" sx={{ mt: 2.5 }} disabled={busy || !email.trim()}>
              {busy ? <CircularProgress size={22} color="inherit" /> : "Email me a code"}
            </Button>
            <Button fullWidth sx={{ mt: 1 }} onClick={() => { setMode("password"); setError(undefined); }}>Use username and password</Button>
          </form>
        )}
        {mode === "code" && (
          <form onSubmit={verify}>
            {info && <Alert severity="info" sx={{ mb: 1.5 }}>{info}</Alert>}
            <TextField label="6-digit code" name="code" fullWidth margin="dense" autoFocus value={code} onChange={(e) => setCode(e.target.value)}
              slotProps={{ htmlInput: { inputMode: "numeric", autoComplete: "one-time-code", maxLength: 7 } }} />
            {error && <Alert severity="error" sx={{ mt: 1.5 }}>{error}</Alert>}
            <Button type="submit" variant="contained" fullWidth size="large" sx={{ mt: 2.5 }} disabled={busy || code.trim().length < 6}>
              {busy ? <CircularProgress size={22} color="inherit" /> : "Sign in"}
            </Button>
            <Button fullWidth sx={{ mt: 1 }} onClick={() => { setMode("email"); setCode(""); setError(undefined); }}>Send a new code</Button>
          </form>
        )}
        {mode === "password" && <form onSubmit={submit}>
          <TextField label="Username" name="username" fullWidth margin="dense" autoFocus value={username} onChange={(e) => setUsername(e.target.value)} />
          <TextField label="Password" name="password" type="password" fullWidth margin="dense" value={password} onChange={(e) => setPassword(e.target.value)} />
          {error && <Alert severity="error" sx={{ mt: 1.5 }}>{error}</Alert>}
          <Button type="submit" variant="contained" fullWidth size="large" sx={{ mt: 2.5 }} disabled={busy || !username || !password}>
            {busy ? <CircularProgress size={22} color="inherit" /> : "Sign in"}
          </Button>
          {codeLogin && <Button fullWidth sx={{ mt: 1 }} onClick={() => { setMode("email"); setError(undefined); }}>Sign in with an email code instead</Button>}
        </form>}
      </Paper>
    </Box>
  );
};
