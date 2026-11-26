import React, { useCallback, useEffect, useState } from "react";
import {
  Alert, Box, Button, Chip, CircularProgress, Dialog, DialogActions, DialogContent, DialogTitle, Divider, FormControlLabel,
  IconButton, MenuItem, Stack, Switch, TextField, Tooltip, Typography,
} from "@mui/material";
import ContentCopy from "@mui/icons-material/ContentCopy";
import DeleteOutline from "@mui/icons-material/DeleteOutlineOutlined";
import Refresh from "@mui/icons-material/Refresh";
import { api } from "../api";

import { t, language, tx } from "../i18n";
interface Invite {
  id: number; role_id: number; role_name: string; tenant_id?: number | null; label?: string; max_uses: number; uses: number;
  expires_at?: string; is_open: boolean; created_by: string; url: string; email?: string;
}
interface Role { role_id: number; role_name: string; is_admin: boolean }

const fullUrl = (u: string) => window.location.origin + u;

/** Invite links: pick a role, share the link; people choose their own username and password and join with that role. */
export const InviteLinks: React.FC<{
  open: boolean; onClose: () => void; roles: Role[]; pickTenant: boolean; tenants: { id: number; label: string }[];
}> = ({ open, onClose, roles, pickTenant, tenants }) => {
  const choices = roles.filter((r) => !r.is_admin && r.role_name.toLowerCase() !== "public"); // visitors' role, not for accounts
  const [items, setItems] = useState<Invite[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string>();
  const [busy, setBusy] = useState(false);
  const [roleId, setRoleId] = useState(0);
  const [tenantId, setTenantId] = useState(0);
  const [label, setLabel] = useState("");
  const [people, setPeople] = useState("1");
  const [expires, setExpires] = useState("");
  const [copied, setCopied] = useState<number>();
  const [emails, setEmails] = useState("");
  const [sentNote, setSentNote] = useState<string>();

  const load = useCallback(async () => {
    setLoading(true);
    try { setItems((await api<any>("GET", "Access/invites")).document || []); }
    catch (e: any) { setError(e.message); } finally { setLoading(false); }
  }, []);

  useEffect(() => {
    if (!open) return;
    setError(undefined); setLabel(""); setPeople("1"); setEmails(""); setSentNote(undefined);
    // a week by default, like most invite links
    setExpires(new Date(Date.now() + 7 * 864e5).toISOString().slice(0, 10));
    setRoleId(choices[0]?.role_id || 0); setTenantId(tenants[0]?.id || 0);
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, load]);

  const copy = (i: Invite) =>
    navigator.clipboard?.writeText(fullUrl(i.url)).then(() => { setCopied(i.id); setTimeout(() => setCopied(undefined), 2000); }).catch(() => {});

  const create = async () => {
    setBusy(true); setError(undefined); setSentNote(undefined);
    const list = emails.split(/[\s,;]+/).map((x) => x.trim()).filter(Boolean);
    try {
      if (list.length) {
        const body: any = { role_id: roleId, emails: list, label: label.trim() || undefined };
        if (pickTenant) body.tenant_id = tenantId;
        if (expires) body.expires_at = new Date(expires + "T23:59:59").toISOString();
        const res = await api<any>("POST", "Access/invites", body);
        const rows: any[] = res.document || [];
        const made = rows.filter((r) => r.invite).map((r) => r.invite);
        setItems((all) => [...made, ...all]);
        const sent = rows.filter((r) => r.sent).map((r) => r.email);
        const failed = rows.filter((r) => r.invite && !r.sent).map((r) => r.email);
        const already = rows.filter((r) => r.already).map((r) => r.email);
        setSentNote([
          sent.length ? t("Invitation emailed to {emails}.", { emails: sent.join(", ") }) : "",
          failed.length ? t("Couldn't email {emails}: copy their link below and send it yourself.", { emails: failed.join(", ") }) : "",
          already.length ? t("Already a member: {emails}.", { emails: already.join(", ") }) : "",
        ].filter(Boolean).join(" "));
        setEmails("");
        return;
      }
      const body: any = { role_id: roleId, label: label.trim() || undefined, max_uses: Number(people) || 1 };
      if (pickTenant) body.tenant_id = tenantId;
      if (expires) body.expires_at = new Date(expires + "T23:59:59").toISOString();
      const res = await api<any>("POST", "Access/invites", body);
      setItems((all) => [res.document, ...all]);
      copy(res.document);
    } catch (e: any) { setError(e.message); } finally { setBusy(false); }
  };

  const change = async (i: Invite, method: string, body?: any) => {
    setError(undefined);
    try {
      const res = await api<any>(method, `Access/invites/${i.id}`, body);
      if (method === "DELETE") setItems((all) => all.filter((x) => x.id !== i.id));
      else setItems((all) => all.map((x) => (x.id === i.id ? res.document : x)));
    } catch (e: any) { setError(e.message); }
  };

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>{t("Invite people")}</DialogTitle>
      <DialogContent dividers>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
          {t("Type email addresses and each person gets their own link by email: they click Join and they're in, no password (next time they sign in with a code sent to that email). Or leave the emails empty for one link you send yourself: people who open it choose a username and password.")}</Typography>
        {choices.length === 0 ? <Alert severity="info">{t("Create a role first (Roles). Administrators are added directly in Members.")}</Alert> : (
          <Stack spacing={2}>
            <TextField select size="small" label={t("Role")} value={roleId} onChange={(e) => setRoleId(Number(e.target.value))}>
              {choices.map((r) => <MenuItem key={r.role_id} value={r.role_id}>{tx(r.role_name)}</MenuItem>)}
            </TextField>
            {pickTenant && (
              <TextField select size="small" label={t("Tenant")} value={tenantId} onChange={(e) => setTenantId(Number(e.target.value))}>
                <MenuItem value={0}>{t("Platform")}</MenuItem>
                {tenants.map((t) => <MenuItem key={t.id} value={t.id}>{t.label}</MenuItem>)}
              </TextField>
            )}
            <TextField size="small" label={t("Email addresses (we email each person their own link)")} multiline minRows={2} value={emails}
              onChange={(e) => setEmails(e.target.value)} placeholder={"priya@example.com, sam@example.com"}
              helperText={t("Leave empty to make one link you copy and send yourself.")} />
            <TextField size="small" label={t("Note (who it's for, optional)")} value={label} onChange={(e) => setLabel(e.target.value)} />
            <Box sx={{ display: "flex", gap: 2 }}>
              {!emails.trim() && <TextField size="small" type="number" label={t("How many people can use it")} value={people} onChange={(e) => setPeople(e.target.value)} sx={{ flex: 1 }}
                slotProps={{ htmlInput: { min: 1, max: 1000 } }} />}
              <TextField size="small" type="date" label={t("Stops working after")} value={expires} onChange={(e) => setExpires(e.target.value)} sx={{ flex: 1 }}
                slotProps={{ inputLabel: { shrink: true } }} />
            </Box>
            <Box><Button variant="contained" onClick={create} disabled={busy || !roleId}>{busy ? <CircularProgress size={20} color="inherit" /> : emails.trim() ? t("Send invitations") : t("Create invite link")}</Button></Box>
            {sentNote && <Alert severity="success">{sentNote}</Alert>}
          </Stack>
        )}
        {error && <Alert severity="error" sx={{ mt: 2 }}>{error}</Alert>}
        <Divider sx={{ my: 2.5 }} />
        <Typography variant="subtitle2" sx={{ mb: 1 }}>{t("Invite links")}</Typography>
        {loading && <CircularProgress size={20} />}
        {!loading && items.length === 0 && <Typography variant="body2" color="text.secondary">{t("No invite links yet.")}</Typography>}
        <Stack spacing={1.5}>
          {items.map((i) => (
            <Box key={i.id} sx={{ border: 1, borderColor: "divider", borderRadius: 1.5, p: 1.5 }}>
              <Box sx={{ display: "flex", alignItems: "center", gap: 1, flexWrap: "wrap", mb: 1 }}>
                <Typography variant="body2" sx={{ fontWeight: 600, flex: 1 }}>{i.email || i.label || t("Joins as {role}", { role: i.role_name })}</Typography>
                <Chip size="small" label={tx(i.role_name)} />
                <FormControlLabel sx={{ m: 0 }} label={i.is_open ? t("On") : t("Off")} control={
                  <Switch size="small" checked={i.is_open} onChange={(e) => change(i, "PUT", { is_open: e.target.checked })} />} />
              </Box>
              <Box sx={{ display: "flex", gap: 1, alignItems: "center" }}>
                <TextField size="small" fullWidth value={fullUrl(i.url)} slotProps={{ input: { readOnly: true } }} onFocus={(e) => e.target.select()} />
                <Tooltip title={copied === i.id ? t("Copied") : t("Copy link")}><IconButton onClick={() => copy(i)}><ContentCopy fontSize="small" /></IconButton></Tooltip>
                <Tooltip title={t("New link (the old one stops working)")}><IconButton onClick={() => change(i, "PUT", { regenerate: true })}><Refresh fontSize="small" /></IconButton></Tooltip>
                <Tooltip title={t("Delete link")}><IconButton onClick={() => change(i, "DELETE")}><DeleteOutline fontSize="small" /></IconButton></Tooltip>
              </Box>
              <Typography variant="caption" color="text.secondary">
                {t("Used {n} of {total}", { n: i.uses, total: i.max_uses })}{i.expires_at ? " · " + t("until {date}", { date: new Date(i.expires_at).toLocaleDateString(language()) }) : ""}{" · "}{t("by {name}", { name: i.created_by })}
              </Typography>
            </Box>
          ))}
        </Stack>
      </DialogContent>
      <DialogActions><Button onClick={onClose}>{t("Done")}</Button></DialogActions>
    </Dialog>
  );
};
