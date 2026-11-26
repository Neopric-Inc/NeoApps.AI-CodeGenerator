import React, { useState } from "react";
import { NavLink, Outlet, useLocation } from "react-router-dom";
import {
  AppBar, Avatar, Box, Chip, Divider, Drawer, IconButton, List, ListItemButton, ListItemIcon, ListItemText, ListSubheader,
  Menu, MenuItem, Toolbar, Typography, useMediaQuery, useTheme,
} from "@mui/material";
import MenuIcon from "@mui/icons-material/Menu";
import { useApp } from "./AppContext";
import { Icon } from "./icons";
import { extensions } from "./customExtensions";
import { Breadcrumbs } from "./Breadcrumbs";
import { NotificationBell } from "./NotificationBell";

import { t, tn, tx } from "./i18n";
import { LanguagePicker } from "./i18n/LanguagePicker";
const WIDTH = 248;

/** The one menu link for this page: the longest link that is the path or a parent of it ("/r/tasks" for "/r/tasks/7"). */
const ActiveContext = React.createContext<string | undefined>(undefined);
const activeLink = (pathname: string, links: string[]) =>
  links.filter((to) => (to === "/" ? pathname === "/" : pathname === to || pathname.startsWith(to + "/"))).sort((a, b) => b.length - a.length)[0];

const NavItem: React.FC<{ to: string; icon: string; label: string; onClick?: () => void }> = ({ to, icon, label, onClick }) => {
  const active = React.useContext(ActiveContext) === to;
  return (
    <ListItemButton component={NavLink as any} to={to} end={to === "/"} selected={active} onClick={() => onClick?.()} aria-current={active ? "page" : undefined}
      sx={{ borderRadius: 1.5, mx: 1, my: 0.25, py: 0.75, "&.Mui-selected": { bgcolor: "primary.main", color: "primary.contrastText", "& .MuiListItemIcon-root": { color: "inherit" }, "&:hover": { bgcolor: "primary.dark" } } }}>
      <ListItemIcon sx={{ minWidth: 34 }}><Icon name={icon} /></ListItemIcon>
      <ListItemText primary={label} slotProps={{
        primary: { noWrap: true, sx: { fontSize: 14, fontWeight: active ? 600 : 500 } }
      }} />
    </ListItemButton>
  );
};

export interface MenuPage { to: string; icon: string; label: string }

/** Pages (not tables) the sidebar can show for this person: custom screens, the assistant and background jobs. */
export function useMenuPages(all = false): MenuPage[] {
  const { config, me, ai } = useApp();
  const pages: MenuPage[] = [];
  for (const n of extensions.nav || []) if (all || (me && (!n.visible || n.visible(me)))) pages.push({ to: n.to, icon: n.icon, label: tx(n.label) });
  if (all || ai?.enabled) pages.push({ to: "/assistant", icon: "auto_awesome", label: ai?.assistantName || t("Assistant") });
  const jobs = ai?.enabled || config.jobs?.menu || ((config as any).jobs?.schedules?.length && me?.is_admin);
  const roleMenu = me?.role_name ? (config as any).menuByRole?.[me.role_name] as string[] | undefined : undefined;
  if (all || (jobs && !(roleMenu && !roleMenu.includes("jobs")))) pages.push({ to: "/jobs", icon: "bolt", label: t("Background jobs") });
  return pages;
}

