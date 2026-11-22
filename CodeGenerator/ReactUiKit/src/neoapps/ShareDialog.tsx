import React, { useCallback, useEffect, useMemo, useState } from "react";
import {
  Alert, Autocomplete, Box, Button, Checkbox, Chip, CircularProgress, Dialog, DialogActions, DialogContent, DialogTitle, Divider,
  FormControlLabel, IconButton, MenuItem, Stack, Switch, TextField, Tooltip, Typography,
} from "@mui/material";
import ContentCopy from "@mui/icons-material/ContentCopy";
import DeleteOutline from "@mui/icons-material/DeleteOutlineOutlined";
import Refresh from "@mui/icons-material/Refresh";
import { api } from "./api";
import { AUDIT_FIELDS } from "./config";
import { ResolvedTable } from "./types";
import { useReferences } from "./useReferences";

export interface ShareLink {
  id: number; table: string; kind: "form" | "record"; recordId?: string; fields: string[]; fixedValues?: Record<string, any>;
  access: "anyone" | "members"; title?: string; expiresAt?: string; maxResponses?: number; responses: number; isOpen: boolean;
  createdBy: string; url: string;
}

const fullUrl = (u: string) => window.location.origin + u;

/**
 * Share a form for a table (new responses) or a link to one record (update chosen fields), like Google Forms:
 * anyone with the link or only signed-in members, optional expiry and response limit, close/reopen, new link.
 */
