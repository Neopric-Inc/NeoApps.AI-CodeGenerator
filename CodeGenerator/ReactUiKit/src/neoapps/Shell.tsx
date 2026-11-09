import React, { useState } from "react";
import { NavLink, Outlet, useLocation } from "react-router-dom";
import {
  AppBar, Avatar, Box, Chip, Divider, Drawer, IconButton, List, ListItemButton, ListItemIcon, ListItemText, ListSubheader,
  Menu, MenuItem, Toolbar, Typography, useMediaQuery, useTheme,
} from "@mui/material";
import MenuIcon from "@mui/icons-material/Menu";
import { useApp } from "./AppContext";
import { Icon } from "./icons";
import { extensions } from "../custom";
import { Breadcrumbs, markMenuNavigation } from "./Breadcrumbs";

const WIDTH = 248;

const NavItem: React.FC<{ to: string; icon: string; label: string; onClick?: () => void }> = ({ to, icon, label, onClick }) => {
  const { pathname } = useLocation();
  const active = to === "/" ? pathname === "/" : pathname === to || pathname.startsWith(to + "/");
  return (
    <ListItemButton component={NavLink as any} to={to} selected={active} onClick={() => { markMenuNavigation(); onClick?.(); }}
      sx={{ borderRadius: 1.5, mx: 1, my: 0.25, py: 0.75, "&.Mui-selected": { bgcolor: "primary.main", color: "primary.contrastText", "& .MuiListItemIcon-root": { color: "inherit" }, "&:hover": { bgcolor: "primary.dark" } } }}>
      <ListItemIcon sx={{ minWidth: 34 }}><Icon name={icon} /></ListItemIcon>
      <ListItemText primary={label} primaryTypographyProps={{ fontSize: 14, fontWeight: active ? 600 : 500, noWrap: true }} />
    </ListItemButton>
  );
};

export const Shell: React.FC = () => {
  const { config, schema, menu, tables, me, ai, logout } = useApp();
  const theme = useTheme();
  const desktop = useMediaQuery(theme.breakpoints.up("md"));
  const [open, setOpen] = useState(false);
  const [anchor, setAnchor] = useState<HTMLElement | null>(null);
  const appName = config.appName || schema.app;
  const close = () => !desktop && setOpen(false);

  const nav = (
    <Box sx={{ height: "100%", display: "flex", flexDirection: "column", bgcolor: "background.paper" }}>
      <Toolbar sx={{ gap: 1.5 }}>
        <Box sx={{ width: 32, height: 32, borderRadius: 1.5, bgcolor: "primary.main", color: "primary.contrastText", display: "grid", placeItems: "center", fontWeight: 800, fontSize: 14 }}>
          {appName.replace(/[^A-Za-z]/g, "").slice(0, 2).toUpperCase() || "A"}
        </Box>
        <Typography fontWeight={700} noWrap>{appName}</Typography>
      </Toolbar>
      <Divider />
      <Box sx={{ overflowY: "auto", flex: 1, py: 1 }}>
        <List dense disablePadding>
          <NavItem to="/" icon="dashboard" label="Dashboard" onClick={close} />
          {me && (extensions.nav || []).filter((n) => !n.visible || n.visible(me)).map((n) => <NavItem key={n.to} to={n.to} icon={n.icon} label={n.label} onClick={close} />)}
          {ai?.enabled && <NavItem to="/assistant" icon="auto_awesome" label={ai.assistantName || "Assistant"} onClick={close} />}
          {(ai?.enabled || config.jobs?.menu) && !(me?.role_name && (config as any).menuByRole?.[me.role_name] && !(config as any).menuByRole[me.role_name].includes("jobs")) && <NavItem to="/jobs" icon="bolt" label="Background jobs" onClick={close} />}
        </List>
        {menu.map((g) => (
          <List key={g.group} dense subheader={<ListSubheader sx={{ lineHeight: "32px", fontSize: 11, textTransform: "uppercase", letterSpacing: 0.6, bgcolor: "transparent" }}>{g.group}</ListSubheader>}>
            {g.items.map((t) => <NavItem key={t} to={`/r/${t}`} icon={tables[t].icon} label={tables[t].label} onClick={close} />)}
          </List>
        ))}
        {me?.is_admin && (
          <List dense subheader={<ListSubheader sx={{ lineHeight: "32px", fontSize: 11, textTransform: "uppercase", letterSpacing: 0.6, bgcolor: "transparent" }}>Administration</ListSubheader>}>
            <NavItem to="/admin/members" icon="group_add" label="Members" onClick={close} />
            <NavItem to="/admin/roles" icon="security" label="Roles & permissions" onClick={close} />
            <NavItem to="/admin/customize" icon="palette" label="Customize UI" onClick={close} />
          </List>
        )}
      </Box>
    </Box>
  );

  return (
    <Box sx={{ display: "flex", minHeight: "100vh", bgcolor: "grey.50" }}>
      <AppBar position="fixed" color="inherit" elevation={0} sx={{ borderBottom: 1, borderColor: "divider", width: { md: `calc(100% - ${WIDTH}px)` }, ml: { md: `${WIDTH}px` } }}>
        <Toolbar>
          {!desktop && <IconButton edge="start" onClick={() => setOpen(true)} sx={{ mr: 1 }}><MenuIcon /></IconButton>}
          <Box sx={{ flex: 1 }} />
          {me && (
            <>
              <Chip size="small" label={me.role_name || "Member"} color={me.is_admin ? "primary" : "default"} variant="outlined" sx={{ mr: 1.5 }} />
              <IconButton onClick={(e) => setAnchor(e.currentTarget)} size="small">
                <Avatar sx={{ width: 32, height: 32, fontSize: 14, bgcolor: "primary.main" }}>{me.username.slice(0, 1).toUpperCase()}</Avatar>
              </IconButton>
              <Menu anchorEl={anchor} open={!!anchor} onClose={() => setAnchor(null)} anchorOrigin={{ vertical: "bottom", horizontal: "right" }} transformOrigin={{ vertical: "top", horizontal: "right" }}>
                <MenuItem disabled><Typography variant="body2">Signed in as <b>{me.username}</b></Typography></MenuItem>
                <Divider />
                <MenuItem onClick={() => { setAnchor(null); logout(); }}>Sign out</MenuItem>
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
  );
};
