import React, { useCallback, useEffect, useState } from "react";
import { Link as RouterLink, useNavigate, useParams } from "react-router-dom";
import { Alert, Box, Button, CircularProgress, Link, Paper, Tab, Tabs, Typography } from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import EditOutlined from "@mui/icons-material/EditOutlined";
import DeleteOutline from "@mui/icons-material/DeleteOutlineOutlined";
import { getRecord, queryRecords } from "./api";
import { Cond, defaultSortOf } from "./listView";
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
import { extensions } from "./customExtensions";
import { ShareDialog } from "./ShareDialog";
import ShareOutlined from "@mui/icons-material/ShareOutlined";

import { t, lower, tn, tx } from "./i18n";
/** Related records of one child table: the child list filtered to this parent on the server, with paging. */
const ChildList: React.FC<{ child: ResolvedTable; fk: string; parentId: any }> = ({ child, fk, parentId }) => {
  const { can } = useApp();
  const navigate = useNavigate();
  const [rows, setRows] = useState<Record<string, any>[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(10);
  const [loading, setLoading] = useState(true);
  const [adding, setAdding] = useState<Record<string, any> | null | undefined>(undefined);
  const [reload, setReload] = useState(0);
  const { labelFor } = useReferences(child, reload);

  useEffect(() => {
    let alive = true;
    setLoading(true);
    queryRecords(child.name, page + 1, pageSize, defaultSortOf(child), undefined, [{ columnName: fk, columnCondition: Cond.Equals, columnValue: String(parentId) }])
      .then((p) => { if (alive) { setRows(p.records); setTotal(p.total); } })
      .catch(() => alive && setRows([]))
      .finally(() => alive && setLoading(false));
    return () => { alive = false; };
  }, [child, fk, parentId, reload, page, pageSize]);

  const columns = child.listColumns.filter((c) => c !== fk);
  const listLink = `/r/${child.name}?f.${fk}=${encodeURIComponent(String(parentId))}`;
  return (
    <Box>
      <Box sx={{ p: 1.5, display: "flex", alignItems: "center", gap: 1 }}>
        <Link component={RouterLink} to={listLink} underline="hover" sx={{ fontSize: 14 }}>{t("Open in {items} list", { items: lower(child.label) })}</Link>
        <Box sx={{ flex: 1 }} />
        {can(child.name, "create") && <Button size="small" startIcon={<AddIcon />} onClick={() => setAdding(null)}>{t("Add {item}", { item: lower(child.singular) })}</Button>}
      </Box>
      <DataTable table={child} columns={columns.length ? columns : child.listColumns} rows={rows} total={total} loading={loading}
        page={page} pageSize={pageSize} labelFor={labelFor} dense onOpen={(r) => navigate(`/r/${child.name}/${r[child.primaryKey]}`)}
        onPage={total > 10 ? (p, size) => { setPage(p); setPageSize(size); } : undefined} />
      <RecordForm table={child} record={null} defaults={{ [fk]: parentId }} open={adding !== undefined}
        onClose={() => setAdding(undefined)} onSaved={() => { setAdding(undefined); setReload((r) => r + 1); }} />
    </Box>
  );
};

export const RecordPage: React.FC = () => {
  const { table: name = "", id = "" } = useParams();
  const { tables, can, canOpen, canChangeRecord, ai, config } = useApp();
  const navigate = useNavigate();
  const table = tables[name];
  const [record, setRecord] = useState<Record<string, any> | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string>();
  const [editing, setEditing] = useState(false);
  const [sharing, setSharing] = useState(false);
  const [tab, setTab] = useState(0);
  const [bump, setBump] = useState(0);
  const { labelFor } = useReferences(table);

  const load = useCallback(async () => {
    if (!table || !canOpen(table.name)) return;
    setLoading(true);
    try {
      setRecord(await getRecord(table.name, id));
    } catch (e: any) {
      setError(e.message);
    } finally {
      setLoading(false);
    }
  }, [table, id, canOpen]);

  useEffect(() => { load(); }, [load]);
  const del = useDelete(name, () => navigate(`/r/${name}`));

  // the app's tables arrive just after sign-in: show nothing until then instead of "Unknown table"
  if (!table) return Object.keys(tables).length === 0 ? null : <Alert severity="warning">{t("Unknown table \"{name}\".", { name })}</Alert>;
  if (!canOpen(name)) return <Alert severity="info">{t("This page isn't available for your role.")}</Alert>;
  if (loading) return <Box sx={{ display: "grid", placeItems: "center", py: 10 }}><CircularProgress /></Box>;
  if (error || !record) return <Alert severity="error">{tx(error) || t("{item} not found.", { item: table.singular })}</Alert>;

  const children = table.children.filter((c) => tables[c] && can(c, "read")).map((c) => ({
    child: tables[c],
    fk: tables[c].schema.columns.find((col) => col.references?.table === table.name)!.name,
  })).filter((x) => x.fk);
  // fields hidden from this role are left out of table.fields (AppContext), so skip the gaps
  const detailFields = table.schema.columns.map((c) => table.fields[c.name])
    .filter((f) => f && !f.hidden && !f.column.isPrimaryKey && !AUDIT_FIELDS.includes(f.name));

  return (
    <Box>
      <Box sx={{ display: "flex", alignItems: "center", gap: 1, mb: 2.5, flexWrap: "wrap" }}>
        <Typography
          variant="h5"
          sx={{
            fontWeight: 700,
            flex: 1
          }}>{String(record[table.displayField] ?? `${table.singular} #${id}`)}</Typography>
        {ai?.enabled && <WorkflowButtons workflows={ai.workflows.filter((w) => w.table === name)} recordId={id} record={record} onApplied={() => setBump((b) => b + 1)} />}
        {ai?.enabled && <Button variant="outlined" startIcon={<AutoAwesome />} onClick={() => navigate(`/assistant?table=${name}&id=${encodeURIComponent(id)}`)}>{t("Ask AI")}</Button>}
        {extensions.recordActions && record && React.createElement(extensions.recordActions, { table: name, record, onChanged: () => setBump((b) => b + 1) })}
        {canChangeRecord(name, record, "update") && (config as any).sharing !== false && <Button variant="outlined" startIcon={<ShareOutlined />} onClick={() => setSharing(true)}>{t("Share")}</Button>}
        {canChangeRecord(name, record, "update") && <Button variant="outlined" startIcon={<EditOutlined />} onClick={() => setEditing(true)}>{t("Edit")}</Button>}
        {canChangeRecord(name, record, "delete") && <Button color="error" startIcon={<DeleteOutline />} onClick={() => del.ask(record)}>{t("Delete")}</Button>}
      </Box>
      <Paper variant="outlined" sx={{ borderRadius: 2, p: 2.5, mb: 3 }}>
        <Box sx={{ display: "grid", gridTemplateColumns: { xs: "1fr", sm: "1fr 1fr", lg: "1fr 1fr 1fr" }, gap: 2.5 }}>
          {detailFields.map((f) => (
            <Box key={f.name} sx={{ gridColumn: f.widget === "textarea" ? "1 / -1" : undefined }}>
              <Typography
                variant="caption"
                sx={{
                  color: "text.secondary",
                  textTransform: "uppercase",
                  letterSpacing: 0.4
                }}>{f.label}</Typography>
              <Box sx={{ mt: 0.25, whiteSpace: f.widget === "textarea" ? "pre-wrap" : undefined }}>
                {f.widget === "reference" && record[f.name] != null && can(f.column.references!.table, "read")
                  ? <Link component={RouterLink} to={`/r/${f.column.references!.table}/${record[f.name]}`}>{labelFor(f.name, record[f.name])}</Link>
                  : f.widget === "textarea" ? (record[f.name] || "—") : renderValue(f, record[f.name], labelFor(f.name, record[f.name]))}
              </Box>
            </Box>
          ))}
        </Box>
        <Typography
          variant="caption"
          sx={{
            color: "text.secondary",
            display: "block",
            mt: 2.5
          }}>
          {tn("Created {when} by {who}", { when: renderValue(table.fields.createdAt, record.createdAt), who: record.created_by_user || record.createdBy || "—" })}{" · "}{record.modified_by_user ? tn("Updated {when} by {who}", { when: renderValue(table.fields.modifiedAt, record.modifiedAt), who: record.modified_by_user }) : tn("Updated {when}", { when: renderValue(table.fields.modifiedAt, record.modifiedAt) })}
        </Typography>
      </Paper>
      {(ai?.enabled || ai?.files) && ai.documentTables.includes(name) && <FilesPanel table={name} recordId={id} />}
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
      <ShareDialog table={table} recordId={id} open={sharing} onClose={() => setSharing(false)} />
      <RecordForm table={table} record={record} open={editing} onClose={() => setEditing(false)} onSaved={() => { setEditing(false); load(); }} />
      {del.dialog}
    </Box>
  );
};