export const ShareDialog: React.FC<{ table: ResolvedTable; recordId?: any; open: boolean; onClose: () => void }> = ({ table, recordId, open, onClose }) => {
  const isRecord = recordId !== undefined && recordId !== null;
  const { refs } = useReferences(table);
  const candidates = useMemo(() => table.formFields.map((n) => table.fields[n])
    .filter((f) => f && !f.column.isPrimaryKey && !AUDIT_FIELDS.includes(f.name) && !f.readOnly), [table]);
  const shareable = candidates.filter((f) => !f.column.references);
  const references = isRecord ? [] : candidates.filter((f) => f.column.references);

  const [links, setLinks] = useState<ShareLink[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string>();
  const [creating, setCreating] = useState(false);
  const [fields, setFields] = useState<string[]>([]);
  const [fixed, setFixed] = useState<Record<string, any>>({});
  const [access, setAccess] = useState<"anyone" | "members">("anyone");
  const [title, setTitle] = useState("");
  const [expires, setExpires] = useState("");
  const [max, setMax] = useState("");
  const [copied, setCopied] = useState<number>();

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const q = new URLSearchParams({ table: table.name });
      if (isRecord) q.set("recordId", String(recordId));
      const res = await api<any>("GET", `Shares?${q}`);
      setLinks(res?.items || []);
    } catch (e: any) { setError(e.message); } finally { setLoading(false); }
  }, [table.name, recordId, isRecord]);

  useEffect(() => {
    if (!open) return;
    setError(undefined);
    setFields(shareable.map((f) => f.name));
    setFixed({});
    setTitle(isRecord ? `Update your ${table.singular.toLowerCase()} details` : table.singular);
    setExpires(""); setMax(""); setAccess("anyone");
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, load]);

  const create = async () => {
    setError(undefined);
    const missing = references.filter((f) => f.required && (fixed[f.name] === undefined || fixed[f.name] === ""));
    if (missing.length) { setError(`Choose ${missing.map((f) => f.label).join(", ")} for every response.`); return; }
    if (!fields.length) { setError("Choose at least one field to share."); return; }
    setCreating(true);
    try {
      const body: any = { table: table.name, fields, access, title: title.trim() || undefined };
      if (isRecord) body.recordId = String(recordId);
      const fx = Object.fromEntries(Object.entries(fixed).filter(([, v]) => v !== undefined && v !== ""));
      if (Object.keys(fx).length) body.fixedValues = fx;
      if (expires) body.expiresAt = new Date(expires + "T23:59:59").toISOString();
      if (max) body.maxResponses = Number(max);
      const link = await api<ShareLink>("POST", "Shares", body);
      setLinks((l) => [link, ...l]);
      copy(link);
    } catch (e: any) { setError(e.message); } finally { setCreating(false); }
  };

  const change = async (l: ShareLink, method: string, path: string, body?: any) => {
    setError(undefined);
    try {
      const res = await api<any>(method, path, body);
      if (method === "DELETE") setLinks((all) => all.filter((x) => x.id !== l.id));
      else setLinks((all) => all.map((x) => (x.id === l.id ? res : x)));
    } catch (e: any) { setError(e.message); }
  };

  const copy = (l: ShareLink) => {
    navigator.clipboard?.writeText(fullUrl(l.url)).then(() => { setCopied(l.id); setTimeout(() => setCopied(undefined), 2000); }).catch(() => {});
  };

  const label = (n: string) => table.fields[n]?.label || n;

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>{isRecord ? `Share this ${table.singular.toLowerCase()}` : `Share a ${table.singular.toLowerCase()} form`}</DialogTitle>
      <DialogContent dividers>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
          {isRecord
            ? "People with the link can see and change only the fields you pick, on this record only."
            : "People with the link can fill in a new entry with only the fields you pick. They can't see other entries."}
        </Typography>
        <Stack spacing={2}>
          <TextField size="small" label="Title shown on the form" value={title} onChange={(e) => setTitle(e.target.value)} />
          <Box>
            <Typography variant="subtitle2" sx={{ mb: 0.5 }}>Fields</Typography>
            <Box sx={{ display: "flex", flexWrap: "wrap", gap: 0.5 }}>
              {shareable.map((f) => (
                <FormControlLabel key={f.name} sx={{ mr: 2 }} label={f.label + (f.required ? " *" : "")} control={
                  <Checkbox size="small" checked={fields.includes(f.name)}
                    onChange={(e) => setFields((s) => e.target.checked ? [...s, f.name] : s.filter((x) => x !== f.name))} />} />
              ))}
            </Box>
          </Box>
          {references.map((f) => {
            const opts = refs[f.column.references!.table] || [];
            return (
              <Autocomplete key={f.name} size="small" options={opts} getOptionLabel={(o) => o.label}
                value={opts.find((o) => String(o.id) === String(fixed[f.name])) || null}
                onChange={(_, o) => setFixed((s) => ({ ...s, [f.name]: o ? o.id : "" }))}
                renderInput={(p) => <TextField {...p} label={`${f.label} for every response${f.required ? " *" : ""}`} />} />
            );
          })}
          <TextField size="small" select label="Who can open it" value={access} onChange={(e) => setAccess(e.target.value as any)}>
            <MenuItem value="anyone">Anyone with the link</MenuItem>
            <MenuItem value="members">Only people signed in to this app</MenuItem>
          </TextField>
          <Box sx={{ display: "flex", gap: 2 }}>
            <TextField size="small" type="date" label="Stops working after" value={expires} onChange={(e) => setExpires(e.target.value)}
              slotProps={{ inputLabel: { shrink: true } }} sx={{ flex: 1 }} />
            {!isRecord && <TextField size="small" type="number" label="Maximum responses" value={max} onChange={(e) => setMax(e.target.value)} sx={{ flex: 1 }} />}
          </Box>
          <Box><Button variant="contained" onClick={create} disabled={creating}>{creating ? <CircularProgress size={20} color="inherit" /> : "Create link"}</Button></Box>
        </Stack>
        {error && <Alert severity="error" sx={{ mt: 2 }}>{error}</Alert>}
        <Divider sx={{ my: 2.5 }} />
        <Typography variant="subtitle2" sx={{ mb: 1 }}>Links</Typography>
        {loading && <CircularProgress size={20} />}
        {!loading && links.length === 0 && <Typography variant="body2" color="text.secondary">No links yet.</Typography>}
        <Stack spacing={1.5}>
          {links.map((l) => (
            <Box key={l.id} sx={{ border: 1, borderColor: "divider", borderRadius: 1.5, p: 1.5 }}>
              <Box sx={{ display: "flex", alignItems: "center", gap: 1, flexWrap: "wrap", mb: 1 }}>
                <Typography variant="body2" sx={{ fontWeight: 600, flex: 1 }}>{l.title || table.singular}</Typography>
                <Chip size="small" label={l.access === "members" ? "Members only" : "Anyone with the link"} />
                <FormControlLabel sx={{ m: 0 }} label={l.isOpen ? "Open" : "Closed"} control={
                  <Switch size="small" checked={l.isOpen} onChange={(e) => change(l, "PUT", `Shares/${l.id}`, { isOpen: e.target.checked })} />} />
              </Box>
              <Box sx={{ display: "flex", gap: 1, alignItems: "center" }}>
                <TextField size="small" fullWidth value={fullUrl(l.url)} slotProps={{ input: { readOnly: true } }} onFocus={(e) => e.target.select()} />
                <Tooltip title={copied === l.id ? "Copied" : "Copy link"}><IconButton onClick={() => copy(l)}><ContentCopy fontSize="small" /></IconButton></Tooltip>
                <Tooltip title="New link (the old one stops working)"><IconButton onClick={() => change(l, "POST", `Shares/${l.id}/regenerate`)}><Refresh fontSize="small" /></IconButton></Tooltip>
                <Tooltip title="Delete link"><IconButton onClick={() => change(l, "DELETE", `Shares/${l.id}`)}><DeleteOutline fontSize="small" /></IconButton></Tooltip>
              </Box>
              <Typography variant="caption" color="text.secondary">
                {l.fields.map(label).join(", ")} · {l.responses}{l.maxResponses ? ` of ${l.maxResponses}` : ""} {l.kind === "form" ? "responses" : "saves"}
                {l.expiresAt ? ` · until ${new Date(l.expiresAt).toLocaleDateString()}` : ""} · by {l.createdBy}
              </Typography>
            </Box>
          ))}
        </Stack>
      </DialogContent>
      <DialogActions><Button onClick={onClose}>Done</Button></DialogActions>
    </Dialog>
  );
};
