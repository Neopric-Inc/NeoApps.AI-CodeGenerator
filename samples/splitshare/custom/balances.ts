// Pure balance maths for SplitShare (no React), so it can be checked in isolation.

export type Row = Record<string, any>;

export interface Data {
  groups: Row[];
  people: Row[]; // members table
  groupMembers: Row[];
  expenses: Row[];
  splits: Row[];
  settlements: Row[];
}

export interface Transfer { from: number; to: number; amount: number }

export interface GroupBalance {
  group: Row;
  currency: string;
  memberIds: number[];
  /** + = the group owes them, - = they owe the group. */
  net: Record<number, number>;
  transfers: Transfer[];
  unsplit: Row[];
  pending: Row[];
  totalSpent: number;
}

const r2 = (n: number) => Math.round(n * 100) / 100;
const active = (r: Row) => r.isActive === undefined || Number(r.isActive) === 1;

/**
 * Net balance per member in each group:
 *   + what they paid for split expenses (credited at the split total, so unsplit or partly split expenses don't skew it)
 *   - their share of each split expense
 *   + completed settlements they sent  - completed settlements they received
 */
export function computeBalances(d: Data): GroupBalance[] {
  return d.groups.filter(active).map((g) => {
    const gid = g.expense_group_id;
    const memberIds = Array.from(new Set(d.groupMembers.filter((gm) => active(gm) && gm.expense_group_id === gid).map((gm) => gm.member_id)));
    const net: Record<number, number> = {};
    const add = (id: number, v: number) => { net[id] = (net[id] || 0) + v; };
    memberIds.forEach((id) => (net[id] = 0));
    const expenses = d.expenses.filter((e) => active(e) && e.expense_group_id === gid);
    const unsplit: Row[] = [];
    for (const e of expenses) {
      const ss = d.splits.filter((s) => active(s) && s.expense_id === e.expense_id);
      if (!ss.length) { unsplit.push(e); continue; }
      const total = ss.reduce((t, s) => t + Number(s.owed_amount || 0), 0);
      add(e.paid_by_member_id, total);
      ss.forEach((s) => add(s.member_id, -Number(s.owed_amount || 0)));
    }
    const sts = d.settlements.filter((s) => active(s) && s.expense_group_id === gid);
    for (const s of sts) {
      if (String(s.status || "completed") !== "completed") continue;
      add(s.from_member_id, Number(s.amount || 0));
      add(s.to_member_id, -Number(s.amount || 0));
    }
    Object.keys(net).forEach((k) => (net[+k] = r2(net[+k])));
    return {
      group: g,
      currency: g.default_currency || "INR",
      memberIds: Array.from(new Set([...memberIds, ...Object.keys(net).map(Number)])),
      net,
      transfers: simplify(net),
      unsplit,
      pending: sts.filter((s) => String(s.status) === "pending"),
      totalSpent: r2(expenses.reduce((t, e) => t + Number(e.amount || 0), 0)),
    };
  });
}

/** Fewest-payments settle-up: repeatedly match the biggest debtor with the biggest creditor. */
export function simplify(net: Record<number, number>): Transfer[] {
  const debtors = Object.entries(net).filter(([, v]) => v < -0.005).map(([k, v]) => ({ id: +k, amt: -v }));
  const creditors = Object.entries(net).filter(([, v]) => v > 0.005).map(([k, v]) => ({ id: +k, amt: v }));
  const out: Transfer[] = [];
  while (debtors.length && creditors.length) {
    debtors.sort((a, b) => b.amt - a.amt);
    creditors.sort((a, b) => b.amt - a.amt);
    const d = debtors[0], c = creditors[0];
    const amt = r2(Math.min(d.amt, c.amt));
    if (amt > 0) out.push({ from: d.id, to: c.id, amount: amt });
    d.amt = r2(d.amt - amt); c.amt = r2(c.amt - amt);
    if (d.amt <= 0.005) debtors.shift();
    if (c.amt <= 0.005) creditors.shift();
  }
  return out;
}

