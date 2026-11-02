import React, { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { Box, Card, CardActionArea, CardContent, Paper, Skeleton, Typography } from "@mui/material";
import { listRecords } from "./api";
import { useApp } from "./AppContext";
import { DataTable } from "./DataTable";
import { Icon } from "./icons";
import { useReferences } from "./useReferences";

export const Dashboard: React.FC = () => {
  const { config, schema, menu, tables, me } = useApp();
  const navigate = useNavigate();
  const readable = menu.flatMap((g) => g.items);
  const cards = (config.dashboard?.cards?.length ? config.dashboard.cards.map((c) => c.table) : readable.slice(0, 8)).filter((t) => readable.includes(t));
  const [counts, setCounts] = useState<Record<string, number | null>>({});
  const recentName = config.dashboard?.recentTable && readable.includes(config.dashboard.recentTable) ? config.dashboard.recentTable : cards[0];
  const recent = recentName ? tables[recentName] : undefined;
  const [recentRows, setRecentRows] = useState<Record<string, any>[]>([]);
  const [loadingRecent, setLoadingRecent] = useState(true);
  const { labelFor } = useReferences(recent);

  useEffect(() => {
    cards.forEach((t) => listRecords(t, 1, 1).then((p) => setCounts((c) => ({ ...c, [t]: p.total }))).catch(() => setCounts((c) => ({ ...c, [t]: null }))));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [cards.join(",")]);

  useEffect(() => {
    if (!recent) return;
    setLoadingRecent(true);
    listRecords(recent.name, 1, 5, "modifiedAt|DESC").then((p) => setRecentRows(p.records)).catch(() => setRecentRows([])).finally(() => setLoadingRecent(false));
  }, [recent]);

  const labelOf = (t: string) => (config.dashboard?.cards || []).find((c) => c.table === t)?.label || tables[t]?.label;

  return (
    <Box>
      <Typography variant="h5" fontWeight={700}>Welcome back{me ? `, ${me.username}` : ""}</Typography>
      <Typography color="text.secondary" sx={{ mb: 3 }}>{config.tagline || `Here's what's happening in ${config.appName || schema.app}.`}</Typography>
      <Box sx={{ display: "grid", gridTemplateColumns: { xs: "1fr 1fr", md: "repeat(4, 1fr)" }, gap: 2, mb: 3 }}>
        {cards.map((t) => (
          <Card key={t} variant="outlined" sx={{ borderRadius: 2 }}>
            <CardActionArea onClick={() => navigate(`/r/${t}`)}>
              <CardContent>
                <Box sx={{ display: "flex", alignItems: "center", gap: 1, color: "text.secondary", mb: 1 }}>
                  <Icon name={tables[t].icon} />
                  <Typography variant="body2" noWrap>{labelOf(t)}</Typography>
                </Box>
                {counts[t] === undefined ? <Skeleton width={48} height={40} /> : <Typography variant="h4" fontWeight={700}>{counts[t] ?? "—"}</Typography>}
              </CardContent>
            </CardActionArea>
          </Card>
        ))}
      </Box>
      {recent && (
        <Paper variant="outlined" sx={{ borderRadius: 2, overflow: "hidden" }}>
          <Typography fontWeight={600} sx={{ px: 2, py: 1.5, borderBottom: 1, borderColor: "divider" }}>Recently updated {recent.label.toLowerCase()}</Typography>
          <DataTable table={recent} columns={recent.listColumns.slice(0, 5)} rows={recentRows} total={recentRows.length} loading={loadingRecent}
            page={0} pageSize={5} labelFor={labelFor} dense onOpen={(r) => navigate(`/r/${recent.name}/${r[recent.primaryKey]}`)} />
        </Paper>
      )}
    </Box>
  );
};
