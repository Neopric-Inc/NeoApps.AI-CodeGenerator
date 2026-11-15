import React, { useMemo, useState } from "react";
import {
  Alert, Box, Button, Dialog, DialogActions, DialogContent, DialogTitle, MenuItem, Table, TableBody, TableCell, TableHead,
  TableRow, TextField, Typography,
} from "@mui/material";
import UploadIcon from "@mui/icons-material/FileUploadOutlined";
import { listRecords } from "./api";
import { useApp } from "./AppContext";
import { JobFeed } from "./jobs/JobFeed";
import { approveJob, FINISHED, rejectJob, startJob, useJobFeed } from "./jobs/jobsApi";
import { ResolvedTable } from "./types";

const MAX_ROWS = 5000;
const NOT_WRITABLE = new Set(["createdby", "modifiedby", "createdat", "modifiedat", "isactive", "created_by_user", "modified_by_user"]);

/** RFC 4180-style CSV: quotes, doubled quotes, newlines inside quotes, BOM; comma or semicolon (Excel in many locales). */
export function parseCsv(text: string): string[][] {
  const src = text.replace(/^﻿/, "");
  const firstLine = src.split(/\r?\n/, 1)[0] ?? "";
  const sep = (firstLine.match(/;/g)?.length ?? 0) > (firstLine.match(/,/g)?.length ?? 0) ? ";" : ",";
  const rows: string[][] = [];
  let row: string[] = [], cell = "", quoted = false;
  for (let i = 0; i < src.length; i++) {
    const ch = src[i];
    if (quoted) {
      if (ch === '"' && src[i + 1] === '"') { cell += '"'; i++; }
      else if (ch === '"') quoted = false;
      else cell += ch;
    } else if (ch === '"') quoted = true;
    else if (ch === sep) { row.push(cell); cell = ""; }
    else if (ch === "\n" || ch === "\r") {
      if (ch === "\r" && src[i + 1] === "\n") i++;
      row.push(cell); cell = "";
      if (row.some((c) => c.trim() !== "")) rows.push(row);
      row = [];
    } else cell += ch;
  }
  row.push(cell);
  if (row.some((c) => c.trim() !== "")) rows.push(row);
  return rows;
}

const norm = (s: string) => s.toLowerCase().replace(/[^a-z0-9]/g, "");

/**
 * Import button + dialog for a list page: pick a CSV, match its columns to fields, then the server checks every row with
 * the app's own rules (nothing saved), shows problems per row, and imports all rows in one go once you approve.
 */
export const ImportButton: React.FC<{ table: ResolvedTable; onDone: () => void }> = ({ table, onDone }) => {
  const [open, setOpen] = useState(false);
  return (
    <>
      <Button variant="outlined" startIcon={<UploadIcon />} onClick={() => setOpen(true)}>Import</Button>
      {open && <ImportDialog table={table} onClose={(changed) => { setOpen(false); if (changed) onDone(); }} />}
    </>
  );
};

