import React, { useEffect, useMemo, useState } from "react";
import {
  Alert, Box, Button, Checkbox, Dialog, DialogActions, DialogContent, DialogTitle, Divider, MenuItem, Paper, Snackbar, Switch,
  Tab, Table, TableBody, TableCell, TableHead, TableRow, Tabs, TextField, Typography,
} from "@mui/material";
import { useApp } from "../AppContext";
import { resolveTable } from "../config";
import { ICONS, Icon } from "../icons";
import { FieldConfig, TableConfig, UiConfig, Widget } from "../types";

const WIDGETS: Widget[] = ["text", "textarea", "number", "currency", "percent", "date", "datetime", "checkbox", "select", "reference", "email", "url", "color"];
const clone = <T,>(o: T): T => JSON.parse(JSON.stringify(o || {}));

/**
 * Visual editor for ui.config.json. Edits preview live across the app; Save stores the config on the server
 * (per workspace) so every member sees it. Export downloads JSON to commit as public/ui.config.json.
 */
export const Customize: React.FC = () => {
  const { baseConfig, schema, tables, saveConfig, previewConfig } = useApp();
  const [draft, setDraft] = useState<UiConfig>(() => clone(baseConfig));
  const [tab, setTab] = useState(0);
  const [tableName, setTableName] = useState<string>(schema.tables[0]?.name || "");
  const [saved, setSaved] = useState(false);
  const [error, setError] = useState<string>();
  const [importing, setImporting] = useState(false);
  const [importText, setImportText] = useState("");

  useEffect(() => {
    previewConfig(draft);
  }, [draft, previewConfig]);
  useEffect(() => () => previewConfig(null), [previewConfig]); // stop previewing when leaving the page

  const update = (fn: (d: UiConfig) => void) => setDraft((d) => { const n = clone(d); n.tables = n.tables || {}; fn(n); return n; });
  const tcfg = (d: UiConfig, t: string): TableConfig => (d.tables![t] = d.tables![t] || {});
  const fcfg = (d: UiConfig, t: string, f: string): FieldConfig => { const tc = tcfg(d, t); tc.fields = tc.fields || {}; return (tc.fields[f] = tc.fields[f] || {}); };

  const appTables = schema.tables.map((t) => t.name);
  const resolved = useMemo(() => {
    const s = schema.tables.find((t) => t.name === tableName);
    return s ? resolveTable(schema, s, (draft.tables || {})[tableName], draft.tables || {}) : undefined;
  }, [schema, tableName, draft]);

  // Menu editor rows: group + order + visible, derived from draft.menu
  const menuRows = useMemo(() => appTables.map((t) => {
    const gi = (draft.menu || []).findIndex((g) => g.items.includes(t));
    return { table: t, group: gi >= 0 ? draft.menu![gi].group : "", order: gi >= 0 ? gi * 100 + draft.menu![gi].items.indexOf(t) : 9999, hidden: !!(draft.tables || {})[t]?.hidden };
  }).sort((a, b) => a.order - b.order || tables[a.table]?.label.localeCompare(tables[b.table]?.label)), [appTables, draft, tables]);

  const setMenuGroup = (table: string, group: string) => update((d) => {
    const groups = (d.menu || []).map((g) => ({ ...g, items: g.items.filter((i) => i !== table) }));
    if (group.trim()) {
      const g = groups.find((x) => x.group === group.trim());
      if (g) g.items.push(table); else groups.push({ group: group.trim(), items: [table] });
    }
    d.menu = groups.filter((g) => g.items.length);
  });
  const moveInMenu = (table: string, delta: number) => update((d) => {
    const g = (d.menu || []).find((x) => x.items.includes(table));
    if (!g) return;
    const i = g.items.indexOf(table);
    const j = i + delta;
    if (j < 0 || j >= g.items.length) return;
    [g.items[i], g.items[j]] = [g.items[j], g.items[i]];
  });

  const save = async () => {
    try {
      await saveConfig(draft);
      setSaved(true);
    } catch (e: any) {
      setError(e.message);
    }
  };
  const exportJson = () => {
    const blob = new Blob([JSON.stringify(draft, null, 2)], { type: "application/json" });
    const a = document.createElement("a");
    a.href = URL.createObjectURL(blob);
    a.download = "ui.config.json";
    a.click();
  };
  const doImport = () => {
    try {
      setDraft(JSON.parse(importText));
      setImporting(false);
    } catch (e: any) {
      setError("Invalid JSON: " + e.message);
    }
  };

  const listCols = resolved?.listColumns || [];
  return (
    <Box>
      <Box sx={{ display: "flex", alignItems: "center", gap: 1, mb: 2, flexWrap: "wrap" }}>
        <Box sx={{ flex: 1, minWidth: 260 }}>
          <Typography variant="h5" sx={{
            fontWeight: 700
          }}>Customize UI</Typography>
          <Typography variant="body2" sx={{
            color: "text.secondary"
          }}>Changes preview live. Save to apply them for everyone in this workspace.</Typography>
        </Box>
        <Button onClick={() => setImporting(true)}>Import JSON</Button>
        <Button onClick={exportJson}>Export JSON</Button>
        <Button onClick={() => setDraft(clone(baseConfig))}>Discard changes</Button>
        <Button variant="contained" onClick={save}>Save</Button>
      </Box>
      {error && <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(undefined)}>{error}</Alert>}
      <Paper variant="outlined" sx={{ borderRadius: 2, overflow: "hidden" }}>
        <Tabs value={tab} onChange={(_, v) => setTab(v)} sx={{ borderBottom: 1, borderColor: "divider", px: 1 }}>
          <Tab label="App" /><Tab label="Menu" /><Tab label="Tables & fields" />
        </Tabs>

        {tab === 0 && (
          <Box sx={{ p: 3, display: "grid", gap: 2, maxWidth: 560 }}>
            <TextField label="App name" value={draft.appName || ""} onChange={(e) => update((d) => { d.appName = e.target.value; })} />
            <TextField label="Tagline" value={draft.tagline || ""} onChange={(e) => update((d) => { d.tagline = e.target.value; })} />
            <Box sx={{ display: "flex", gap: 2 }}>
              <TextField label="Primary color" type="color" sx={{ width: 160 }} value={draft.theme?.primary || "#3b5bdb"} onChange={(e) => update((d) => { d.theme = { ...(d.theme || {}), primary: e.target.value }; })} />
              <TextField label="Secondary color" type="color" sx={{ width: 160 }} value={draft.theme?.secondary || "#0ca678"} onChange={(e) => update((d) => { d.theme = { ...(d.theme || {}), secondary: e.target.value }; })} />
            </Box>
            <TextField select label="Dashboard cards" value={Array.from(new Set((draft.dashboard?.cards || []).map((c) => c.table)))} helperText="Leave empty to show the first eight menu items."
              onChange={(e) => update((d) => { d.dashboard = { ...(d.dashboard || {}), cards: (e.target.value as unknown as string[]).flatMap((table) => { const existing = d.dashboard?.cards?.filter((card) => card.table === table); return existing?.length ? existing : [{ table }]; }) }; })}
              slotProps={{
                select: { multiple: true }
              }}>
              {appTables.map((t) => <MenuItem key={t} value={t}>{tables[t]?.label}</MenuItem>)}
            </TextField>
            <TextField select label="Dashboard 'recently updated' list" value={draft.dashboard?.recentTable || ""}
              onChange={(e) => update((d) => { d.dashboard = { ...(d.dashboard || {}), recentTable: e.target.value || undefined }; })}>
              <MenuItem value=""><em>First dashboard card</em></MenuItem>
              {appTables.map((t) => <MenuItem key={t} value={t}>{tables[t]?.label}</MenuItem>)}
            </TextField>
          </Box>
        )}

        {tab === 1 && (
          <Table size="small">
            <TableHead>
              <TableRow sx={{ "& th": { bgcolor: "grey.50", fontWeight: 600 } }}>
                <TableCell>Table</TableCell><TableCell>Menu group</TableCell><TableCell>Order</TableCell><TableCell>Show in menu</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {menuRows.map((r) => (
                <TableRow key={r.table}>
                  <TableCell><Box sx={{ display: "flex", alignItems: "center", gap: 1 }}><Icon name={tables[r.table]?.icon} />{tables[r.table]?.label}</Box></TableCell>
                  <TableCell sx={{ width: 260 }}>
                    <TextField size="small" fullWidth placeholder="e.g. Studies" value={r.group}
                      onChange={(e) => setMenuGroup(r.table, e.target.value)} slotProps={{
                      htmlInput: { list: "neoapps-menu-groups" }
                    }} />
                  </TableCell>
                  <TableCell>
                    <Button size="small" disabled={!r.group} onClick={() => moveInMenu(r.table, -1)}>↑</Button>
                    <Button size="small" disabled={!r.group} onClick={() => moveInMenu(r.table, 1)}>↓</Button>
                  </TableCell>
                  <TableCell><Switch checked={!r.hidden} onChange={(e) => update((d) => { tcfg(d, r.table).hidden = !e.target.checked; })} /></TableCell>
                </TableRow>
              ))}
            </TableBody>
            <datalist id="neoapps-menu-groups">{Array.from(new Set((draft.menu || []).map((g) => g.group))).map((g) => <option key={g} value={g} />)}</datalist>
          </Table>
        )}

        {tab === 2 && resolved && (
          <Box sx={{ p: 2.5 }}>
            <TextField select size="small" label="Table" value={tableName} onChange={(e) => setTableName(e.target.value)} sx={{ minWidth: 280, mb: 2.5 }}>
              {appTables.map((t) => <MenuItem key={t} value={t}>{tables[t]?.label} ({t})</MenuItem>)}
            </TextField>
            <Box sx={{ display: "grid", gridTemplateColumns: { xs: "1fr", md: "repeat(3, 1fr)" }, gap: 2, mb: 2.5 }}>
              <TextField size="small" label="Label (plural)" value={resolved.label} onChange={(e) => update((d) => { tcfg(d, tableName).label = e.target.value; })} />
              <TextField size="small" label="Singular" value={resolved.singular} onChange={(e) => update((d) => { tcfg(d, tableName).singular = e.target.value; })} />
              <TextField size="small" select label="Icon" value={resolved.icon} onChange={(e) => update((d) => { tcfg(d, tableName).icon = e.target.value; })}>
                {Object.keys(ICONS).map((i) => <MenuItem key={i} value={i}><Box sx={{ display: "flex", gap: 1, alignItems: "center" }}><Icon name={i} />{i}</Box></MenuItem>)}
              </TextField>
              <TextField size="small" select label="Display field" helperText="Shown when other records link here" value={resolved.displayField}
                onChange={(e) => update((d) => { tcfg(d, tableName).displayField = e.target.value; })}>
                {resolved.schema.columns.map((c) => <MenuItem key={c.name} value={c.name}>{resolved.fields[c.name].label}</MenuItem>)}
              </TextField>
              <TextField size="small" select label="Default sort" value={resolved.defaultSort || ""} onChange={(e) => update((d) => { tcfg(d, tableName).defaultSort = e.target.value || undefined; })}>
                <MenuItem value=""><em>Default</em></MenuItem>
                {resolved.schema.columns.flatMap((c) => [`${c.name}|ASC`, `${c.name}|DESC`]).map((s) => (
                  <MenuItem key={s} value={s}>{resolved.fields[s.split("|")[0]].label} {s.endsWith("ASC") ? "↑" : "↓"}</MenuItem>
                ))}
              </TextField>
              <TextField size="small" label="Description" value={resolved.description || ""} onChange={(e) => update((d) => { tcfg(d, tableName).description = e.target.value || undefined; })} />
            </Box>
            <Divider sx={{ mb: 1 }} />
            <Table size="small">
              <TableHead>
                <TableRow sx={{ "& th": { fontWeight: 600 } }}>
                  <TableCell>Column</TableCell><TableCell>Label</TableCell><TableCell>Input type</TableCell><TableCell>Choices (comma-separated)</TableCell>
                  <TableCell align="center">In list</TableCell><TableCell align="center">In form</TableCell><TableCell align="center">Required</TableCell><TableCell>Help text</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {resolved.schema.columns.map((c) => {
                  const f = resolved.fields[c.name];
                  return (
                    <TableRow key={c.name}>
                      <TableCell sx={{ fontFamily: "monospace", fontSize: 12 }}>{c.name}<br /><Typography variant="caption" sx={{
                        color: "text.secondary"
                      }}>{c.columnType}</Typography></TableCell>
                      <TableCell><TextField size="small" value={f.label} onChange={(e) => update((d) => { fcfg(d, tableName, c.name).label = e.target.value; })} /></TableCell>
                      <TableCell>
                        <TextField size="small" select value={f.widget} sx={{ minWidth: 120 }} onChange={(e) => update((d) => { fcfg(d, tableName, c.name).widget = e.target.value as Widget; })}>
                          {WIDGETS.filter((w) => w !== "reference" || c.references).map((w) => <MenuItem key={w} value={w}>{w}</MenuItem>)}
                        </TextField>
                      </TableCell>
                      <TableCell>
                        {f.widget === "select" && (
                          <TextField size="small" placeholder="startup, enrolling, closed" value={(f.options || []).join(", ")}
                            onChange={(e) => update((d) => { fcfg(d, tableName, c.name).options = e.target.value.split(",").map((s) => s.trim()).filter(Boolean); })} />
                        )}
                      </TableCell>
                      <TableCell align="center">
                        <Checkbox size="small" checked={listCols.includes(c.name)} onChange={(e) => update((d) => {
                          const cols = listCols.filter((x) => x !== c.name);
                          tcfg(d, tableName).listColumns = e.target.checked ? [...cols, c.name] : cols;
                        })} />
                      </TableCell>
                      <TableCell align="center">
                        <Checkbox size="small" checked={!f.hideInForm} disabled={c.isAutoIncrement} onChange={(e) => update((d) => { fcfg(d, tableName, c.name).hideInForm = !e.target.checked; })} />
                      </TableCell>
                      <TableCell align="center">
                        <Checkbox size="small" checked={f.required} onChange={(e) => update((d) => { fcfg(d, tableName, c.name).required = e.target.checked; })} />
                      </TableCell>
                      <TableCell><TextField size="small" value={f.help || ""} onChange={(e) => update((d) => { fcfg(d, tableName, c.name).help = e.target.value || undefined; })} /></TableCell>
                    </TableRow>
                  );
                })}
              </TableBody>
            </Table>
          </Box>
        )}
      </Paper>
      <Dialog open={importing} onClose={() => setImporting(false)} fullWidth maxWidth="md">
        <DialogTitle>Import ui.config.json</DialogTitle>
        <DialogContent dividers>
          <TextField multiline minRows={14} fullWidth value={importText} onChange={(e) => setImportText(e.target.value)} placeholder='{ "appName": "...", "tables": { ... } }' sx={{ fontFamily: "monospace" }} />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setImporting(false)}>Cancel</Button>
          <Button variant="contained" onClick={doImport}>Load into editor</Button>
        </DialogActions>
      </Dialog>
      <Snackbar open={saved} autoHideDuration={3000} onClose={() => setSaved(false)}>
        <Alert severity="success" onClose={() => setSaved(false)}>UI saved for everyone in this workspace</Alert>
      </Snackbar>
    </Box>
  );
};
