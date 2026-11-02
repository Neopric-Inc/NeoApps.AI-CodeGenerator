import React, { useCallback, useEffect, useState } from "react";
import { Link as RouterLink, useNavigate, useParams } from "react-router-dom";
import { Alert, Box, Breadcrumbs, Button, CircularProgress, Link, Paper, Tab, Tabs, Typography } from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import EditOutlined from "@mui/icons-material/EditOutlined";
import DeleteOutline from "@mui/icons-material/DeleteOutline";
import { getRecord, listRecords } from "./api";
import { useApp } from "./AppContext";
import { AUDIT_FIELDS } from "./config";
import { DataTable } from "./DataTable";
import { renderValue } from "./format";
import { RecordForm } from "./RecordForm";
import { useDelete } from "./ResourcePage";
import { ResolvedTable } from "./types";
import { useReferences } from "./useReferences";
import AutoAwesome from "@mui/icons-material/AutoAwesome";
import { FilesPanel } from "./ai/FilesPanel";
import { WorkflowButtons } from "./ai/Workflows";

/** Related records of one child table, filtered client-side by the FK pointing at the parent. */
const ChildList: React.FC<{ child: ResolvedTable; fk: string; parentId: any }> = ({ child, fk, parentId }) => {
  const { can } = useApp();
  const navigate = useNavigate();
  const [rows, setRows] = useState<Record<string, any>[]>([]);
  const [loading, setLoading] = useState(true);
  const [adding, setAdding] = useState<Record<string, any> | null | undefined>(undefined);
  const [reload, setReload] = useState(0);
  const { labelFor } = useReferences(child, reload);

  useEffect(() => {
    let alive = true;
    setLoading(true);
    listRecords(child.name, 1, 1000)
      .then((p) => alive && setRows(p.records.filter((r) => String(r[fk]) === String(parentId))))
      .catch(() => alive && setRows([]))
      .finally(() => alive && setLoading(false));
    return () => { alive = false; };
  }, [child.name, fk, parentId, reload]);

  const columns = child.listColumns.filter((c) => c !== fk);
  return (
    <Box>
      {can(child.name, "create") && (
        <Box sx={{ p: 1.5, display: "flex", justifyContent: "flex-end" }}>
          <Button size="small" startIcon={<AddIcon />} onClick={() => setAdding(null)}>Add {child.singular.toLowerCase()}</Button>
        </Box>
      )}
      <DataTable table={child} columns={columns.length ? columns : child.listColumns} rows={rows} total={rows.length} loading={loading}
        page={0} pageSize={rows.length} labelFor={labelFor} dense onOpen={(r) => navigate(`/r/${child.name}/${r[child.primaryKey]}`)} />
      <RecordForm table={child} record={null} defaults={{ [fk]: parentId }} open={adding !== undefined}
        onClose={() => setAdding(undefined)} onSaved={() => { setAdding(undefined); setReload((r) => r + 1); }} />
    </Box>
  );
};