export const Shell: React.FC = () => {
  const { config, schema, menu, tables, me, ai, logout } = useApp();
  const theme = useTheme();
  const desktop = useMediaQuery(theme.breakpoints.up("md"));
  const [open, setOpen] = useState(false);
  const [anchor, setAnchor] = useState<HTMLElement | null>(null);
  const appName = tx(config.appName) || schema.app;
  const close = () => !desktop && setOpen(false);
  const { pathname } = useLocation();
  const hiddenPages = new Set(config.hiddenPages || []);
  const pages = useMenuPages().filter((p) => !hiddenPages.has(p.to));
  const pageOf = (to: string) => pages.find((p) => p.to === to);
  const grouped = new Set(menu.flatMap((g) => g.items.filter((i) => i.startsWith("/"))));
  const links = ["/", "/admin/members", "/admin/roles", "/admin/customize", ...pages.map((p) => p.to),
    ...menu.flatMap((g) => g.items.filter((i) => !i.startsWith("/")).map((i) => `/r/${i}`))];
  const active = activeLink(pathname, links);
  const item = (i: string) => {
    if (!i.startsWith("/")) return tables[i] ? <NavItem key={i} to={`/r/${i}`} icon={tables[i].icon} label={tables[i].label} onClick={close} /> : null;
    const p = pageOf(i);
    return p ? <NavItem key={i} to={p.to} icon={p.icon} label={p.label} onClick={close} /> : null;
  };

  const nav = (
    <Box sx={{ height: "100%", display: "flex", flexDirection: "column", bgcolor: "background.paper" }}>
      <Toolbar sx={{ gap: 1.5 }}>
        <Box sx={{ width: 32, height: 32, borderRadius: 1.5, bgcolor: "primary.main", color: "primary.contrastText", display: "grid", placeItems: "center", fontWeight: 800, fontSize: 14 }}>
          {appName.replace(/[^A-Za-z]/g, "").slice(0, 2).toUpperCase() || "A"}
        </Box>
        <Typography noWrap sx={{
          fontWeight: 700
        }}>{appName}</Typography>
      </Toolbar>
      <Divider />
      <Box sx={{ overflowY: "auto", flex: 1, py: 1 }}>
        <List dense disablePadding>
          <NavItem to="/" icon="dashboard" label={t("Dashboard")} onClick={close} />
          {pages.filter((p) => !grouped.has(p.to)).map((p) => item(p.to))}
        </List>
        {menu.map((g) => ({ g, items: g.items.map(item).filter(Boolean) })).filter((x) => x.items.length).map(({ g, items }) => (
          <List key={g.group} dense subheader={<ListSubheader sx={{ lineHeight: "32px", fontSize: 11, textTransform: "uppercase", letterSpacing: 0.6, bgcolor: "transparent" }}>{g.group}</ListSubheader>}>
            {items}
          </List>
        ))}
        {me?.is_admin && (
          <List dense subheader={<ListSubheader sx={{ lineHeight: "32px", fontSize: 11, textTransform: "uppercase", letterSpacing: 0.6, bgcolor: "transparent" }}>{t("Administration")}</ListSubheader>}>
            <NavItem to="/admin/members" icon="group_add" label={t("Members")} onClick={close} />
            <NavItem to="/admin/roles" icon="security" label={t("Roles & permissions")} onClick={close} />
            <NavItem to="/admin/customize" icon="palette" label={t("Customize UI")} onClick={close} />
          </List>
        )}
      </Box>
    </Box>
  );

  return (
    <ActiveContext.Provider value={active}>
    <Box sx={{ display: "flex", minHeight: "100vh", bgcolor: "grey.50" }}>
      <AppBar position="fixed" color="inherit" elevation={0} sx={{ borderBottom: 1, borderColor: "divider", width: { md: `calc(100% - ${WIDTH}px)` }, ml: { md: `${WIDTH}px` } }}>
        <Toolbar>
          {!desktop && <IconButton edge="start" onClick={() => setOpen(true)} sx={{ mr: 1 }}><MenuIcon /></IconButton>}
          <Box sx={{ flex: 1 }} />
          {me && (
            <>
              <LanguagePicker signedIn compact />
              <NotificationBell />
              <Chip size="small" label={tx(me.role_name) || t("Member")} color={me.is_admin ? "primary" : "default"} variant="outlined" sx={{ mr: 1.5 }} />
              <IconButton onClick={(e) => setAnchor(e.currentTarget)} size="small">
                <Avatar sx={{ width: 32, height: 32, fontSize: 14, bgcolor: "primary.main" }}>{me.username.slice(0, 1).toUpperCase()}</Avatar>
              </IconButton>
              <Menu anchorEl={anchor} open={!!anchor} onClose={() => setAnchor(null)} anchorOrigin={{ vertical: "bottom", horizontal: "right" }} transformOrigin={{ vertical: "top", horizontal: "right" }}>
                <MenuItem disabled><Typography variant="body2">{tn("Signed in as {name}", { name: <b>{me.username}</b> })}</Typography></MenuItem>
                <Divider />
                <MenuItem onClick={() => { setAnchor(null); logout(); }}>{t("Sign out")}</MenuItem>
              </Menu>
            </>
          )}
        </Toolbar>
      </AppBar>
      <Box component="nav" sx={{ width: { md: WIDTH }, flexShrink: { md: 0 } }}>
        <Drawer variant={desktop ? "permanent" : "temporary"} open={desktop || open} onClose={() => setOpen(false)}
          sx={{ "& .MuiDrawer-paper": { width: WIDTH, boxSizing: "border-box", borderRight: 1, borderColor: "divider" } }}>
          {nav}
        </Drawer>
      </Box>
      <Box component="main" sx={{ flexGrow: 1, minWidth: 0, p: { xs: 2, md: 3 }, mt: 8 }}>
        <Breadcrumbs />
        <Outlet />
      </Box>
    </Box>
    </ActiveContext.Provider>
  );
};
