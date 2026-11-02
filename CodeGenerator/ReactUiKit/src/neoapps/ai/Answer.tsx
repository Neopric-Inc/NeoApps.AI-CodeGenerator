import React, { useState } from "react";
import { Link as RouterLink } from "react-router-dom";
import { Box, Button, Chip, Link, Popover, Typography } from "@mui/material";
import DescriptionOutlined from "@mui/icons-material/DescriptionOutlined";
import { Citation, openFile } from "./aiApi";
import { useApp } from "../AppContext";

/** Inline formatting: **bold**, `code`, and [S1] citation chips. Rendered as React nodes (no HTML injection). */
function inline(text: string, cite: (id: string, key: string) => React.ReactNode): React.ReactNode[] {
  const out: React.ReactNode[] = [];
  const re = /(\*\*[^*]+\*\*|`[^`]+`|\[S\d+\](?:\s*\[S\d+\])*)/g;
  let last = 0;
  let m: RegExpExecArray | null;
  let k = 0;
  while ((m = re.exec(text))) {
    if (m.index > last) out.push(text.slice(last, m.index));
    const t = m[0];
    if (t.startsWith("**")) out.push(<b key={k++}>{t.slice(2, -2)}</b>);
    else if (t.startsWith("`")) out.push(<Box key={k++} component="code" sx={{ bgcolor: "grey.100", px: 0.5, borderRadius: 0.5, fontSize: "0.9em" }}>{t.slice(1, -1)}</Box>);
    else (t.match(/S\d+/g) || []).forEach((id) => out.push(cite(id, `c${k++}`)));
    last = m.index + t.length;
  }
  if (last < text.length) out.push(text.slice(last));
  return out;
}

const CitationChip: React.FC<{ c?: Citation; id: string }> = ({ c, id }) => {
  const { tables, can } = useApp();
  const [anchor, setAnchor] = useState<HTMLElement | null>(null);
  if (!c) return <Chip size="small" label={id} sx={{ height: 18, fontSize: 11, mx: 0.25 }} />;
  const table = tables[c.table];
  return (
    <>
      <Chip size="small" color="primary" variant="outlined" label={id} onClick={(e) => setAnchor(e.currentTarget)}
        sx={{ height: 18, fontSize: 11, mx: 0.25, cursor: "pointer", verticalAlign: "baseline" }} />
      <Popover open={!!anchor} anchorEl={anchor} onClose={() => setAnchor(null)} anchorOrigin={{ vertical: "bottom", horizontal: "left" }}>
        <Box sx={{ p: 2, maxWidth: 460 }}>
          <Typography variant="subtitle2">{c.file_name} · page {c.page}</Typography>
          {table && can(c.table, "read") && (
            <Typography variant="caption" color="text.secondary">
              {table.singular}: <Link component={RouterLink} to={`/r/${c.table}/${c.record_id}`}>{c.record_label}</Link>
            </Typography>
          )}
          <Typography variant="body2" sx={{ mt: 1, whiteSpace: "pre-wrap", maxHeight: 220, overflow: "auto", bgcolor: "grey.50", p: 1, borderRadius: 1, fontSize: 13 }}>
            {c.excerpt}
          </Typography>
          <Button size="small" startIcon={<DescriptionOutlined />} sx={{ mt: 1 }} onClick={() => openFile(c.file_id, c.file_name).catch(() => undefined)}>
            Open document
          </Button>
        </Box>
      </Popover>
    </>
  );
};

/** Renders the assistant's markdown-ish answer: paragraphs, headings, bullet and numbered lists, bold, code, citations. */
export const Answer: React.FC<{ text: string; citations?: Citation[] }> = ({ text, citations = [] }) => {
  const byId = Object.fromEntries(citations.map((c) => [c.id, c]));
  const cite = (id: string, key: string) => <CitationChip key={key} id={id} c={byId[id]} />;
  const blocks: React.ReactNode[] = [];
  let list: { ordered: boolean; items: string[] } | null = null;
  const flush = () => {
    if (!list) return;
    const L = list;
    blocks.push(
      <Box key={blocks.length} component={L.ordered ? "ol" : "ul"} sx={{ my: 0.5, pl: 3 }}>
        {L.items.map((it, i) => <li key={i}><Typography variant="body2" component="span">{inline(it, cite)}</Typography></li>)}
      </Box>
    );
    list = null;
  };
  for (const raw of text.split("\n")) {
    const line = raw.trimEnd();
    const bullet = line.match(/^\s*[-*•]\s+(.*)$/);
    const num = line.match(/^\s*\d+[.)]\s+(.*)$/);
    if (bullet || num) {
      const ordered = !!num;
      if (!list || list.ordered !== ordered) {
        flush();
        list = { ordered, items: [] };
      }
      list.items.push((bullet || num)![1]);
      continue;
    }
    flush();
    if (!line.trim()) continue;
    const h = line.match(/^#{1,4}\s+(.*)$/);
    if (h) blocks.push(<Typography key={blocks.length} variant="subtitle2" sx={{ mt: 1.5, mb: 0.5 }}>{inline(h[1], cite)}</Typography>);
    else if (/^\|.*\|$/.test(line)) {
      if (/^\|[\s:|-]+\|$/.test(line)) continue;
      blocks.push(
        <Box key={blocks.length} sx={{ display: "flex", borderBottom: 1, borderColor: "divider", fontSize: 13 }}>
          {line.slice(1, -1).split("|").map((cell, i) => <Box key={i} sx={{ flex: 1, py: 0.5, pr: 1 }}>{inline(cell.trim(), cite)}</Box>)}
        </Box>
      );
    } else blocks.push(<Typography key={blocks.length} variant="body2" sx={{ my: 0.75 }}>{inline(line, cite)}</Typography>);
  }
  flush();
  return <Box sx={{ "& li": { mb: 0.25 } }}>{blocks}</Box>;
};