export interface ProposedSplit { member_id: number; share_value: number | null; owed_amount: number }

/**
 * Splits an expense among the group's members by its split type. Exact / percentage / shares read per-person values
 * from notes, matched by first or full name, e.g. "Asha 60%, Ravi 40%" or "Asha 2, Ravi 1" or "Asha 1500, Ravi 900".
 * Rounding differences go to the payer. Returns a message when it had to fall back to an equal split.
 */
export function proposeSplits(expense: Row, members: Row[]): { splits: ProposedSplit[]; note?: string } {
  const amount = Number(expense.amount || 0);
  const type = String(expense.split_type || "equal");
  const payer = expense.paid_by_member_id;
  if (!members.length) return { splits: [], note: "This group has no members yet." };
  const equal = (note?: string) => {
    const each = Math.floor((amount / members.length) * 100) / 100;
    const splits = members.map((m) => ({ member_id: m.member_id, share_value: null, owed_amount: each }));
    return { splits: fixRounding(splits, amount, payer), note };
  };
  if (type === "equal") return equal();

  const values: Record<number, number> = {};
  const notes = String(expense.notes || "");
  for (const m of members) {
    const full = String(m.full_name || "").trim();
    const first = full.split(/\s+/)[0];
    for (const name of [full, first]) {
      if (!name) continue;
      const re = new RegExp(name.replace(/[.*+?^${}()|[\]\\]/g, "\\$&") + "\\s*[:=\\-]?\\s*(?:₹|rs\\.?|inr)?\\s*([0-9]+(?:\\.[0-9]+)?)", "i");
      const hit = notes.match(re);
      if (hit) { values[m.member_id] = Number(hit[1]); break; }
    }
  }
  const named = members.filter((m) => values[m.member_id] !== undefined && values[m.member_id] > 0);
  if (!named.length) return equal(`No ${type} values found in Notes, so it was split equally. Write e.g. "Asha 60%, Ravi 40%".`);

  let splits: ProposedSplit[];
  let note: string | undefined;
  if (type === "percentage") {
    const totalPct = named.reduce((t, m) => t + values[m.member_id], 0);
    splits = named.map((m) => ({ member_id: m.member_id, share_value: values[m.member_id], owed_amount: Math.floor(amount * values[m.member_id]) / 100 }));
    if (Math.abs(totalPct - 100) > 0.01) note = `Percentages add up to ${totalPct}%, not 100%; the difference goes to the payer.`;
  } else if (type === "shares") {
    const totalShares = named.reduce((t, m) => t + values[m.member_id], 0);
    splits = named.map((m) => ({ member_id: m.member_id, share_value: values[m.member_id], owed_amount: Math.floor((amount * values[m.member_id] / totalShares) * 100) / 100 }));
  } else {
    const sum = named.reduce((t, m) => t + values[m.member_id], 0);
    splits = named.map((m) => ({ member_id: m.member_id, share_value: values[m.member_id], owed_amount: values[m.member_id] }));
    if (Math.abs(sum - amount) > 0.01) note = `Exact amounts add up to ${r2(sum)}, not ${r2(amount)}; the difference goes to the payer.`;
  }
  return { splits: fixRounding(splits, amount, payer), note };
}

function fixRounding(splits: ProposedSplit[], amount: number, payer: number): ProposedSplit[] {
  const diff = r2(amount - splits.reduce((t, s) => t + s.owed_amount, 0));
  if (Math.abs(diff) >= 0.01) {
    let p = splits.find((s) => s.member_id === payer);
    if (!p) { p = { member_id: payer, share_value: null, owed_amount: 0 }; splits.push(p); }
    p.owed_amount = r2(p.owed_amount + diff);
  }
  return splits.map((s) => ({ ...s, owed_amount: r2(s.owed_amount) })).filter((s) => s.owed_amount !== 0);
}
