import React, { useEffect, useRef, useState } from "react";
import { Alert, Button, Dialog, DialogActions, DialogContent, DialogTitle, TextField, Typography } from "@mui/material";
import { setSignatureHandler } from "./api";
import { useApp } from "./AppContext";

/**
 * Electronic signature prompt for lifecycle steps with "signature" in ui.config.json: shows what the person is
 * signing and asks for their password. The server checks it, records signer, time and statement, and never stores it.
 */
export const SignatureDialog: React.FC = () => {
  const { me } = useApp();
  const [meaning, setMeaning] = useState<string | null>(null);
  const [password, setPassword] = useState("");
  const resolver = useRef<((v: string | null) => void) | null>(null);

  useEffect(() => {
    setSignatureHandler((m) => new Promise((resolve) => {
      resolver.current = resolve;
      setPassword("");
      setMeaning(m || "I approve this step.");
    }));
    return () => setSignatureHandler(null);
  }, []);

  const close = (value: string | null) => {
    resolver.current?.(value);
    resolver.current = null;
    setMeaning(null);
    setPassword("");
  };

  return (
    <Dialog open={meaning !== null} onClose={() => close(null)} maxWidth="xs" fullWidth>
      <form onSubmit={(e) => { e.preventDefault(); if (password) close(password); }}>
        <DialogTitle>Sign this step</DialogTitle>
        <DialogContent>
          <Alert severity="info" icon={false} sx={{ mb: 2 }}>
            <Typography variant="body2" sx={{ fontWeight: 600 }}>{meaning}</Typography>
          </Alert>
          <Typography variant="body2" sx={{ color: "text.secondary", mb: 1.5 }}>
            Signing as <b>{me?.username || "you"}</b>. Your name, the time and this statement are recorded.
          </Typography>
          <TextField autoFocus fullWidth type="password" label="Your password" autoComplete="current-password"
            value={password} onChange={(e) => setPassword(e.target.value)} />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => close(null)}>Cancel</Button>
          <Button type="submit" variant="contained" disabled={!password}>Sign</Button>
        </DialogActions>
      </form>
    </Dialog>
  );
};