export const RecordPage: React.FC = () => {
  const { table: name = "", id = "" } = useParams();
  const { tables, can, ai } = useApp();
  const navigate = useNavigate();
  const table = tables[name];
  const [record, setRecord] = useState<Record<string, any> | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string>();
  const [editing, setEditing] = useState(false);
  const [tab, setTab] = useState(0);
  const [bump, setBump] = useState(0);
  const { labelFor } = useReferences(table);

  const load = useCallback(async () => {
    if (!table) return;
    setLoading(true);
    try {
      setRecord(await getRecord(table.name, id));
    } catch (e: any) {
      setError(e.message);
    } finally {
      setLoading(false);
    }
  }, [table, id]);

  useEffect(() => { load(); }, [load]);
  const del = useDelete(name, () => navigate(`/r/${name}`));

  if (!table) return <Alert severity="warning">Unknown table "{name}".</Alert>;
  if (loading) return <Box sx={{ display: "grid", placeItems: "center", py: 10 }}><CircularProgress /></Box>;
  if (error || !record) return <Alert severity="error">{error || `${table.singular} not found.`}</Alert>;

  const children = table.children.filter((c) => tables[c] && can(c, "read")).map((c) => ({
    child: tables[c],
    fk: tables[c].schema.columns.find((col) => col.references?.table === table.name)!.name,
  })).filter((x) => x.fk);
  const detailFields = table.schema.columns.map((c) => table.fields[c.name])
    .filter((f) => !f.hidden && !f.column.isPrimaryKey && !AUDIT_FIELDS.includes(f.name));

  return (
    <Box>
      <Breadcrumbs sx={{ mb: 1 }}>
        <Link component={RouterLink} to={`/r/${name}`} underline="hover" color="inherit">{table.label}</Link>
        <Typography color="text.primary">{String(record[table.displayField] ?? `#${id}`)}</Typography>
      </Breadcrumbs>
      <Box sx={{ display: "flex", alignItems: "center", gap: 1, mb: 2.5, flexWrap: "wrap" }}>
        <Typography variant="h5" fontWeight={700} sx={{ flex: 1 }}>{String(record[table.displayField] ?? `${table.singular} #${id}`)}</Typography>
        {ai?.enabled && <WorkflowButtons workflows={ai.workflows.filter((w) => w.table === name)} recordId={id} record={record} onApplied={() => setBump((b) => b + 1)} />}
        {ai?.enabled && <Button variant="outlined" startIcon={<AutoAwesome />} onClick={() => navigate(`/assistant?table=${name}&id=${encodeURIComponent(id)}`)}>Ask AI</Button>}
        {can(name, "update") && <Button variant="outlined" startIcon={<EditOutlined />} onClick={() => setEditing(true)}>Edit</Button>}
        {can(name, "delete") && <Button color="error" startIcon={<DeleteOutline />} onClick={() => del.ask(record)}>Delete</Button>}
      </Box>
      <Paper variant="outlined" sx={{ borderRadius: 2, p: 2.5, mb: 3 }}>
        <Box sx={{ display: "grid", gridTemplateColumns: { xs: "1fr", sm: "1fr 1fr", lg: "1fr 1fr 1fr" }, gap: 2.5 }}>
          {detailFields.map((f) => (
            <Box key={f.name} sx={{ gridColumn: f.widget === "textarea" ? "1 / -1" : undefined }}>
              <Typography variant="caption" color="text.secondary" sx={{ textTransform: "uppercase", letterSpacing: 0.4 }}>{f.label}</Typography>
              <Box sx={{ mt: 0.25, whiteSpace: f.widget === "textarea" ? "pre-wrap" : undefined }}>
                {f.widget === "reference" && record[f.name] != null && can(f.column.references!.table, "read")
                  ? <Link component={RouterLink} to={`/r/${f.column.references!.table}/${record[f.name]}`}>{labelFor(f.name, record[f.name])}</Link>
                  : f.widget === "textarea" ? (record[f.name] || "—") : renderValue(f, record[f.name], labelFor(f.name, record[f.name]))}
              </Box>
            </Box>
          ))}
        </Box>
        <Typography variant="caption" color="text.secondary" sx={{ display: "block", mt: 2.5 }}>
          Created {renderValue(table.fields.createdAt, record.createdAt)} by {record.createdBy || "—"} · Updated {renderValue(table.fields.modifiedAt, record.modifiedAt)}
        </Typography>
      </Paper>
      {ai?.enabled && ai.documentTables.includes(name) && <FilesPanel table={name} recordId={id} />}
      {children.length > 0 && (
        <Paper variant="outlined" sx={{ borderRadius: 2, overflow: "hidden" }}>
          <Tabs value={Math.min(tab, children.length - 1)} onChange={(_, v) => setTab(v)} variant="scrollable" sx={{ borderBottom: 1, borderColor: "divider", px: 1 }}>
            {children.map((c) => <Tab key={c.child.name} label={c.child.label} />)}
          </Tabs>
          {children[Math.min(tab, children.length - 1)] && (
            <ChildList key={children[Math.min(tab, children.length - 1)].child.name + bump} {...children[Math.min(tab, children.length - 1)]} parentId={record[table.primaryKey]} />
          )}
        </Paper>
      )}
      <RecordForm table={table} record={record} open={editing} onClose={() => setEditing(false)} onSaved={() => { setEditing(false); load(); }} />
      {del.dialog}
    </Box>
  );
};
