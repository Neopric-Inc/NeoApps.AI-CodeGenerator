// SplitShare end-user screens: My balances, a personal dashboard panel, "Split now" on expenses and
// "Create login" on members. Installed into src/custom by the generator (see neoapps/extensions.ts).
import React, { useCallback, useEffect, useMemo, useState } from "react";
import { Link as RouterLink } from "react-router-dom";
import {
  Alert, Box, Button, Card, CardContent, Chip, CircularProgress, Dialog, DialogActions, DialogContent, DialogTitle,
  Divider, MenuItem, Paper, Stack, Table, TableBody, TableCell, TableHead, TableRow, TextField, Typography,
} from "@mui/material";
import { api, createRecord, listRecords, updateRecord } from "../neoapps/api";
import { useApp } from "../neoapps/AppContext";
import { Extensions } from "../neoapps/extensions";
import { formatMoney, toDateInput } from "../neoapps/format";
import { computeBalances, Data, GroupBalance, proposeSplits, ProposedSplit, Row } from "./balances";

const ALL = 100000;
const all = (t: string) => listRecords(t, 1, ALL).then((p) => p.records).catch(() => [] as Row[]);
const today = () => new Date().toISOString().slice(0, 10) + "T00:00:00";
const AUDIT = ["createdBy", "modifiedBy", "createdAt", "modifiedAt"];

/** Body for PUT: business columns only, dates in the format the API accepts. */
function putBody(row: Row, dateCols: string[] = []): Row {
  const b: Row = {};
  Object.entries(row).forEach(([k, v]) => { if (!AUDIT.includes(k)) b[k] = v; });
  dateCols.forEach((c) => { if (b[c]) b[c] = `${toDateInput(b[c])}T00:00:00`; });
  return b;
}

function useSplitData() {
  const { me } = useApp();
  const [data, setData] = useState<Data | null>(null);
  const [bump, setBump] = useState(0);
  useEffect(() => {
    let live = true;
    Promise.all(["expense_groups", "members", "group_members", "expenses", "expense_splits", "settlements"].map(all))
      .then(([groups, people, groupMembers, expenses, splits, settlements]) => live && setData({ groups, people, groupMembers, expenses, splits, settlements }));
    return () => { live = false; };
  }, [bump]);
  const meMember = useMemo(() => {
    if (!data || !me) return null;
    const u = me.username.toLowerCase();
    return data.people.find((p) => String(p.login_username || "").toLowerCase() === u)
      || data.people.find((p) => String(p.email || "").toLowerCase() === u) || null;
  }, [data, me]);
  const balances = useMemo(() => (data ? computeBalances(data) : []), [data]);
  const reload = useCallback(() => setBump((b) => b + 1), []);
  return { data, balances, meMember, reload };
}

const nameOf = (data: Data, id: number) => data.people.find((p) => p.member_id === id)?.full_name || `#${id}`;
const first = (data: Data, id: number) => String(nameOf(data, id)).split(" ")[0];

const Amount: React.FC<{ v: number; currency: string; signed?: boolean }> = ({ v, currency, signed }) => {
  const color = !signed || Math.abs(v) < 0.005 ? "text.primary" : v > 0 ? "success.main" : "error.main";
  return <Typography component="span" fontWeight={600} color={color}>{signed && v > 0.005 ? "+" : ""}{formatMoney(v, currency)}</Typography>;
};

// ---------------------------------------------------------------------------------------------- My balances page

