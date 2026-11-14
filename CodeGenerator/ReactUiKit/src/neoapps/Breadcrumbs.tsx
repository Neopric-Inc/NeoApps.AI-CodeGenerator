import React, { useEffect, useState } from "react";
import { Link as RouterLink, useLocation } from "react-router-dom";
import { Breadcrumbs as MuiBreadcrumbs, Link, Typography } from "@mui/material";
import NavigateNextIcon from "@mui/icons-material/NavigateNext";
import { useApp } from "./AppContext";
import { getRecord } from "./api";
import { extensions } from "../custom";

/**
 * Breadcrumbs that follow how the user actually got here: a sidebar click starts a new trail, following links
 * (a list row, a child record, a related record) extends it, and going back to a page already in the trail
 * (breadcrumb click or browser Back) trims it. Kept per browser tab in sessionStorage so a reload keeps it.
 */
const KEY = "neoapps.trail";
const MAX = 6;
let fromMenu = false;
/** Call when navigating from the sidebar: the next page starts a fresh trail. */
export const markMenuNavigation = () => { fromMenu = true; };

const read = (): string[] => { try { return JSON.parse(sessionStorage.getItem(KEY) || "[]"); } catch { return []; } };
const write = (t: string[]) => { try { sessionStorage.setItem(KEY, JSON.stringify(t)); } catch { /* storage unavailable */ } };
const pathOf = (p: string) => p.split("?")[0];
const RECORD = /^\/r\/([^/]+)\/([^/]+)$/;
const LIST = /^\/r\/([^/]+)$/;
const FIXED: Record<string, string> = {
  "/assistant": "Assistant", "/jobs": "Background jobs", "/admin/members": "Members",
  "/admin/roles": "Roles & permissions", "/admin/customize": "Customize UI",
};
const recordLabels: Record<string, string> = {};

export const Breadcrumbs: React.FC = () => {
  const { pathname, search } = useLocation();
  const { tables, menu } = useApp();
  const [trail, setTrail] = useState<string[]>([]);
  const [, rerender] = useState(0);

  useEffect(() => {
    let t = fromMenu || pathname === "/" ? [] : read();
    fromMenu = false;
    const i = t.findIndex((p) => pathOf(p) === pathname);
    if (i >= 0) t = t.slice(0, i);
    t = [...t, pathname + search].filter((p) => pathOf(p) !== "/").slice(-MAX); // the dashboard is always the first crumb
    delete recordLabels[pathname]; // refresh the current record's name (it may have been edited)
    write(t);
    setTrail(t);
  }, [pathname, search]);

  useEffect(() => {
    trail.map(pathOf).forEach((path) => {
      const m = path.match(RECORD);
      if (!m || recordLabels[path] !== undefined) return;
      const meta = tables[m[1]];
      const id = decodeURIComponent(m[2]);
      const fallback = `${meta?.singular || m[1]} #${id}`;
      recordLabels[path] = "";
      getRecord(m[1], id)
        .then((d: any) => {
          const r = Array.isArray(d) ? d[0] : d;
          const v = r && meta?.displayField ? r[meta.displayField] : null;
          recordLabels[path] = v !== null && v !== undefined && String(v).trim() !== "" ? String(v) : fallback;
        })
        .catch(() => { recordLabels[path] = fallback; })
        .finally(() => rerender((x) => x + 1));
    });
  }, [trail, tables]);

  if (pathname === "/" || trail.length === 0) return null;

  const label = (p: string, prev?: string): string => {
    const path = pathOf(p);
    let m = path.match(LIST);
    if (m) return tables[m[1]]?.label || m[1];
    m = path.match(RECORD);
    if (m) {
      const meta = tables[m[1]];
      const name = recordLabels[path] || `${meta?.singular || m[1]} #${decodeURIComponent(m[2])}`;
      // without its list just before it, say what kind of record this is
      return prev && pathOf(prev) === `/r/${m[1]}` ? name : `${meta?.singular || m[1]}: ${name}`;
    }
    const nav = (extensions.nav || []).find((n) => n.to === path);
    if (nav) return nav.label;
    return FIXED[path] || path.split("/").filter(Boolean).pop()?.replace(/[-_]/g, " ") || "Page";
  };

  // the sidebar group of the first page, shown as plain text for orientation
  const first = pathOf(trail[0]).match(LIST) || pathOf(trail[0]).match(RECORD);
  const groupName = first ? menu.find((g) => g.items.includes(first[1]))?.group : undefined;
  const group = groupName && groupName !== label(trail[0]) ? groupName : undefined; // "Engagements > Engagements" says nothing

  return (
    <MuiBreadcrumbs separator={<NavigateNextIcon fontSize="small" />} sx={{ mb: 2, "& .MuiBreadcrumbs-ol": { flexWrap: "wrap" } }}>
      <Link component={RouterLink} to="/" underline="hover" color="inherit" onClick={markMenuNavigation}>Dashboard</Link>
      {group && <Typography
        sx={{
          color: "text.secondary",
          fontSize: "inherit"
        }}>{group}</Typography>}
      {trail.map((p, i) => i === trail.length - 1
        ? <Typography
        key={p}
        noWrap
        sx={{
          color: "text.primary",
          fontWeight: 600,
          fontSize: "inherit",
          maxWidth: 360
        }}>{label(p, trail[i - 1])}</Typography>
        : <Link key={p} component={RouterLink} to={p} underline="hover" color="inherit" sx={{ maxWidth: 260, overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>{label(p, trail[i - 1])}</Link>)}
    </MuiBreadcrumbs>
  );
};
