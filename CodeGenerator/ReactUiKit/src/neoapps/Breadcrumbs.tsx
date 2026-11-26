import React, { useEffect, useState } from "react";
import { Link as RouterLink, useLocation } from "react-router-dom";
import { Breadcrumbs as MuiBreadcrumbs, Link, Typography } from "@mui/material";
import NavigateNextIcon from "@mui/icons-material/NavigateNext";
import { useApp } from "./AppContext";
import { getRecord } from "./api";
import { extensions } from "./customExtensions";

import { t, tx } from "./i18n";
/**
 * Breadcrumbs by location, the same however the page was reached: Dashboard › menu group › list › record.
 * The list crumb returns to that list as it was last shown (its filters, search and page), kept per browser tab.
 */
const LAST_LIST = "neoapps.lastList.";
const RECORD = /^\/r\/([^/]+)\/([^/]+)$/;
const LIST = /^\/r\/([^/]+)$/;
const ADMIN: Record<string, string> = {
  "/admin/members": "Members", "/admin/roles": "Roles & permissions", "/admin/customize": "Customize UI", // translated where shown
};
const FIXED: Record<string, string> = { "/assistant": "Assistant", "/jobs": "Background jobs" };
const recordLabels: Record<string, string> = {};

/** Kept for custom code that called it; breadcrumbs no longer depend on how a page was reached. */
export const markMenuNavigation = () => { /* no-op */ };

const lastList = (table: string) => { try { return sessionStorage.getItem(LAST_LIST + table) || ""; } catch { return ""; } };

export const Breadcrumbs: React.FC = () => {
  const { pathname, search } = useLocation();
  const { tables, menu, ai } = useApp();
  const [, rerender] = useState(0);

  // remember each list's filters/search/page, so "Tasks" in a record's breadcrumbs goes back to the same view
  useEffect(() => {
    const m = pathname.match(LIST);
    if (m) try { sessionStorage.setItem(LAST_LIST + m[1], search); } catch { /* storage unavailable */ }
  }, [pathname, search]);

  const rec = pathname.match(RECORD);
  useEffect(() => {
    if (!rec) return;
    const meta = tables[rec[1]];
    const id = decodeURIComponent(rec[2]);
    const fallback = `${meta?.singular || rec[1]} #${id}`;
    let alive = true;
    getRecord(rec[1], id)
      .then((d: any) => {
        const r = Array.isArray(d) ? d[0] : d;
        const v = r && meta?.displayField ? r[meta.displayField] : null;
        recordLabels[pathname] = v !== null && v !== undefined && String(v).trim() !== "" ? String(v) : fallback;
      })
      .catch(() => { recordLabels[pathname] = fallback; })
      .finally(() => alive && rerender((x) => x + 1));
    return () => { alive = false; };
  }, [pathname, tables]); // eslint-disable-line react-hooks/exhaustive-deps

  if (pathname === "/") return null;

  const crumbs: { label: string; to?: string }[] = [];
  const table = (pathname.match(LIST) || rec)?.[1];
  if (table) {
    const meta = tables[table];
    const group = menu.find((g) => g.items.includes(table))?.group;
    if (group && group !== meta?.label) crumbs.push({ label: group });
    crumbs.push({ label: meta?.label || table, to: rec ? `/r/${table}${lastList(table)}` : undefined });
    if (rec) crumbs.push({ label: recordLabels[pathname] || `${meta?.singular || table} #${decodeURIComponent(rec[2])}` });
  } else if (ADMIN[pathname]) {
    crumbs.push({ label: t("Administration") }, { label: t(ADMIN[pathname]) });
  } else if (FIXED[pathname]) {
    const group = menu.find((g) => g.items.includes(pathname))?.group;
    if (group) crumbs.push({ label: group });
    crumbs.push({ label: pathname === "/assistant" && ai?.assistantName ? ai.assistantName : t(FIXED[pathname]) });
  } else {
    const nav = (extensions.nav || []).find((n) => n.to === pathname);
    const group = menu.find((g) => g.items.includes(pathname))?.group;
    if (group) crumbs.push({ label: group });
    crumbs.push({ label: nav ? tx(nav.label) : pathname.split("/").filter(Boolean).pop()?.replace(/[-_]/g, " ") || t("Page") });
  }

  return (
    <MuiBreadcrumbs separator={<NavigateNextIcon fontSize="small" />} sx={{ mb: 2, "& .MuiBreadcrumbs-ol": { flexWrap: "wrap" } }}>
      <Link component={RouterLink} to="/" underline="hover" color="inherit">{t("Dashboard")}</Link>
      {crumbs.map((c, i) => i === crumbs.length - 1
        ? <Typography key={i} noWrap sx={{ color: "text.primary", fontWeight: 600, fontSize: "inherit", maxWidth: 420 }}>{c.label}</Typography>
        : c.to
          ? <Link key={i} component={RouterLink} to={c.to} underline="hover" color="inherit" sx={{ maxWidth: 260, overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>{c.label}</Link>
          : <Typography key={i} sx={{ color: "text.secondary", fontSize: "inherit" }}>{c.label}</Typography>)}
    </MuiBreadcrumbs>
  );
};