const GroupCard: React.FC<{ b: GroupBalance; data: Data; meId?: number; onPaid: () => void }> = ({ b, data, meId, onPaid }) => {
  const { can } = useApp();
  const [busy, setBusy] = useState<string>();
  const [err, setErr] = useState<string>();
  const mine = meId !== undefined ? b.net[meId] || 0 : undefined;
  const transfers = meId !== undefined ? [...b.transfers].sort((x, y) => Number(y.from === meId || y.to === meId) - Number(x.from === meId || x.to === meId)) : b.transfers;

  const record = async (t: { from: number; to: number; amount: number }) => {
    const key = `${t.from}-${t.to}`;
    setBusy(key); setErr(undefined);
    try {
      await createRecord("settlements", {
        expense_group_id: b.group.expense_group_id, from_member_id: t.from, to_member_id: t.to, amount: t.amount,
        currency: b.currency, settlement_date: today(), method: "upi", status: "completed", notes: "Recorded from My balances",
      });
      onPaid();
    } catch (e: any) { setErr(e.message); } finally { setBusy(undefined); }
  };

  return (
    <Card variant="outlined" sx={{ borderRadius: 2 }}>
      <CardContent>
        <Stack direction="row" alignItems="center" justifyContent="space-between" sx={{ mb: 1.5 }}>
          <Box>
            <Typography variant="h6" fontWeight={700} component={RouterLink} to={`/r/expense_groups/${b.group.expense_group_id}`} sx={{ color: "inherit", textDecoration: "none" }}>{b.group.name}</Typography>
            <Typography variant="body2" color="text.secondary">{b.memberIds.length} people · {formatMoney(b.totalSpent, b.currency)} spent</Typography>
          </Box>
          {mine !== undefined && (
            <Box textAlign="right">
              <Typography variant="body2" color="text.secondary">{mine > 0.005 ? "You are owed" : mine < -0.005 ? "You owe" : "You're all square"}</Typography>
              {Math.abs(mine) > 0.005 && <Typography variant="h5" fontWeight={700} color={mine > 0 ? "success.main" : "error.main"}>{formatMoney(Math.abs(mine), b.currency)}</Typography>}
            </Box>
          )}
        </Stack>

        <Typography variant="subtitle2" sx={{ mb: 0.5 }}>Simplest way to settle up</Typography>
        {transfers.length === 0 ? <Typography color="text.secondary" variant="body2">Everyone is settled up.</Typography> : (
          <Stack spacing={1} sx={{ mb: 2 }}>
            {transfers.map((t) => {
              const involved = meId !== undefined && (t.from === meId || t.to === meId);
              return (
                <Paper key={`${t.from}-${t.to}`} variant="outlined" sx={{ p: 1.25, display: "flex", alignItems: "center", gap: 1.5, borderColor: involved ? "primary.main" : undefined, bgcolor: involved ? "action.hover" : undefined }}>
                  <Typography sx={{ flex: 1 }}>
                    <b>{t.from === meId ? "You" : first(data, t.from)}</b> {t.from === meId ? "pay" : "pays"} <b>{t.to === meId ? "you" : first(data, t.to)}</b>
                  </Typography>
                  <Amount v={t.amount} currency={b.currency} />
                  {can("settlements", "create") && (
                    <Button size="small" variant={involved ? "contained" : "outlined"} disabled={!!busy} onClick={() => record(t)}>
                      {busy === `${t.from}-${t.to}` ? <CircularProgress size={16} /> : "Record payment"}
                    </Button>
                  )}
                </Paper>
              );
            })}
          </Stack>
        )}
        {err && <Alert severity="error" sx={{ mb: 1 }}>{err}</Alert>}

        <Table size="small">
          <TableHead><TableRow><TableCell>Member</TableCell><TableCell align="right">Balance</TableCell></TableRow></TableHead>
          <TableBody>
            {[...b.memberIds].sort((x, y) => (b.net[y] || 0) - (b.net[x] || 0)).map((id) => (
              <TableRow key={id} selected={id === meId}>
                <TableCell>{nameOf(data, id)}{id === meId ? " (you)" : ""}</TableCell>
                <TableCell align="right"><Amount v={b.net[id] || 0} currency={b.currency} signed /></TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
        {(b.unsplit.length > 0 || b.pending.length > 0) && <Divider sx={{ my: 1.5 }} />}
        {b.unsplit.length > 0 && (
          <Alert severity="warning" sx={{ mb: 1 }}>
            Not split yet (not in balances):{" "}
            {b.unsplit.map((e, i) => <React.Fragment key={e.expense_id}>{i > 0 && ", "}<RouterLink to={`/r/expenses/${e.expense_id}`}>{e.description}</RouterLink> ({formatMoney(e.amount, b.currency)})</React.Fragment>)}
          </Alert>
        )}
        {b.pending.length > 0 && (
          <Alert severity="info">
            Pending payments (counted once marked completed):{" "}
            {b.pending.map((s, i) => <React.Fragment key={s.settlement_id}>{i > 0 && ", "}<RouterLink to={`/r/settlements/${s.settlement_id}`}>{first(data, s.from_member_id)} → {first(data, s.to_member_id)} {formatMoney(s.amount, b.currency)}</RouterLink></React.Fragment>)}
          </Alert>
        )}
      </CardContent>
    </Card>
  );
};

const MyBalances: React.FC = () => {
  const { me } = useApp();
  const { data, balances, meMember, reload } = useSplitData();
  if (!data) return <Box sx={{ display: "grid", placeItems: "center", py: 8 }}><CircularProgress /></Box>;
  const meId = meMember?.member_id;
  const myGroups = meId !== undefined ? balances.filter((b) => b.memberIds.includes(meId)) : balances;
  return (
    <Box>
      <Typography variant="h5" fontWeight={700}>{meMember ? `Hi ${String(meMember.full_name).split(" ")[0]}` : "Balances"}</Typography>
      <Typography color="text.secondary" sx={{ mb: 2 }}>
        Who owes whom in each group, and the fewest payments to settle up. Balances use split expenses and completed settlements.
      </Typography>
      {!meMember && (
        <Alert severity="info" sx={{ mb: 2 }}>
          Your login ({me?.username}) isn't linked to a member yet, so you're seeing every group. {me?.is_admin ? "Open a member and set \"App login\", or use Create login." : "Ask an admin to link your login to your member profile."}
        </Alert>
      )}
      {meMember && <Totals balances={myGroups} meId={meId!} />}
      {myGroups.length === 0 && <Alert severity="info">You're not in any groups yet.</Alert>}
      <Box sx={{ display: "grid", gridTemplateColumns: { xs: "1fr", lg: "1fr 1fr" }, gap: 2 }}>
        {myGroups.map((b) => <GroupCard key={b.group.expense_group_id} b={b} data={data} meId={meId} onPaid={reload} />)}
      </Box>
    </Box>
  );
};

const Totals: React.FC<{ balances: GroupBalance[]; meId: number }> = ({ balances, meId }) => {
  // Totals per currency so mixed-currency groups aren't added together.
  const per: Record<string, { owe: number; owed: number }> = {};
  balances.forEach((b) => {
    const v = b.net[meId] || 0; const t = (per[b.currency] = per[b.currency] || { owe: 0, owed: 0 });
    if (v < 0) t.owe += -v; else t.owed += v;
  });
  const cur = Object.keys(per).length ? Object.keys(per) : ["INR"];
  const sum = (k: "owe" | "owed") => cur.map((c) => formatMoney(per[c]?.[k] || 0, c)).join(" + ");
  return (
    <Box sx={{ display: "grid", gridTemplateColumns: { xs: "1fr", sm: "1fr 1fr" }, gap: 2, mb: 2 }}>
      <Card variant="outlined" sx={{ borderRadius: 2 }}><CardContent>
        <Typography variant="body2" color="text.secondary">You owe</Typography>
        <Typography variant="h4" fontWeight={700} color="error.main">{sum("owe")}</Typography>
      </CardContent></Card>
      <Card variant="outlined" sx={{ borderRadius: 2 }}><CardContent>
        <Typography variant="body2" color="text.secondary">You are owed</Typography>
        <Typography variant="h4" fontWeight={700} color="success.main">{sum("owed")}</Typography>
      </CardContent></Card>
    </Box>
  );
};

// ------------------------------------------------------------------------------------------ dashboard panel

const DashboardTop: React.FC = () => {
  const { data, balances, meMember } = useSplitData();
  if (!data || !meMember) return null;
  const mine = balances.filter((b) => b.memberIds.includes(meMember.member_id));
  const lines = mine.flatMap((b) => b.transfers.filter((t) => t.from === meMember.member_id || t.to === meMember.member_id).map((t) => ({ b, t })));
  return (
    <Paper variant="outlined" sx={{ p: 2, mb: 3, borderRadius: 2 }}>
      <Stack direction={{ xs: "column", sm: "row" }} justifyContent="space-between" alignItems={{ sm: "center" }} gap={1} sx={{ mb: lines.length ? 1.5 : 0 }}>
        <Typography fontWeight={700}>Your balances</Typography>
        <Button component={RouterLink} to="/balances" size="small">Open My balances</Button>
      </Stack>
      <Totals balances={mine} meId={meMember.member_id} />
      {lines.length === 0 ? <Typography color="text.secondary">You're all settled up.</Typography> : (
        <Stack spacing={0.75}>
          {lines.map(({ b, t }) => (
            <Typography key={`${b.group.expense_group_id}-${t.from}-${t.to}`}>
              {t.from === meMember.member_id
                ? <>You owe <b>{first(data, t.to)}</b> <Amount v={t.amount} currency={b.currency} /></>
                : <><b>{first(data, t.from)}</b> owes you <Amount v={t.amount} currency={b.currency} /></>}
              {" "}<Chip size="small" label={b.group.name} variant="outlined" />
            </Typography>
          ))}
        </Stack>
      )}
    </Paper>
  );
};

// --------------------------------------------------------------------------------------- record page actions

const SplitNow: React.FC<{ record: Row; onChanged: () => void }> = ({ record, onChanged }) => {
  const { can } = useApp();
  const [open, setOpen] = useState(false);
  const [state, setState] = useState<{ people: Row[]; proposal: ProposedSplit[]; note?: string; existing: number } | null>(null);
  const [saving, setSaving] = useState(false);
  const [err, setErr] = useState<string>();
  if (!can("expense_splits", "create")) return null;

  const start = async () => {
    setOpen(true); setState(null); setErr(undefined);
    const [gms, people, splits] = await Promise.all([all("group_members"), all("members"), all("expense_splits")]);
    const ids = new Set(gms.filter((g) => g.expense_group_id === record.expense_group_id && Number(g.isActive ?? 1) === 1).map((g) => g.member_id));
    const members = people.filter((p) => ids.has(p.member_id));
    const existing = splits.filter((s) => s.expense_id === record.expense_id && Number(s.isActive ?? 1) === 1).length;
    const { splits: proposal, note } = proposeSplits(record, members);
    setState({ people, proposal, note, existing });
  };

  const save = async () => {
    if (!state) return;
    setSaving(true); setErr(undefined);
    try {
      for (const s of state.proposal) {
        await createRecord("expense_splits", {
          expense_id: record.expense_id, member_id: s.member_id, share_value: s.share_value, owed_amount: s.owed_amount,
          status: s.member_id === record.paid_by_member_id ? "settled" : "unsettled",
        });
      }
      if (can("expenses", "update")) await updateRecord("expenses", record.expense_id, { ...putBody(record, ["expense_date"]), split_status: "split" });
      setOpen(false); onChanged();
    } catch (e: any) { setErr(e.message); } finally { setSaving(false); }
  };

  const who = (id: number) => state?.people.find((p) => p.member_id === id)?.full_name || `#${id}`;
  return (
    <>
      <Button variant="contained" onClick={start}>Split now</Button>
      <Dialog open={open} onClose={() => setOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Split "{record.description}" ({record.split_type})</DialogTitle>
        <DialogContent>
          {!state ? <Box sx={{ display: "grid", placeItems: "center", py: 4 }}><CircularProgress /></Box> : (
            <>
              {state.existing > 0 && <Alert severity="warning" sx={{ mb: 2 }}>This expense already has {state.existing} split row(s). Saving adds more; delete the old ones first if you're re-splitting.</Alert>}
              {state.note && <Alert severity="info" sx={{ mb: 2 }}>{state.note}</Alert>}
              <Table size="small">
                <TableHead><TableRow><TableCell>Member</TableCell><TableCell align="right">Share</TableCell><TableCell align="right">Owes</TableCell></TableRow></TableHead>
                <TableBody>
                  {state.proposal.map((s) => (
                    <TableRow key={s.member_id}>
                      <TableCell>{who(s.member_id)}{s.member_id === record.paid_by_member_id ? " (paid)" : ""}</TableCell>
                      <TableCell align="right">{s.share_value ?? "equal"}</TableCell>
                      <TableCell align="right">{formatMoney(s.owed_amount, record.currency || "INR")}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
              {err && <Alert severity="error" sx={{ mt: 2 }}>{err}</Alert>}
            </>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpen(false)}>Cancel</Button>
          <Button variant="contained" disabled={!state || !state.proposal.length || saving} onClick={save}>{saving ? "Saving…" : "Save splits"}</Button>
        </DialogActions>
      </Dialog>
    </>
  );
};

/** Random initial password from the browser's cryptographic RNG (rejection sampling avoids modulo bias). */
const randomPassword = (length = 14) => {
  const c = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
  const limit = 256 - (256 % c.length);
  const out: string[] = [];
  const buf = new Uint8Array(32);
  while (out.length < length) {
    window.crypto.getRandomValues(buf);
    for (const b of Array.from(buf)) if (b < limit && out.length < length) out.push(c[b % c.length]);
  }
  return out.join("") + "@1";
};

const CreateLogin: React.FC<{ record: Row; onChanged: () => void }> = ({ record, onChanged }) => {
  const { me } = useApp();
  const [open, setOpen] = useState(false);
  const [roles, setRoles] = useState<{ role_id: number; role_name: string }[]>([]);
  const [form, setForm] = useState({ username: "", password: "", role_id: 0 });
  const [done, setDone] = useState(false);
  const [err, setErr] = useState<string>();
  if (!me?.is_admin) return null;

  const start = async () => {
    setOpen(true); setDone(false); setErr(undefined);
    const r = await api<any>("GET", "Access/roles").catch(() => null);
    const list = (r?.document || []).filter((x: any) => !x.is_admin);
    setRoles(list);
    setForm({ username: String(record.email || record.full_name || "").toLowerCase().replace(/\s+/g, "."), password: randomPassword(), role_id: list.find((x: any) => x.role_name === "Member")?.role_id || list[0]?.role_id || 0 });
  };
  const save = async () => {
    setErr(undefined);
    try {
      const r = await api<any>("POST", "Access/members", form);
      if (r?.code !== 1) throw new Error(r?.message || "Could not create the login.");
      await updateRecord("members", record.member_id, { ...putBody(record), login_username: form.username });
      setDone(true); onChanged();
    } catch (e: any) { setErr(e.message); }
  };
  return (
    <>
      <Button variant="outlined" onClick={start}>{record.login_username ? "Create another login" : "Create login"}</Button>
      <Dialog open={open} onClose={() => setOpen(false)} maxWidth="xs" fullWidth>
        <DialogTitle>App login for {record.full_name}</DialogTitle>
        <DialogContent>
          {done ? (
            <Alert severity="success" sx={{ mt: 1 }}>
              Login created and linked. Share these with {String(record.full_name).split(" ")[0]}:<br />
              Username: <b>{form.username}</b><br />Password: <b>{form.password}</b>
            </Alert>
          ) : (
            <Stack spacing={2} sx={{ mt: 1 }}>
              <TextField label="Username" value={form.username} onChange={(e) => setForm({ ...form, username: e.target.value })} />
              <TextField label="Password" value={form.password} onChange={(e) => setForm({ ...form, password: e.target.value })} />
              <TextField select label="Role" value={form.role_id || ""} onChange={(e) => setForm({ ...form, role_id: Number(e.target.value) })}>
                {roles.map((r) => <MenuItem key={r.role_id} value={r.role_id}>{r.role_name}</MenuItem>)}
              </TextField>
              {err && <Alert severity="error">{err}</Alert>}
            </Stack>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpen(false)}>{done ? "Close" : "Cancel"}</Button>
          {!done && <Button variant="contained" disabled={!form.username || !form.password || !form.role_id} onClick={save}>Create login</Button>}
        </DialogActions>
      </Dialog>
    </>
  );
};

const RecordActions: Extensions["recordActions"] = ({ table, record, onChanged }) => {
  if (table === "expenses" && record.split_status !== "split" && record.split_status !== "settled") return <SplitNow record={record} onChanged={onChanged} />;
  if (table === "members") return <CreateLogin record={record} onChanged={onChanged} />;
  return null;
};

export const extensions: Extensions = {
  routes: [{ path: "/balances", element: <MyBalances /> }],
  nav: [{ to: "/balances", icon: "wallet", label: "My balances" }],
  dashboardTop: DashboardTop,
  recordActions: RecordActions,
};
