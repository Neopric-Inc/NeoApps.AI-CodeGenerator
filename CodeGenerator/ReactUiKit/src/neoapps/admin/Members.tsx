import React, { useCallback, useEffect, useState } from "react";
import {
  Alert, Box, Button, Chip, Dialog, DialogActions, DialogContent, DialogTitle, MenuItem, Paper, Switch, Table, TableBody,
  TableCell, TableHead, TableRow, TextField, Typography,
} from "@mui/material";
import PersonAddOutlined from "@mui/icons-material/PersonAddOutlined";
import { api, listRecords } from "../api";
import { useApp } from "../AppContext";
import { parseApiDate } from "../format";
import { InviteLinks } from "./InviteLinks";
import LinkIcon from "@mui/icons-material/Link";

import { t, language, tx } from "../i18n";
interface Member { email?: string | null; user_id: number; username: string; role_id: number; role_name: string; isActive: number; createdAt?: string; is_you: boolean; tenant_id?: number | null; tenant_assigned?: boolean }
interface Role { role_id: number; role_name: string; is_admin: boolean }

export const Members: React.FC = () => {
  const [members, setMembers] = useState<Member[]>([]);
  const [roles, setRoles] = useState<Role[]>([]);
  const [error, setError] = useState<string>();
  const [inviting, setInviting] = useState(false);
  const [links, setLinks] = useState(false);
  const [emailFor, setEmailFor] = useState<{ m: Member; value: string } | null>(null);
  const saveEmail = async () => {
    if (!emailFor) return;
    setError(undefined);
    try { await api("PUT", `Access/members/${emailFor.m.user_id}/email`, { email: emailFor.value.trim() }); setEmailFor(null); load(); }
    catch (e: any) { setError(e.message); setEmailFor(null); }
  };
  const [form, setForm] = useState({ username: "", password: "", role_id: 0, tenant_id: 0 });
  const { me } = useApp();
  // platform administrators place members in a tenant (0 = platform user); tenant administrators' members join their own tenant
  const pickTenant = !!(me?.tenancy && me?.is_platform && me?.tenant_table);
  const [tenants, setTenants] = useState<{ id: number; label: string }[]>([]);
  useEffect(() => {
    if (!pickTenant) return;
    listRecords(me!.tenant_table!, 1, 500).then((p: any) => {
      const rows: any[] = p?.records || p?.rows || [];
      setTenants(rows.map((r) => {
        const id = Number(Object.entries(r).find(([k]) => k.endsWith("_id"))?.[1]);
        return { id, label: String(r.name ?? r.title ?? r.legal_name ?? r.tenant_code ?? `#${id}`) };
      }));
    }).catch(() => setTenants([]));
  }, [pickTenant, me]);
  const tenantLabel = (id?: number | null) => (id ? tenants.find((t) => t.id === id)?.label || `#${id}` : t("Platform"));
  const unassigned = members.filter((m) => me?.tenancy && m.tenant_assigned === false);
  const [bulkTenant, setBulkTenant] = useState(0);
  const assignAll = async () => {
    if (!bulkTenant) return;
    setBusy(true);
    try { await api("POST", "Access/members/assign-tenant", { tenant_id: bulkTenant }); await load(); }
    catch (e: any) { setError(e.message); } finally { setBusy(false); }
  };
  const [busy, setBusy] = useState(false);

  const load = useCallback(async () => {
    try {
      const [m, r] = await Promise.all([api<any>("GET", "Access/members"), api<any>("GET", "Access/roles")]);
      setMembers(m.document || []);
      setRoles(r.document || []);
    } catch (e: any) {
      setError(e.message);
    }
  }, []);
  useEffect(() => { load(); }, [load]);

  const update = async (m: Member, patch: Partial<Member>) => {
    setError(undefined);
    try {
      await api("PUT", `Access/members/${m.user_id}`, { role_id: patch.role_id ?? m.role_id, isActive: patch.isActive ?? m.isActive, ...(patch.tenant_id !== undefined ? { tenant_id: patch.tenant_id ?? 0 } : {}) });
      load();
    } catch (e: any) {
      setError(e.message);
    }
  };

  const invite = async () => {
    setBusy(true);
    setError(undefined);
    try {
      await api("POST", "Access/members", pickTenant ? form : { ...form, tenant_id: undefined });
      setInviting(false);
      setForm({ username: "", password: "", role_id: 0, tenant_id: 0 });
      load();
    } catch (e: any) {
      setError(e.message);
    } finally {
      setBusy(false);
    }
  };

  return (
    <Box>
      <Box sx={{ display: "flex", alignItems: "center", mb: 2.5 }}>
        <Box sx={{ flex: 1 }}>
          <Typography variant="h5" sx={{
            fontWeight: 700
          }}>{t("Members")}</Typography>
          <Typography variant="body2" sx={{
            color: "text.secondary"
          }}>{t("People in your workspace and the role that controls what they can see and change.")}</Typography>
        </Box>
        <Button variant="outlined" startIcon={<LinkIcon />} sx={{ mr: 1 }} onClick={() => setLinks(true)}>{t("Invite link")}</Button>
        <Button variant="contained" startIcon={<PersonAddOutlined />} onClick={() => { setForm({ username: "", password: "", role_id: roles.find((r) => !r.is_admin)?.role_id || 1, tenant_id: tenants[0]?.id || 0 }); setInviting(true); }}>{t("Add member")}</Button>
      </Box>
      {error && <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(undefined)}>{error}</Alert>}
      {unassigned.length > 0 && (
        <Alert severity="warning" sx={{ mb: 2, alignItems: "center" }}
          action={pickTenant ? (
            <Box sx={{ display: "flex", gap: 1, alignItems: "center" }}>
              <TextField select size="small" label={t("Tenant")} value={bulkTenant} onChange={(e) => setBulkTenant(Number(e.target.value))} sx={{ minWidth: 220 }}>
                <MenuItem value={0} disabled>{t("Choose a tenant")}</MenuItem>
                {tenants.map((t) => <MenuItem key={t.id} value={t.id}>{t.label}</MenuItem>)}
              </TextField>
              <Button variant="contained" size="small" disabled={!bulkTenant || busy} onClick={assignAll}>{t("Assign all")}</Button>
            </Box>
          ) : undefined}>
          {t("{count} members have no tenant and can't see any data until assigned.", { count: unassigned.length })}</Alert>
      )}
      <Paper variant="outlined" sx={{ borderRadius: 2, overflow: "hidden" }}>
        <Table>
          <TableHead>
            <TableRow sx={{ "& th": { bgcolor: "grey.50", fontWeight: 600, fontSize: 12, textTransform: "uppercase", color: "text.secondary" } }}>
              <TableCell>{t("Username")}</TableCell><TableCell>{t("Sign-in email")}</TableCell><TableCell>{t("Role")}</TableCell>{(pickTenant || me?.tenancy) && <TableCell>{t("Tenant")}</TableCell>}<TableCell>{t("Active")}</TableCell><TableCell>{t("Added")}</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {members.map((m) => (
              <TableRow key={m.user_id}>
                <TableCell sx={{ fontWeight: 600 }}>{m.username} {m.is_you && <Chip size="small" label="you" sx={{ ml: 1 }} />}</TableCell>
                <TableCell>
                  <Button size="small" sx={{ textTransform: "none" }} onClick={() => setEmailFor({ m, value: m.email || "" })}>{m.email || t("Add email")}</Button>
                </TableCell>
                <TableCell sx={{ width: 240 }}>
                  <TextField select size="small" fullWidth value={m.role_id} disabled={m.is_you} onChange={(e) => update(m, { role_id: Number(e.target.value) })}>
                    {roles.map((r) => <MenuItem key={r.role_id} value={r.role_id}>{tx(r.role_name)}</MenuItem>)}
                  </TextField>
                </TableCell>
                {pickTenant ? (
                  <TableCell sx={{ width: 220 }}>
                    <TextField select size="small" fullWidth value={m.tenant_assigned === false ? -1 : m.tenant_id || 0} disabled={m.is_you}
                      error={m.tenant_assigned === false} onChange={(e) => update(m, { tenant_id: Number(e.target.value) || null })}>
                      {m.tenant_assigned === false && <MenuItem value={-1} disabled>{t("Unassigned")}</MenuItem>}
                      <MenuItem value={0}>{t("Platform")}</MenuItem>
                      {tenants.map((t) => <MenuItem key={t.id} value={t.id}>{t.label}</MenuItem>)}
                    </TextField>
                  </TableCell>
                ) : me?.tenancy ? <TableCell>{m.tenant_assigned === false ? <Chip size="small" color="warning" label={t("Unassigned")} /> : tenantLabel(m.tenant_id)}</TableCell> : null}
                <TableCell><Switch checked={m.isActive === 1} disabled={m.is_you} onChange={(e) => update(m, { isActive: e.target.checked ? 1 : 0 })} /></TableCell>
                <TableCell>{parseApiDate(m.createdAt, true)?.toLocaleDateString(language()) || "—"}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </Paper>
      <Dialog open={inviting} onClose={() => setInviting(false)} fullWidth maxWidth="xs">
        <DialogTitle>{t("Add member")}</DialogTitle>
        <DialogContent dividers>
          <TextField label={t("Username")} name="username" fullWidth margin="dense" value={form.username} onChange={(e) => setForm({ ...form, username: e.target.value })} />
          <TextField label={t("Temporary password")} name="password" type="password" fullWidth margin="dense" helperText={t("At least 10 characters with letters and digits. Share it with them securely.")}
            value={form.password} onChange={(e) => setForm({ ...form, password: e.target.value })} />
          <TextField select label={t("Role")} name="role_id" fullWidth margin="dense" value={form.role_id} onChange={(e) => setForm({ ...form, role_id: Number(e.target.value) })}>
            {roles.map((r) => <MenuItem key={r.role_id} value={r.role_id}>{tx(r.role_name)}</MenuItem>)}
          </TextField>
          {pickTenant && (
            <TextField select label={t("Tenant")} name="tenant_id" fullWidth margin="dense" value={form.tenant_id}
              helperText={t("Which company's data this person works with. Platform users see every tenant.")}
              onChange={(e) => setForm({ ...form, tenant_id: Number(e.target.value) })}>
              <MenuItem value={0}>{t("Platform")}</MenuItem>
              {tenants.map((t) => <MenuItem key={t.id} value={t.id}>{t.label}</MenuItem>)}
            </TextField>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setInviting(false)}>{t("Cancel")}</Button>
          <Button variant="contained" onClick={invite} disabled={busy || !form.username || form.password.length < 10}>{t("Add member")}</Button>
        </DialogActions>
      </Dialog>
      <Dialog open={!!emailFor} onClose={() => setEmailFor(null)} fullWidth maxWidth="xs">
        <DialogTitle>{t("Sign-in email for {name}", { name: emailFor?.m.username })}</DialogTitle>
        <DialogContent>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>{t("They can sign in with a code sent to this email. Leave empty to remove it.")}</Typography>
          <TextField autoFocus fullWidth size="small" type="email" label={t("Email")} value={emailFor?.value || ""} onChange={(e) => emailFor && setEmailFor({ ...emailFor, value: e.target.value })} />
        </DialogContent>
        <DialogActions><Button onClick={() => setEmailFor(null)}>{t("Cancel")}</Button><Button variant="contained" onClick={saveEmail}>{t("Save")}</Button></DialogActions>
      </Dialog>
      <InviteLinks open={links} onClose={() => { setLinks(false); load(); }} roles={roles} pickTenant={pickTenant} tenants={tenants} />
    </Box>
  );
};