const ImportDialog: React.FC<{ table: ResolvedTable; onClose: (changed: boolean) => void }> = ({ table, onClose }) => {
  const { tables } = useApp();
  const fields = useMemo(() => table.schema.columns
    .filter((c) => !(c.isPrimaryKey && c.isAutoIncrement) && !NOT_WRITABLE.has(c.name.toLowerCase()) && !table.fields[c.name]?.hidden)
    .map((c) => ({ name: c.name, label: table.fields[c.name]?.label || c.name, column: c })), [table]);
  const [fileName, setFileName] = useState("");
  const [header, setHeader] = useState<string[]>([]);
  const [data, setData] = useState<string[][]>([]);
  const [mapping, setMapping] = useState<string[]>([]); // CSV column index -> field name ("" = ignore)
  const [key, setKey] = useState("");
  const [error, setError] = useState<string>();
  const [busy, setBusy] = useState(false);
  const [jobId, setJobId] = useState<string>();
  const { job, events, error: feedError } = useJobFeed(jobId);

  const readFile = async (file: File) => {
    setError(undefined);
    const rows = parseCsv(await file.text());
    if (rows.length < 2) return setError("The file needs a header row and at least one data row.");
    if (rows.length - 1 > MAX_ROWS) return setError(`Import up to ${MAX_ROWS} rows at a time; this file has ${rows.length - 1}.`);
    const [head, ...body] = rows;
    setFileName(file.name);
    setHeader(head);
    setData(body);
    setMapping(head.map((h) => {
      const n = norm(h);
      if (n === "id") return "";
      return fields.find((f) => norm(f.label) === n || norm(f.name) === n)?.name ?? "";
    }));
  };

  /** Rows as {field: text}; references given as a label (as Export writes them) are turned into ids here. */
  const buildRows = async () => {
    const used = mapping.map((m, i) => ({ field: m, i })).filter((m) => m.field);
    const refs: Record<string, Map<string, any>> = {};
    for (const { field } of used) {
      const ref = fields.find((f) => f.name === field)?.column.references;
      if (!ref || refs[field]) continue;
      const target = tables[ref.table];
      const res = await listRecords(ref.table, 1, 1000);
      const byLabel = new Map<string, any>();
      for (const r of res.records) {
        const id = r[ref.column];
        byLabel.set(String(id), id);
        if (target) byLabel.set(String(r[target.displayField] ?? "").trim().toLowerCase(), id);
      }
      refs[field] = byLabel;
    }
    const problems: string[] = [];
    const rows = data.map((r, n) => {
      const o: Record<string, string> = {};
      for (const { field, i } of used) {
        let v = (r[i] ?? "").trim();
        if (v && refs[field]) {
          const id = refs[field].get(v) ?? refs[field].get(v.toLowerCase());
          if (id === undefined) problems.push(`Row ${n + 2}: no ${fields.find((f) => f.name === field)?.label} called "${v}".`);
          else v = String(id);
        }
        o[field] = v;
      }
      return o;
    });
    return { rows, problems };
  };

  const check = async () => {
    setBusy(true);
    setError(undefined);
    try {
      const { rows, problems } = await buildRows();
      if (problems.length) { setError(problems.slice(0, 8).join(" ") + (problems.length > 8 ? ` …and ${problems.length - 8} more.` : "")); return; }
      const j = await startJob("data.import", { table: table.name, rows, ...(key ? { key } : {}) });
      setJobId(j.job_id);
    } catch (e: any) {
      setError(e.message);
    } finally {
      setBusy(false);
    }
  };

  const result: any = job?.result;
  const done = job?.status === "completed" && (result?.imported ?? 0) > 0;
  const mapped = mapping.filter(Boolean).length;
  const dup = mapping.filter(Boolean).find((m, i, a) => a.indexOf(m) !== i);

  return (
    <Dialog open fullWidth maxWidth="md" onClose={() => onClose(done)}>
      <DialogTitle>Import {table.label.toLowerCase()}</DialogTitle>
      <DialogContent dividers>
        {!jobId && (
          <>
            <Button component="label" variant="outlined" startIcon={<UploadIcon />}>
              {fileName ? "Choose another file" : "Choose a CSV file"}
              <input hidden type="file" accept=".csv,text/csv" onChange={(e) => e.target.files?.[0] && readFile(e.target.files[0])} />
            </Button>
            {fileName && <Typography variant="body2" sx={{ mt: 1, color: "text.secondary" }}>{fileName}: {data.length} rows. Match each column to a field; leave a column unmatched to ignore it.</Typography>}
            {header.length > 0 && (
              <Table size="small" sx={{ mt: 2 }}>
                <TableHead><TableRow><TableCell>Column in file</TableCell><TableCell>Field</TableCell><TableCell>First row</TableCell></TableRow></TableHead>
                <TableBody>
                  {header.map((h, i) => (
                    <TableRow key={i}>
                      <TableCell>{h}</TableCell>
                      <TableCell>
                        <TextField select size="small" value={mapping[i] ?? ""} sx={{ minWidth: 200 }} slotProps={{ select: { displayEmpty: true } }}
                          onChange={(e) => setMapping((m) => m.map((x, j) => (j === i ? e.target.value : x)))}>
                          <MenuItem value=""><em>Ignore</em></MenuItem>
                          {fields.map((f) => <MenuItem key={f.name} value={f.name}>{f.label}</MenuItem>)}
                        </TextField>
                      </TableCell>
                      <TableCell sx={{ color: "text.secondary", maxWidth: 220, overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>{data[0]?.[i]}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            )}
            {header.length > 0 && (
              <TextField select size="small" label="Skip rows that already exist, matched by" value={key} onChange={(e) => setKey(e.target.value)} sx={{ mt: 2, minWidth: 320 }} slotProps={{ select: { displayEmpty: true }, inputLabel: { shrink: true } }}>
                <MenuItem value=""><em>Don't check (import every row)</em></MenuItem>
                {mapping.filter(Boolean).map((m) => <MenuItem key={m} value={m}>{fields.find((f) => f.name === m)?.label}</MenuItem>)}
              </TextField>
            )}
            {dup && <Alert severity="warning" sx={{ mt: 2 }}>Two columns are matched to {fields.find((f) => f.name === dup)?.label}. Match each field once.</Alert>}
          </>
        )}
        {jobId && (
          <>
            <JobFeed job={job} events={events} error={feedError} />
            {Array.isArray(result?.errors) && result.errors.length > 0 && (
              <Box sx={{ mt: 2 }}>
                <Typography variant="subtitle2">Rows to fix ({result.errorCount})</Typography>
                <Table size="small">
                  <TableHead><TableRow><TableCell>Row</TableCell><TableCell>Problem</TableCell></TableRow></TableHead>
                  <TableBody>
                    {result.errors.slice(0, 50).map((e: any) => (
                      <TableRow key={e.row}><TableCell>{e.row}</TableCell><TableCell>{String(e.message).replace(/'([A-Za-z0-9_]+)'/g, (m: string, n: string) => (table.fields[n] ? `"${table.fields[n].label}"` : m))}</TableCell></TableRow>
                    ))}
                  </TableBody>
                </Table>
              </Box>
            )}
          </>
        )}
        {error && <Alert severity="error" sx={{ mt: 2 }}>{error}</Alert>}
      </DialogContent>
      <DialogActions>
        {!jobId && <Button onClick={() => onClose(false)}>Cancel</Button>}
        {!jobId && <Button variant="contained" disabled={!mapped || !!dup || busy} onClick={check}>{busy ? "Checking…" : `Check ${data.length} rows`}</Button>}
        {job?.status === "awaiting_approval" && (
          <>
            <Button onClick={async () => { await rejectJob(job.job_id, "Cancelled"); onClose(false); }}>Cancel import</Button>
            <Button variant="contained" onClick={() => approveJob(job.job_id, {}).catch((e) => setError(e.message))}>Import</Button>
          </>
        )}
        {job && FINISHED.includes(job.status) && <Button variant="contained" onClick={() => onClose(done)}>Close</Button>}
      </DialogActions>
    </Dialog>
  );
};
