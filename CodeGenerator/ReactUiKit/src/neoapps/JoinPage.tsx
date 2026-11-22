import React, { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { Alert, Box, Button, CircularProgress, Paper, TextField, Typography } from "@mui/material";
import { apiFetch, login } from "./api";
import { useApp } from "./AppContext";

async function call(method: string, token: string, body?: any) {
  const res = await apiFetch(`public/invite/${encodeURIComponent(token)}`, {
    method, headers: body ? { "Content-Type": "application/json" } : undefined, body: body ? JSON.stringify(body) : undefined,
    cache: "no-store", // always the current state (closed, full, used), never a copy the browser kept
  });
  let data: any = null;
  try { data = await res.json(); } catch { /* no body */ }
  if (!res.ok) throw Object.assign(new Error(data?.message || (res.status === 429 ? "Too many tries. Please wait a minute." : "Something went wrong. Please try again.")), { status: res.status });
  return data;
}

/** /join/:token — an invite link: the person picks a username and password and joins with the invite's role. */
export const JoinPage: React.FC = () => {
  const { token = "" } = useParams();
  const navigate = useNavigate();
  const { config, refreshSession } = useApp();
  const [invite, setInvite] = useState<{ role: string; label?: string; invitedBy: string; email?: string }>();
  const [problem, setProblem] = useState<string>();
  const [error, setError] = useState<string>();
  const [busy, setBusy] = useState(false);
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [again, setAgain] = useState("");
  const name = (config as any)?.appName || "the app";

  useEffect(() => { call("GET", token).then(setInvite).catch((e) => setProblem(e.message)); }, [token]);

  const join = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(undefined);
    if (password !== again) { setError("The two passwords don't match."); return; }
    setBusy(true);
    try {
      await call("POST", token, { username: username.trim(), password });
      await login(username.trim(), password);
      await refreshSession();
      navigate("/", { replace: true });
    } catch (err: any) { setError(err.message); } finally { setBusy(false); }
  };

  const joinByEmail = async () => {
    setBusy(true); setError(undefined);
    try { await call("POST", token, {}); await refreshSession(); navigate("/", { replace: true }); }
    catch (err: any) { setError(err.message); } finally { setBusy(false); }
  };

  return (
    <Box sx={{ minHeight: "100vh", bgcolor: "background.default", display: "grid", placeItems: "center", p: 2 }}>
      <Paper variant="outlined" sx={{ width: "100%", maxWidth: 420, p: 4, borderRadius: 3 }}>
        <Typography variant="h5" sx={{ fontWeight: 700, mb: 1 }}>Join {name}</Typography>
        {problem ? <Alert severity="info">{problem}</Alert> : !invite ? <CircularProgress /> : invite.email ? (
          <>
            <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
              {invite.invitedBy} invited <b>{invite.email}</b> as <b>{invite.role}</b>. No password needed: next time, sign in with a code sent to this email.
            </Typography>
            {error && <Alert severity="error" sx={{ mb: 1.5 }}>{error}</Alert>}
            <Button variant="contained" fullWidth onClick={joinByEmail} disabled={busy}>
              {busy ? <CircularProgress size={20} color="inherit" /> : `Join ${name}`}
            </Button>
            {error && /already a member/i.test(error) && <Button fullWidth sx={{ mt: 1 }} onClick={() => navigate("/", { replace: true })}>Go to sign in</Button>}
          </>
        ) : (
          <form onSubmit={join}>
            <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
              {invite.invitedBy} invited you as <b>{invite.role}</b>. Choose a username and password to create your account.
            </Typography>
            <TextField label="Username" fullWidth margin="dense" autoComplete="username" value={username} onChange={(e) => setUsername(e.target.value)} autoFocus />
            <TextField label="Password" type="password" fullWidth margin="dense" autoComplete="new-password" value={password} onChange={(e) => setPassword(e.target.value)}
              helperText="At least 10 characters, with letters and digits." />
            <TextField label="Password again" type="password" fullWidth margin="dense" autoComplete="new-password" value={again} onChange={(e) => setAgain(e.target.value)} />
            {error && <Alert severity="error" sx={{ mt: 1.5 }}>{error}</Alert>}
            <Button type="submit" variant="contained" fullWidth sx={{ mt: 2 }} disabled={busy || !username.trim() || !password}>
              {busy ? <CircularProgress size={20} color="inherit" /> : "Create account"}
            </Button>
          </form>
        )}
      </Paper>
    </Box>
  );
};
