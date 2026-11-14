import React, { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { Box, Card, CardActionArea, CardContent, Paper, Skeleton, Typography } from "@mui/material";
import { api, listRecords, type Page } from "./api";
import type { DashboardCard } from "./types";
import { formatMoney } from "./format";
import { useApp } from "./AppContext";
import { DataTable } from "./DataTable";
import { Icon } from "./icons";
import { useReferences } from "./useReferences";
import { extensions } from "../custom";

async function loadCard(card: DashboardCard): Promise<number> {
  const now = new Date();
  const monthStart = (offset: number) =>
    new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth() + offset, 1)).toISOString().slice(0, 10);
  const where = Object.entries(card.where || {}).flatMap(([columnName, value]) => {
    const filter = (columnCondition: number, columnValue: string) => ({ columnName, columnCondition, columnValue });
    if (typeof value === "object" && value !== null) {
      if ("notEqual" in value) return [filter(8, String(value.notEqual))];
      if (value.period === "month") return [filter(6, monthStart(0)), filter(4, monthStart(1))];
      throw new Error("Unsupported dashboard filter");
    }
    return [filter(5, String(value))];
  });
  const size = card.sum ? 200 : 1;
  const load = async (page: number): Promise<Page> => {
    if (!where.length) return listRecords(card.table, page, size);
    const response = await api("POST", `${card.table}/filter?page=${page}&itemsPerPage=${size}`, where);
    return { records: response?.document?.records || [], total: response?.code === 1 ? response.document?.totalRecords || 0 : 0 };
  };
  const first = await load(1);
  if (!card.sum) return first.total;
  const sumColumn = card.sum;
  const sum = (data: Page) => data.records.reduce((total, row) => total + (Number(row[sumColumn]) || 0), 0);
  let total = sum(first);
  for (let page = 2; page <= Math.ceil(first.total / size); page++) total += sum(await load(page));
  return total;
}

export const Dashboard: React.FC = () => {
  const { config, schema, menu, tables, me } = useApp();
  const navigate = useNavigate();
  const cards = useMemo(() => {
    const readable = menu.flatMap((group) => group.items);
    const configured: DashboardCard[] = config.dashboard?.cards?.length
      ? config.dashboard.cards
      : readable.slice(0, 8).map((table) => ({ table }));
    return configured.filter(({ table }) => readable.includes(table));
  }, [config.dashboard?.cards, menu]);
  const [counts, setCounts] = useState<(number | null | undefined)[]>([]);
  const recentName = config.dashboard?.recentTable && menu.some((group) => group.items.includes(config.dashboard!.recentTable!))
    ? config.dashboard.recentTable
    : cards[0]?.table;
  const recent = recentName ? tables[recentName] : undefined;
  const [recentRows, setRecentRows] = useState<Record<string, any>[]>([]);
  const [loadingRecent, setLoadingRecent] = useState(true);
  const { labelFor } = useReferences(recent);

  useEffect(() => {
    let active = true;
    setCounts([]);
    cards.forEach((card, index) => {
      const save = (value: number | null) => {
        if (active) setCounts((previous) => Object.assign([...previous], { [index]: value }));
      };
      loadCard(card).then(save, () => save(null));
    });
    return () => { active = false; };
  }, [cards, me]);

  useEffect(() => {
    if (!recent) return;
    setLoadingRecent(true);
    listRecords(recent.name, 1, 5, "modifiedAt|DESC").then((p) => setRecentRows(p.records)).catch(() => setRecentRows([])).finally(() => setLoadingRecent(false));
  }, [recent]);

  const display = (value: number | null, card: DashboardCard) => {
    if (value === null) return "—";
    return card.format === "currency" ? formatMoney(value, card.currencyCode || "INR") : value.toLocaleString();
  };

  return (
    <Box>
      <Typography variant="h5" sx={{
        fontWeight: 700
      }}>Welcome back{me ? `, ${me.username}` : ""}</Typography>
      <Typography
        sx={{
          color: "text.secondary",
          mb: 3
        }}>{config.tagline || `Here's what's happening in ${config.appName || schema.app}.`}</Typography>
      {extensions.dashboardTop && React.createElement(extensions.dashboardTop)}
      <Box sx={{ display: "grid", gridTemplateColumns: { xs: "1fr 1fr", md: "repeat(4, 1fr)" }, gap: 2, mb: 3 }}>
        {cards.map((card, index) => (
          <Card key={index} variant="outlined" sx={{ borderRadius: 2 }}>
            <CardActionArea onClick={() => navigate(`/r/${card.table}`)}>
              <CardContent>
                <Box sx={{ display: "flex", alignItems: "center", gap: 1, color: "text.secondary", mb: 1 }}>
                  <Icon name={tables[card.table].icon} />
                  <Typography variant="body2" noWrap>{card.label || tables[card.table].label}</Typography>
                </Box>
                {counts[index] === undefined ? <Skeleton width={48} height={40} /> : <Typography variant="h4" sx={{
                  fontWeight: 700
                }}>{display(counts[index] ?? null, card)}</Typography>}
              </CardContent>
            </CardActionArea>
          </Card>
        ))}
      </Box>
      {recent && (
        <Paper variant="outlined" sx={{ borderRadius: 2, overflow: "hidden" }}>
          <Typography
            sx={{
              fontWeight: 600,
              px: 2,
              py: 1.5,
              borderBottom: 1,
              borderColor: "divider"
            }}>Recently updated {recent.label.toLowerCase()}</Typography>
          <DataTable table={recent} columns={recent.listColumns.slice(0, 5)} rows={recentRows} total={recentRows.length} loading={loadingRecent}
            page={0} pageSize={5} labelFor={labelFor} dense onOpen={(r) => navigate(`/r/${recent.name}/${r[recent.primaryKey]}`)} />
        </Paper>
      )}
    </Box>
  );
};
