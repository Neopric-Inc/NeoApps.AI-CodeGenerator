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
import { useApp } from "./AppContext";

import { t, language, lower } from "./i18n";
export interface ShareLink {
  id: number; table: string; kind: "form" | "record"; recordId?: string; fields: string[]; fixedValues?: Record<string, any>;
  access: "anyone" | "members"; title?: string; expiresAt?: string; maxResponses?: number; responses: number; isOpen: boolean;
  createdBy: string; url: string; showFields?: Record<string, { table: string; fields: string[] }>; allowFiles?: boolean;
}

const fullUrl = (u: string) => window.location.origin + u;

/**
 * Share a form for a table (new responses) or a link to one record (update chosen fields), like Google Forms:
 * anyone with the link or only signed-in members, optional expiry and response limit, close/reopen, new link.
 */
export const ShareDialog: React.FC<{ table: ResolvedTable; recordId?: any; open: boolean; onClose: () => void }> = ({ table, recordId, open, onClose }) => {
  const { tables } = useApp();
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
  const [show, setShow] = useState<Record<string, string[]>>({}); // reference column -> fields of the linked record shown above the form
  const [allowFiles, setAllowFiles] = useState(false);
  const showable = (refCol: string) => {
    const target = tables[table.fields[refCol]?.column.references?.table || ""];
    return target ? target.formFields.map((n) => target.fields[n]).filter((f) => f && !f.column.references && !AUDIT_FIELDS.includes(f.name)) : [];
  };

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
    setTitle(isRecord ? t("Update your {item} details", { item: lower(table.singular) }) : table.singular);
    setExpires(""); setMax(""); setAccess("anyone"); setShow({}); setAllowFiles(false);
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, load]);

  const create = async () => {
    setError(undefined);
    const missing = references.filter((f) => f.required && (fixed[f.name] === undefined || fixed[f.name] === ""));
    if (missing.length) { setError(t("Choose {fields} for every response.", { fields: missing.map((f) => f.label).join(", ") })); return; }
    if (!fields.length) { setError(t("Choose at least one field to share.")); return; }
    setCreating(true);
    try {
      const body: any = { table: table.name, fields, access, title: title.trim() || undefined };
      if (isRecord) body.recordId = String(recordId);
      const fx = Object.fromEntries(Object.entries(fixed).filter(([, v]) => v !== undefined && v !== ""));
      if (Object.keys(fx).length) body.fixedValues = fx;
      if (expires) body.expiresAt = new Date(expires + "T23:59:59").toISOString();
      if (max) body.maxResponses = Number(max);
      const sh = Object.fromEntries(Object.entries(show).filter(([k, v]) => v.length && fx[k] !== undefined));
      if (Object.keys(sh).length) body.showFields = sh;
      if (!isRecord && allowFiles) body.allowFiles = true;
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
      <DialogTitle>{isRecord ? t("Share this {item}", { item: lower(table.singular) }) : t("Share a {item} form", { item: lower(table.singular) })}</DialogTitle>
      <DialogContent dividers>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
          {isRecord
            ? t("People with the link can see and change only the fields you pick, on this record only.")
            : t("People with the link can fill in a new entry with only the fields you pick. They can't see other entries.")}
        </Typography>
        <Stack spacing={2}>
          <TextField size="small" label={t("Title shown on the form")} value={title} onChange={(e) => setTitle(e.target.value)} />
          <Box>
            <Typography variant="subtitle2" sx={{ mb: 0.5 }}>{t("Fields")}</Typography>
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
                renderInput={(p) => <TextField {...p} label={t("{field} for every response", { field: f.label }) + (f.required ? " *" : "")} />} />
            );
          })}
          {references.filter((f) => fixed[f.name] !== undefined && fixed[f.name] !== "").map((f) => (
            <Box key={`show-${f.name}`}>
              <Typography variant="subtitle2" sx={{ mb: 0.5 }}>{t("Show the chosen {item} above the form (with its files)", { item: lower(f.label) })}</Typography>
              <Box sx={{ display: "flex", flexWrap: "wrap" }}>
                {showable(f.name).map((sf) => (
                  <FormControlLabel key={sf.name} sx={{ mr: 2 }} label={sf.label} control={
                    <Checkbox size="small" checked={(show[f.name] || []).includes(sf.name)}
                      onChange={(e) => setShow((s) => ({ ...s, [f.name]: e.target.checked ? [...(s[f.name] || []), sf.name] : (s[f.name] || []).filter((x) => x !== sf.name) }))} />} />
                ))}
              </Box>
            </Box>
          ))}
          {!isRecord && (
            <FormControlLabel label={t("Let people attach files (e.g. a resume): PDF, Word, text or images, up to 10 MB")} control={
              <Switch checked={allowFiles} onChange={(e) => setAllowFiles(e.target.checked)} />} />
          )}
          <TextField size="small" select label={t("Who can open it")} value={access} onChange={(e) => setAccess(e.target.value as any)}>
            <MenuItem value="anyone">{t("Anyone with the link")}</MenuItem>
            <MenuItem value="members">{t("Only people signed in to this app")}</MenuItem>
          </TextField>
          <Box sx={{ display: "flex", gap: 2 }}>
            <TextField size="small" type="date" label={t("Stops working after")} value={expires} onChange={(e) => setExpires(e.target.value)}
              slotProps={{ inputLabel: { shrink: true } }} sx={{ flex: 1 }} />
            {!isRecord && <TextField size="small" type="number" label={t("Maximum responses")} value={max} onChange={(e) => setMax(e.target.value)} sx={{ flex: 1 }} />}
          </Box>
          <Box><Button variant="contained" onClick={create} disabled={creating}>{creating ? <CircularProgress size={20} color="inherit" /> : t("Create link")}</Button></Box>
        </Stack>
        {error && <Alert severity="error" sx={{ mt: 2 }}>{error}</Alert>}
        <Divider sx={{ my: 2.5 }} />
        <Typography variant="subtitle2" sx={{ mb: 1 }}>{t("Links")}</Typography>
        {loading && <CircularProgress size={20} />}
        {!loading && links.length === 0 && <Typography variant="body2" color="text.secondary">{t("No links yet.")}</Typography>}
        <Stack spacing={1.5}>
          {links.map((l) => (
            <Box key={l.id} sx={{ border: 1, borderColor: "divider", borderRadius: 1.5, p: 1.5 }}>
              <Box sx={{ display: "flex", alignItems: "center", gap: 1, flexWrap: "wrap", mb: 1 }}>
                <Typography variant="body2" sx={{ fontWeight: 600, flex: 1 }}>{l.title || table.singular}</Typography>
                <Chip size="small" label={l.access === "members" ? t("Members only") : t("Anyone with the link")} />
                <FormControlLabel sx={{ m: 0 }} label={l.isOpen ? t("Open") : t("Closed")} control={
                  <Switch size="small" checked={l.isOpen} onChange={(e) => change(l, "PUT", `Shares/${l.id}`, { isOpen: e.target.checked })} />} />
              </Box>
              <Box sx={{ display: "flex", gap: 1, alignItems: "center" }}>
                <TextField size="small" fullWidth value={fullUrl(l.url)} slotProps={{ input: { readOnly: true } }} onFocus={(e) => e.target.select()} />
                <Tooltip title={copied === l.id ? t("Copied") : t("Copy link")}><IconButton onClick={() => copy(l)}><ContentCopy fontSize="small" /></IconButton></Tooltip>
                <Tooltip title={t("New link (the old one stops working)")}><IconButton onClick={() => change(l, "POST", `Shares/${l.id}/regenerate`)}><Refresh fontSize="small" /></IconButton></Tooltip>
                <Tooltip title={t("Delete link")}><IconButton onClick={() => change(l, "DELETE", `Shares/${l.id}`)}><DeleteOutline fontSize="small" /></IconButton></Tooltip>
              </Box>
              {l.kind === "form" && l.maxResponses && l.responses >= l.maxResponses && (
                <Alert severity="warning" sx={{ my: 1 }} action={
                  <Button size="small" onClick={() => change(l, "PUT", `Shares/${l.id}`, { maxResponses: (l.maxResponses || 0) + 10 })}>{t("Allow 10 more")}</Button>
                }>
                  {t("Full: this link has its {count} responses. A new link keeps the same limit; allow more to take new responses.", { count: l.maxResponses })}</Alert>
              )}
              <Typography variant="caption" color="text.secondary">
                {l.fields.map(label).join(", ")} · {l.kind === "form"
                  ? (l.maxResponses ? t("{count} of {max} responses", { count: l.responses, max: l.maxResponses }) : t("{count} responses", { count: l.responses }))
                  : (l.maxResponses ? t("{count} of {max} saves", { count: l.responses, max: l.maxResponses }) : t("{count} saves", { count: l.responses }))}
                {l.expiresAt ? " · " + t("until {date}", { date: new Date(l.expiresAt).toLocaleDateString(language()) }) : ""}{" · "}{t("by {name}", { name: l.createdBy })}
              </Typography>
            </Box>
          ))}
        </Stack>
      </DialogContent>
      <DialogActions><Button onClick={onClose}>{t("Done")}</Button></DialogActions>
    </Dialog>
  );
};
