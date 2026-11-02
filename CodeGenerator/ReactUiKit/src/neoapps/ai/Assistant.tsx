import React, { useCallback, useEffect, useRef, useState } from "react";
import { useSearchParams } from "react-router-dom";
import {
  Alert, Box, Button, Chip, CircularProgress, Divider, IconButton, List, ListItemButton, ListItemText, Paper, TextField, Tooltip, Typography,
} from "@mui/material";
import SendIcon from "@mui/icons-material/Send";
import AddIcon from "@mui/icons-material/Add";
import DeleteOutline from "@mui/icons-material/DeleteOutline";
import AutoAwesome from "@mui/icons-material/AutoAwesome";
import { useApp } from "../AppContext";
import { AiMessage, ask, conversationMessages, conversations, deleteConversation } from "./aiApi";
import { Answer } from "./Answer";
import { WorkflowButtons } from "./Workflows";

const TOOL_LABELS: Record<string, string> = {
  search_documents: "Searched documents",
  query_records: "Read records",
  aggregate_records: "Counted records",
  list_tables: "Looked at tables",
  search_clinical_trials: "Searched ClinicalTrials.gov",
};

type Conv = { conversation_id: number; title: string; modifiedAt: string };

export const Assistant: React.FC = () => {
  const { ai, tables, can } = useApp();
  const [params, setParams] = useSearchParams();
  const ctxTable = params.get("table") || undefined;
  const ctxRecord = params.get("id") || undefined;
  const [convs, setConvs] = useState<Conv[]>([]);
  const [current, setCurrent] = useState<number | undefined>();
  const [messages, setMessages] = useState<AiMessage[]>([]);
  const [input, setInput] = useState(params.get("q") || "");
  const [busy, setBusy] = useState(false);
  const bottom = useRef<HTMLDivElement>(null);

  const refresh = useCallback(() => conversations().then(setConvs).catch(() => setConvs([])), []);
  useEffect(() => { refresh(); }, [refresh]);
  useEffect(() => { bottom.current?.scrollIntoView({ behavior: "smooth" }); }, [messages]);

  const open = async (id?: number) => {
    setCurrent(id);
    setMessages(id ? await conversationMessages(id).catch(() => []) : []);
  };

  const send = async (q: string) => {
    q = q.trim();
    if (!q || busy) return;
    setInput("");
    setBusy(true);
    setMessages((m) => [...m, { role: "user", content: q }, { role: "assistant", content: "", pending: true }]);
    try {
      const res = await ask(q, current, current ? undefined : ctxTable, current ? undefined : ctxRecord);
      setCurrent(res.conversation_id);
      setMessages((m) => [...m.slice(0, -1), { role: "assistant", content: res.answer, citations: res.citations, tools: res.tools }]);
      refresh();
    } catch (e: any) {
      setMessages((m) => [...m.slice(0, -1), { role: "assistant", content: e.message, error: true }]);
    } finally {
      setBusy(false);
    }
  };

  if (!ai) return <Box sx={{ display: "grid", placeItems: "center", py: 10 }}><CircularProgress /></Box>;
  if (!ai.enabled)
    return (
      <Alert severity="info">
        The assistant isn't set up yet. {ai.configured ? "It's turned off in the app's AI settings." : "Set the ANTHROPIC_API_KEY environment variable for the API and restart it."}
      </Alert>
    );

  const ctxLabel = ctxTable && tables[ctxTable] ? `${tables[ctxTable].singular} #${ctxRecord}` : undefined;
  const globalWorkflows = ai.workflows.filter((w) => !w.table && w.targets.every((t) => can(t, "create")));

  return (
    <Box sx={{ display: "flex", gap: 2, height: { md: "calc(100vh - 112px)" }, flexDirection: { xs: "column", md: "row" } }}>
      <Paper variant="outlined" sx={{ width: { md: 260 }, flexShrink: 0, borderRadius: 2, display: "flex", flexDirection: "column", overflow: "hidden" }}>
        <Box sx={{ p: 1.5 }}>
          <Button fullWidth variant="contained" startIcon={<AddIcon />} onClick={() => { open(undefined); setParams({}); }}>New conversation</Button>
        </Box>
        <Divider />
        <List dense sx={{ flex: 1, overflowY: "auto", maxHeight: { xs: 200, md: "none" } }}>
          {convs.length === 0 && <Typography variant="body2" color="text.secondary" sx={{ p: 2 }}>No conversations yet.</Typography>}
          {convs.map((c) => (
            <ListItemButton key={c.conversation_id} selected={c.conversation_id === current} onClick={() => open(c.conversation_id)}
              sx={{ "& .del": { visibility: "hidden" }, "&:hover .del": { visibility: "visible" } }}>
              <ListItemText primary={c.title} primaryTypographyProps={{ noWrap: true, fontSize: 14 }}
                secondary={new Date(c.modifiedAt).toLocaleDateString()} />
              <IconButton className="del" size="small" onClick={async (e) => {
                e.stopPropagation();
                await deleteConversation(c.conversation_id).catch(() => undefined);
                if (c.conversation_id === current) open(undefined);
                refresh();
              }}><DeleteOutline fontSize="small" /></IconButton>
            </ListItemButton>
          ))}
        </List>
        {globalWorkflows.length > 0 && (
          <>
            <Divider />
            <Box sx={{ p: 1.5 }}>
              <Typography variant="caption" color="text.secondary" sx={{ textTransform: "uppercase", letterSpacing: 0.5 }}>Workflows</Typography>
              <WorkflowButtons workflows={globalWorkflows} vertical />
            </Box>
          </>
        )}
      </Paper>

      <Paper variant="outlined" sx={{ flex: 1, borderRadius: 2, display: "flex", flexDirection: "column", minHeight: 480, overflow: "hidden" }}>
        <Box sx={{ flex: 1, overflowY: "auto", p: { xs: 2, md: 3 } }}>
          {messages.length === 0 && (
            <Box sx={{ maxWidth: 640, mx: "auto", textAlign: "center", pt: { md: 6 } }}>
              <AutoAwesome color="primary" sx={{ fontSize: 40 }} />
              <Typography variant="h5" fontWeight={700} sx={{ mt: 1 }}>{ai.assistantName}</Typography>
              <Typography color="text.secondary" sx={{ mt: 0.5, mb: 3 }}>
                Ask about your documents and records. Answers cite the source passages{ctxLabel ? `; this conversation starts from ${ctxLabel}` : ""}.
              </Typography>
              <Box sx={{ display: "flex", flexWrap: "wrap", gap: 1, justifyContent: "center" }}>
                {ai.suggestions.map((s) => <Chip key={s} label={s} variant="outlined" onClick={() => send(s)} sx={{ height: "auto", py: 0.75, "& .MuiChip-label": { whiteSpace: "normal" } }} />)}
              </Box>
            </Box>
          )}
          {messages.map((m, i) => (
            <Box key={i} sx={{ display: "flex", justifyContent: m.role === "user" ? "flex-end" : "flex-start", mb: 2 }}>
              {m.role === "user" ? (
                <Box sx={{ bgcolor: "primary.main", color: "primary.contrastText", px: 2, py: 1.25, borderRadius: 2, maxWidth: "75%", whiteSpace: "pre-wrap" }}>
                  <Typography variant="body2">{m.content}</Typography>
                </Box>
              ) : (
                <Box sx={{ maxWidth: "85%", bgcolor: m.error ? "#fff5f5" : "grey.50", border: 1, borderColor: m.error ? "error.light" : "divider", px: 2, py: 1.25, borderRadius: 2 }}>
                  {m.pending ? (
                    <Box sx={{ display: "flex", alignItems: "center", gap: 1, color: "text.secondary" }}>
                      <CircularProgress size={16} /> <Typography variant="body2">Searching documents and records…</Typography>
                    </Box>
                  ) : m.error ? <Typography variant="body2" color="error">{m.content}</Typography> : (
                    <>
                      <Answer text={m.content} citations={m.citations} />
                      {(m.tools?.length || 0) > 0 && (
                        <Box sx={{ display: "flex", gap: 0.5, flexWrap: "wrap", mt: 1 }}>
                          {m.tools!.map((t) => <Chip key={t} size="small" label={TOOL_LABELS[t] || t} sx={{ height: 20, fontSize: 11 }} />)}
                          {(m.citations?.length || 0) > 0 && <Chip size="small" color="primary" variant="outlined" label={`${m.citations!.length} source${m.citations!.length > 1 ? "s" : ""}`} sx={{ height: 20, fontSize: 11 }} />}
                        </Box>
                      )}
                    </>
                  )}
                </Box>
              )}
            </Box>
          ))}
          <div ref={bottom} />
        </Box>
        <Divider />
        <Box component="form" onSubmit={(e) => { e.preventDefault(); send(input); }} sx={{ p: 1.5, display: "flex", gap: 1, alignItems: "flex-end" }}>
          {ctxLabel && !current && <Tooltip title="The assistant will focus on this record and its documents"><Chip size="small" label={ctxLabel} onDelete={() => setParams({})} sx={{ mb: 1 }} /></Tooltip>}
          <TextField fullWidth multiline maxRows={6} size="small" placeholder="Ask a question…" value={input} onChange={(e) => setInput(e.target.value)}
            onKeyDown={(e) => { if (e.key === "Enter" && !e.shiftKey) { e.preventDefault(); send(input); } }} disabled={busy} />
          <Button type="submit" variant="contained" disabled={busy || !input.trim()} sx={{ minWidth: 0, px: 1.5, py: 1 }}><SendIcon fontSize="small" /></Button>
        </Box>
      </Paper>
    </Box>
  );
};
