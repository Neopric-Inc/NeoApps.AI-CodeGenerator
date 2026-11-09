import React, { useCallback, useEffect, useMemo, useState } from "react";
import {
  Alert, Box, Button, Checkbox, Chip, Dialog, DialogActions, DialogContent, DialogTitle, List, ListItemButton, ListItemText,
  Paper, Snackbar, Table, TableBody, TableCell, TableHead, TableRow, TextField, Typography,
} from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import { api } from "../api";
import { useApp } from "../AppContext";

interface Role { role_id: number; role_name: string; members: number; is_admin: boolean }
interface Perm { entity: string; read: boolean; create: boolean; update: boolean; delete: boolean }
const ACTIONS: (keyof Omit<Perm, "entity">)[] = ["read", "create", "update", "delete"];
const ACTION_LABEL = { read: "View", create: "Create", update: "Edit", delete: "Delete" };

export const Roles: React.FC = () => {
  const { tables, refreshSession } = useApp();
  const [roles, setRoles] = useState<Role[]>([]);
  const [selected, setSelected] = useState<number>();
  const [perms, setPerms] = useState<Perm[]>([]);
  const [dirty, setDirty] = useState(false);
  const [error, setError] = useState<string>();
  const [saved, setSaved] = useState(false);
  const [creating, setCreating] = useState(false);
  const [newName, setNewName] = useState("");
  const [showSystem, setShowSystem] = useState(false);

  const loadRoles = useCallback(async () => {
    const r = await api<any>("GET", "Access/roles");
    setRoles(r.document || []);
    return (r.document || []) as Role[];
  }, []);

  useEffect(() => {
    loadRoles().then((r) => setSelected((s) => s ?? (r.find((x) => !x.is_admin) || r[0])?.role_id)).catch((e) => setError(e.message));
  }, [loadRoles]);

  useEffect(() => {
    if (selected === undefined) return;
    setDirty(false);
    api<any>("GET", `Access/roles/${selected}/permissions`).then((r) => setPerms(r.document || [])).catch((e) => setError(e.message));
  }, [selected]);

  const role = roles.find((r) => r.role_id === selected);
  // App tables first (labelled), then the generator's internal tables if requested.
  const rows = useMemo(() => {
    const app = perms.filter((p) => tables[p.entity]).sort((a, b) => tables[a.entity].label.localeCompare(tables[b.entity].label));
    const sys = perms.filter((p) => !tables[p.entity]);
    return showSystem ? [...app, ...sys] : app;
  }, [perms, tables, showSystem]);

  const toggle = (entity: string, action: keyof Omit<Perm, "entity">, value: boolean) => {
    setPerms((ps) => ps.map((p) => {
      if (p.entity !== entity) return p;
      const next = { ...p, [action]: value };
      if (action !== "read" && value) next.read = true; // can't change what you can't see
      if (action === "read" && !value) { next.create = next.update = next.delete = false; }
      return next;
    }));
    setDirty(true);
  };
  const toggleColumn = (action: keyof Omit<Perm, "entity">, value: boolean) => {
    rows.forEach((p) => toggle(p.entity, action, value));
  };

  const save = async () => {
    try {
      await api("PUT", `Access/roles/${selected}/permissions`, perms);
      setDirty(false);
      setSaved(true);
      refreshSession();
    } catch (e: any) {
      setError(e.message);
    }
  };

  const create = async () => {
    try {
      const r = await api<any>("POST", "Access/roles", { role_name: newName });
      setCreating(false);
      setNewName("");
      await loadRoles();
      setSelected(r.document.role_id);
    } catch (e: any) {
      setError(e.message);
    }
  };

  return (
    <Box>
      <Box sx={{ display: "flex", alignItems: "center", mb: 2.5 }}>
        <Box sx={{ flex: 1 }}>
          <Typography variant="h5" sx={{
            fontWeight: 700
          }}>Roles & permissions</Typography>
          <Typography variant="body2" sx={{
            color: "text.secondary"
          }}>Choose what each role can view, create, edit and delete. Changes apply to every member with that role.</Typography>
        </Box>
        <Button startIcon={<AddIcon />} variant="outlined" onClick={() => setCreating(true)}>New role</Button>
      </Box>
      {error && <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(undefined)}>{error}</Alert>}
      <Box sx={{ display: "grid", gridTemplateColumns: { xs: "1fr", md: "240px 1fr" }, gap: 2 }}>
        <Paper variant="outlined" sx={{ borderRadius: 2, alignSelf: "start" }}>
          <List dense>
            {roles.map((r) => (
              <ListItemButton key={r.role_id} selected={r.role_id === selected} onClick={() => setSelected(r.role_id)}>
                <ListItemText primary={r.role_name} secondary={`${r.members} member${r.members === 1 ? "" : "s"}`} />
                {r.is_admin && <Chip size="small" label="full access" />}
              </ListItemButton>
            ))}
          </List>
        </Paper>
        <Paper variant="outlined" sx={{ borderRadius: 2, overflow: "hidden" }}>
          {role?.is_admin && <Alert severity="info" sx={{ borderRadius: 0 }}>Administrators always have full access and can manage members, roles and the UI.</Alert>}
          <Table size="small">
            <TableHead>
              <TableRow sx={{ "& th": { bgcolor: "grey.50", fontWeight: 600 } }}>
                <TableCell>Table</TableCell>
                {ACTIONS.map((a) => (
                  <TableCell key={a} align="center">
                    {ACTION_LABEL[a]}
                    {!role?.is_admin && <Checkbox size="small" checked={rows.length > 0 && rows.every((p) => p[a])} indeterminate={rows.some((p) => p[a]) && !rows.every((p) => p[a])}
                      onChange={(e) => toggleColumn(a, e.target.checked)} />}
                  </TableCell>
                ))}
              </TableRow>
            </TableHead>
            <TableBody>
              {rows.map((p) => (
                <TableRow key={p.entity} hover>
                  <TableCell>{tables[p.entity]?.label || <Typography variant="body2" sx={{
                    color: "text.secondary"
                  }}>{p.entity} (system)</Typography>}</TableCell>
                  {ACTIONS.map((a) => (
                    <TableCell key={a} align="center">
                      <Checkbox size="small" checked={p[a]} disabled={role?.is_admin} onChange={(e) => toggle(p.entity, a, e.target.checked)} slotProps={{
                        input: { "aria-label": `${p.entity} ${a}` }
                      }} />
                    </TableCell>
                  ))}
                </TableRow>
              ))}
            </TableBody>
          </Table>
          <Box sx={{ display: "flex", alignItems: "center", gap: 1, p: 1.5, borderTop: 1, borderColor: "divider" }}>
            <Button size="small" onClick={() => setShowSystem((s) => !s)}>{showSystem ? "Hide" : "Show"} system tables</Button>
            <Box sx={{ flex: 1 }} />
            {!role?.is_admin && <Button variant="contained" disabled={!dirty} onClick={save}>Save permissions</Button>}
          </Box>
        </Paper>
      </Box>
      <Dialog open={creating} onClose={() => setCreating(false)} fullWidth maxWidth="xs">
        <DialogTitle>New role</DialogTitle>
        <DialogContent dividers>
          <TextField autoFocus label="Role name" fullWidth value={newName} onChange={(e) => setNewName(e.target.value)} helperText="Starts with view-only access to every table." />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setCreating(false)}>Cancel</Button>
          <Button variant="contained" disabled={!newName.trim()} onClick={create}>Create role</Button>
        </DialogActions>
      </Dialog>
      <Snackbar open={saved} autoHideDuration={3000} onClose={() => setSaved(false)}>
        <Alert severity="success" onClose={() => setSaved(false)}>Permissions saved</Alert>
      </Snackbar>
    </Box>
  );
};
