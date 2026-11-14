import React, { useCallback, useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import {
  Alert, Badge, Box, Button, Dialog, DialogActions, DialogContent, DialogTitle, Divider, FormControlLabel, IconButton, List,
  ListItemButton, ListItemText, Popover, Switch, TextField, Tooltip, Typography,
} from "@mui/material";
import NotificationsNoneOutlined from "@mui/icons-material/NotificationsNoneOutlined";
import { api } from "./api";

interface Item { id: number; title: string; table?: string; record?: string; actor?: string; createdAt: string; read: boolean }

const ago = (iso: string) => {
  const s = Math.max(0, (Date.now() - new Date(iso).getTime()) / 1000);
  if (s < 60) return "just now";
  if (s < 3600) return `${Math.floor(s / 60)} min ago`;
  if (s < 86400) return `${Math.floor(s / 3600)} h ago`;
  return new Date(iso).toLocaleDateString();
};

/** The bell: unread count (checked every 30 seconds), recent notifications, and the person's email setting. */
export const NotificationBell: React.FC = () => {
  const navigate = useNavigate();
  const [anchor, setAnchor] = useState<HTMLElement | null>(null);
  const [unread, setUnread] = useState(0);
  const [items, setItems] = useState<Item[]>([]);
  const [settings, setSettings] = useState<{ email: string; emailOn: boolean; emailAvailable: boolean } | null>(null);
  const [error, setError] = useState<string>();

  const poll = useCallback(() => {
    if (document.hidden) return;
    api<any>("GET", "Notifications/unread").then((r) => setUnread(r?.document?.unread ?? 0)).catch(() => undefined);
  }, []);
  useEffect(() => {
    poll();
    const t = setInterval(poll, 30000);
    return () => clearInterval(t);
  }, [poll]);

  const open = async (el: HTMLElement) => {
    setAnchor(el);
    const r = await api<any>("GET", "Notifications?limit=20").catch(() => null);
    setItems(r?.document?.items || []);
    setUnread(r?.document?.unread ?? 0);
  };

  const go = async (n: Item) => {
    setAnchor(null);
    if (!n.read) api<any>("POST", `Notifications/${n.id}/read`).then((r) => setUnread(r?.document?.unread ?? 0)).catch(() => undefined);
    if (n.table && n.record) navigate(`/r/${n.table}/${encodeURIComponent(n.record)}`);
  };

  const readAll = async () => {
    const r = await api<any>("POST", "Notifications/read-all").catch(() => null);
    setUnread(r?.document?.unread ?? 0);
    setItems((xs) => xs.map((x) => ({ ...x, read: true })));
  };

  const openSettings = async () => {
    setAnchor(null);
    setError(undefined);
    const r = await api<any>("GET", "Notifications/settings").catch(() => null);
    const d = r?.document || {};
    setSettings({ email: d.email || "", emailOn: !!d.emailOn, emailAvailable: !!d.emailAvailable });
  };

  const saveSettings = async () => {
    if (!settings) return;
    try {
      await api("PUT", "Notifications/settings", { email: settings.email, emailOn: settings.emailOn });
      setSettings(null);
    } catch (e: any) {
      setError(e.message);
    }
  };

  return (
    <>
      <Tooltip title="Notifications">
        <IconButton onClick={(e) => open(e.currentTarget)} sx={{ mr: 1 }} aria-label={`Notifications, ${unread} unread`}>
          <Badge badgeContent={unread} color="error" max={99}><NotificationsNoneOutlined /></Badge>
        </IconButton>
      </Tooltip>
      <Popover open={!!anchor} anchorEl={anchor} onClose={() => setAnchor(null)} anchorOrigin={{ vertical: "bottom", horizontal: "right" }}
        transformOrigin={{ vertical: "top", horizontal: "right" }} slotProps={{ paper: { sx: { width: 360, maxHeight: 480 } } }}>
        <Box sx={{ display: "flex", alignItems: "center", px: 2, py: 1.25 }}>
          <Typography sx={{ fontWeight: 700, flex: 1 }}>Notifications</Typography>
          {unread > 0 && <Button size="small" onClick={readAll}>Mark all read</Button>}
        </Box>
        <Divider />
        {items.length === 0
          ? <Typography variant="body2" sx={{ color: "text.secondary", p: 3, textAlign: "center" }}>Nothing yet.</Typography>
          : <List dense disablePadding>
            {items.map((n) => (
              <ListItemButton key={n.id} onClick={() => go(n)} sx={{ alignItems: "flex-start", bgcolor: n.read ? undefined : "action.hover" }}>
                <ListItemText primary={n.title} secondary={`${n.actor ? n.actor + " · " : ""}${ago(n.createdAt)}`}
                  slotProps={{ primary: { sx: { fontWeight: n.read ? 400 : 600, fontSize: 14 } } }} />
              </ListItemButton>
            ))}
          </List>}
        <Divider />
        <Box sx={{ px: 2, py: 1 }}><Button size="small" onClick={openSettings}>Email settings</Button></Box>
      </Popover>
      <Dialog open={!!settings} onClose={() => setSettings(null)} maxWidth="xs" fullWidth>
        <DialogTitle>Email notifications</DialogTitle>
        <DialogContent>
          {settings && !settings.emailAvailable && (
            <Alert severity="info" sx={{ mb: 2 }}>Email isn't set up for this app yet. Your choice is saved and applies once it is.</Alert>
          )}
          <TextField fullWidth type="email" label="Your email" sx={{ mt: 1 }} value={settings?.email || ""}
            onChange={(e) => setSettings((s) => s && { ...s, email: e.target.value })} />
          <FormControlLabel sx={{ mt: 1 }} label="Also email me my notifications"
            control={<Switch checked={!!settings?.emailOn} onChange={(e) => setSettings((s) => s && { ...s, emailOn: e.target.checked })} />} />
          {error && <Alert severity="error" sx={{ mt: 1 }}>{error}</Alert>}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setSettings(null)}>Cancel</Button>
          <Button variant="contained" onClick={saveSettings}>Save</Button>
        </DialogActions>
      </Dialog>
    </>
  );
};
