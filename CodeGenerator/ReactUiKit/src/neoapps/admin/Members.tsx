import React, { useCallback, useEffect, useState } from "react";
import {
  Alert, Box, Button, Chip, Dialog, DialogActions, DialogContent, DialogTitle, MenuItem, Paper, Switch, Table, TableBody,
  TableCell, TableHead, TableRow, TextField, Typography,
} from "@mui/material";
import PersonAddOutlined from "@mui/icons-material/PersonAddOutlined";
import { api, listRecords } from "../api";
import { useApp } from "../AppContext";
import { parseApiDate } from "../format";

interface Member { user_id: number; username: string; role_id: number; role_name: string; isActive: number; createdAt?: string; is_you: boolean; tenant_id?: number | null; tenant_assigned?: boolean }
interface Role { role_id: number; role_name: string; is_admin: boolean }

export const Members: React.FC = () => {
  const [members, setMembers] = useState<Member[]>([]);
  const [roles, setRoles] = useState<Role[]>([]);
  const [error, setError] = useState<string>();
  const [inviting, setInviting] = useState(false);
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
  const tenantLabel = (id?: number | null) => (id ? tenants.find((t) => t.id === id)?.label || `#${id}` : "Platform");
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
          <Typography variant="h5" fontWeight={700}>Members</Typography>
          <Typography variant="body2" color="text.secondary">People in your workspace and the role that controls what they can see and change.</Typography>
        </Box>
        <Button variant="contained" startIcon={<PersonAddOutlined />} onClick={() => { setForm({ username: "", password: "", role_id: roles.find((r) => !r.is_admin)?.role_id || 1, tenant_id: tenants[0]?.id || 0 }); setInviting(true); }}>Add member</Button>
      </Box>
      {error && <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(undefined)}>{error}</Alert>}
      {unassigned.length > 0 && (
        <Alert severity="warning" sx={{ mb: 2, alignItems: "center" }}
          action={pickTenant ? (
            <Box sx={{ display: "flex", gap: 1, alignItems: "center" }}>
              <TextField select size="small" label="Tenant" value={bulkTenant} onChange={(e) => setBulkTenant(Number(e.target.value))} sx={{ minWidth: 220 }}>
                <MenuItem value={0} disabled>Choose a tenant</MenuItem>
                {tenants.map((t) => <MenuItem key={t.id} value={t.id}>{t.label}</MenuItem>)}
              </TextField>
              <Button variant="contained" size="small" disabled={!bulkTenant || busy} onClick={assignAll}>Assign all</Button>
            </Box>
          ) : undefined}>
          {unassigned.length} member{unassigned.length === 1 ? " has" : "s have"} no tenant and can't see any data until assigned.
        </Alert>
      )}
      <Paper variant="outlined" sx={{ borderRadius: 2, overflow: "hidden" }}>
        <Table>
          <TableHead>
            <TableRow sx={{ "& th": { bgcolor: "grey.50", fontWeight: 600, fontSize: 12, textTransform: "uppercase", color: "text.secondary" } }}>
              <TableCell>Username</TableCell><TableCell>Role</TableCell>{(pickTenant || me?.tenancy) && <TableCell>Tenant</TableCell>}<TableCell>Active</TableCell><TableCell>Added</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {members.map((m) => (
              <TableRow key={m.user_id}>
                <TableCell sx={{ fontWeight: 600 }}>{m.username} {m.is_you && <Chip size="small" label="you" sx={{ ml: 1 }} />}</TableCell>
                <TableCell sx={{ width: 240 }}>
                  <TextField select size="small" fullWidth value={m.role_id} disabled={m.is_you} onChange={(e) => update(m, { role_id: Number(e.target.value) })}>
                    {roles.map((r) => <MenuItem key={r.role_id} value={r.role_id}>{r.role_name}</MenuItem>)}
                  </TextField>
                </TableCell>
                {pickTenant ? (
                  <TableCell sx={{ width: 220 }}>
                    <TextField select size="small" fullWidth value={m.tenant_assigned === false ? -1 : m.tenant_id || 0} disabled={m.is_you}
                      error={m.tenant_assigned === false} onChange={(e) => update(m, { tenant_id: Number(e.target.value) || null })}>
                      {m.tenant_assigned === false && <MenuItem value={-1} disabled>Unassigned</MenuItem>}
                      <MenuItem value={0}>Platform</MenuItem>
                      {tenants.map((t) => <MenuItem key={t.id} value={t.id}>{t.label}</MenuItem>)}
                    </TextField>
                  </TableCell>
                ) : me?.tenancy ? <TableCell>{m.tenant_assigned === false ? <Chip size="small" color="warning" label="Unassigned" /> : tenantLabel(m.tenant_id)}</TableCell> : null}
                <TableCell><Switch checked={m.isActive === 1} disabled={m.is_you} onChange={(e) => update(m, { isActive: e.target.checked ? 1 : 0 })} /></TableCell>
                <TableCell>{parseApiDate(m.createdAt)?.toLocaleDateString() || "—"}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </Paper>
      <Dialog open={inviting} onClose={() => setInviting(false)} fullWidth maxWidth="xs">
        <DialogTitle>Add member</DialogTitle>
        <DialogContent dividers>
          <TextField label="Username" name="username" fullWidth margin="dense" value={form.username} onChange={(e) => setForm({ ...form, username: e.target.value })} />
          <TextField label="Temporary password" name="password" type="password" fullWidth margin="dense" helperText="At least 8 characters. Share it with them securely."
            value={form.password} onChange={(e) => setForm({ ...form, password: e.target.value })} />
          <TextField select label="Role" name="role_id" fullWidth margin="dense" value={form.role_id} onChange={(e) => setForm({ ...form, role_id: Number(e.target.value) })}>
            {roles.map((r) => <MenuItem key={r.role_id} value={r.role_id}>{r.role_name}</MenuItem>)}
          </TextField>
          {pickTenant && (
            <TextField select label="Tenant" name="tenant_id" fullWidth margin="dense" value={form.tenant_id}
              helperText="Which company's data this person works with. Platform users see every tenant."
              onChange={(e) => setForm({ ...form, tenant_id: Number(e.target.value) })}>
              <MenuItem value={0}>Platform</MenuItem>
              {tenants.map((t) => <MenuItem key={t.id} value={t.id}>{t.label}</MenuItem>)}
            </TextField>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setInviting(false)}>Cancel</Button>
          <Button variant="contained" onClick={invite} disabled={busy || !form.username || form.password.length < 8}>Add member</Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};
