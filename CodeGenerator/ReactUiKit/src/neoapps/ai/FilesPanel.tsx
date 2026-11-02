import React, { useCallback, useEffect, useRef, useState } from "react";
import { Alert, Box, Button, Chip, CircularProgress, IconButton, List, ListItem, ListItemIcon, ListItemText, Paper, Tooltip, Typography } from "@mui/material";
import UploadFile from "@mui/icons-material/UploadFile";
import DescriptionOutlined from "@mui/icons-material/DescriptionOutlined";
import DeleteOutline from "@mui/icons-material/DeleteOutline";
import { useApp } from "../AppContext";
import { AiFile, deleteFile, listFiles, openFile, uploadFile } from "./aiApi";

const size = (n: number) => (n > 1_000_000 ? `${(n / 1_000_000).toFixed(1)} MB` : `${Math.max(1, Math.round(n / 1000))} KB`);
const STATUS: Record<string, { label: string; color: "success" | "warning" | "error" | "default" }> = {
  indexed: { label: "Searchable", color: "success" },
  indexing: { label: "Indexing", color: "default" },
  no_text: { label: "No text", color: "warning" },
  failed: { label: "Failed", color: "error" },
};

/** Files attached to a record. Uploaded text is indexed so the assistant can search and cite it. */
export const FilesPanel: React.FC<{ table: string; recordId: string; onChange?: () => void }> = ({ table, recordId, onChange }) => {
  const { can } = useApp();
  const [files, setFiles] = useState<AiFile[]>([]);
  const [loading, setLoading] = useState(true);
  const [uploading, setUploading] = useState(false);
  const [error, setError] = useState<string>();
  const input = useRef<HTMLInputElement>(null);
  const canEdit = can(table, "update") || can(table, "create");

  const load = useCallback(() => {
    setLoading(true);
    listFiles(table, recordId).then(setFiles).catch((e) => setError(e.message)).finally(() => setLoading(false));
  }, [table, recordId]);
  useEffect(() => { load(); }, [load]);

  const upload = async (list: FileList | null) => {
    if (!list?.length) return;
    setUploading(true);
    setError(undefined);
    try {
      for (const f of Array.from(list)) {
        const res = await uploadFile(table, recordId, f);
        if (res.status !== "indexed") setError(`${res.file_name}: ${res.error || "couldn't read any text"}`);
      }
      load();
      onChange?.();
    } catch (e: any) {
      setError(e.message);
    } finally {
      setUploading(false);
      if (input.current) input.current.value = "";
    }
  };

  return (
    <Paper variant="outlined" sx={{ borderRadius: 2, p: 2, mb: 3 }}
      onDragOver={(e) => canEdit && e.preventDefault()} onDrop={(e) => { if (!canEdit) return; e.preventDefault(); upload(e.dataTransfer.files); }}>
      <Box sx={{ display: "flex", alignItems: "center", gap: 1, mb: 1 }}>
        <Typography variant="subtitle1" fontWeight={600} sx={{ flex: 1 }}>Files</Typography>
        {canEdit && (
          <>
            <input ref={input} type="file" hidden multiple accept=".pdf,.docx,.txt,.md,.csv,.json" onChange={(e) => upload(e.target.files)} />
            <Button size="small" variant="outlined" startIcon={uploading ? <CircularProgress size={14} /> : <UploadFile />} disabled={uploading} onClick={() => input.current?.click()}>
              {uploading ? "Uploading…" : "Upload"}
            </Button>
          </>
        )}
      </Box>
      {error && <Alert severity="warning" sx={{ mb: 1 }} onClose={() => setError(undefined)}>{error}</Alert>}
      {loading ? <CircularProgress size={20} /> : files.length === 0 ? (
        <Typography variant="body2" color="text.secondary">
          {canEdit ? "Drop a PDF, Word or text file here. The assistant can then answer questions about it and cite pages." : "No files attached."}
        </Typography>
      ) : (
        <List dense disablePadding>
          {files.map((f) => (
            <ListItem key={f.file_id} disableGutters
              secondaryAction={can(table, "update") || can(table, "delete") ? (
                <Tooltip title="Remove file"><IconButton edge="end" size="small" onClick={async () => { await deleteFile(f.file_id).catch((e) => setError(e.message)); load(); onChange?.(); }}>
                  <DeleteOutline fontSize="small" /></IconButton></Tooltip>
              ) : undefined}>
              <ListItemIcon sx={{ minWidth: 34 }}><DescriptionOutlined fontSize="small" /></ListItemIcon>
              <ListItemText
                primary={<Box component="span" sx={{ cursor: "pointer", "&:hover": { textDecoration: "underline" } }} onClick={() => openFile(f.file_id, f.file_name).catch((e) => setError(e.message))}>{f.file_name}</Box>}
                secondary={`${size(f.size_bytes)}${f.pages ? ` · ${f.pages} page${f.pages === 1 ? "" : "s"}` : ""} · ${f.createdBy} · ${new Date(f.createdAt).toLocaleDateString()}`} />
              <Tooltip title={f.error || ""}><Chip size="small" label={STATUS[f.status]?.label || f.status} color={STATUS[f.status]?.color || "default"} variant="outlined" sx={{ mr: 4 }} /></Tooltip>
            </ListItem>
          ))}
        </List>
      )}
    </Paper>
  );
};
