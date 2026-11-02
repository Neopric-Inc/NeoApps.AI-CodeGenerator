import React, { useState } from "react";
import { Alert, Box, Button, CircularProgress, Paper, TextField, Typography } from "@mui/material";
import LockOutlined from "@mui/icons-material/LockOutlined";
import { login } from "./api";
import { useApp } from "./AppContext";

export const Login: React.FC = () => {
  const { config, schema, refreshSession } = useApp();
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string>();
  const appName = config.appName || schema.app || "App";

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
            <Typography variant="h6" fontWeight={700} lineHeight={1.2}>{appName}</Typography>
            <Typography variant="body2" color="text.secondary">{config.tagline || "Sign in to continue"}</Typography>
          </Box>
        </Box>
        <form onSubmit={submit}>
          <TextField label="Username" name="username" fullWidth margin="dense" autoFocus value={username} onChange={(e) => setUsername(e.target.value)} />
          <TextField label="Password" name="password" type="password" fullWidth margin="dense" value={password} onChange={(e) => setPassword(e.target.value)} />
          {error && <Alert severity="error" sx={{ mt: 1.5 }}>{error}</Alert>}
          <Button type="submit" variant="contained" fullWidth size="large" sx={{ mt: 2.5 }} disabled={busy || !username || !password}>
            {busy ? <CircularProgress size={22} color="inherit" /> : "Sign in"}
          </Button>
        </form>
      </Paper>
    </Box>
  );
};
