# ConstructOS requirements

**Goal.** One app for a commercial general contractor: bid the work, run the field, bill and pay, close out.

**Users & roles.** Administrator; Estimator (preconstruction); Project Manager (most of the job); Superintendent
(field records); Accounting (financials); Subcontractor (bids, RFIs, submittals, pay apps); Owner Rep (read-only);
Equipment Manager (v2: equipment and its usage, purchase orders and invoices for rentals).

**What it tracks.** Projects, team, documents · spec sections, bid packages, scope items, invitations, bids, bid lines,
scope gaps · schedule, daily logs, manpower, equipment and daily usage (v2), RFIs, submittals and reviews, inspections,
punch list, safety, meetings and action items, timesheets · budget, subcontracts and schedule of values, purchase
orders, change events, change orders and lines, pay apps and lines, lien waivers, invoices · closeout log, warranties,
warranty claims · companies, contacts, cost codes.

**AI.** Specs → bid packages; extract a bid and find gaps; level bids; review a submittal; drawings → RFIs; check a pay
app; price a change event; process a daily log (manpower, equipment usage, punch, safety); meeting action items.

**Assumptions.** Permissions are per table, so subcontractors see every company's bids (row-level scoping is open).

**Open questions.** Row-level access for subcontractors; equipment cost roll-up into the budget.

**Changelog**
- v1 (2026-11-02): initial 39-table app.
- v2 (2026-11-03): equipment tracking: `equipment` and `equipment_logs` tables, `daily_logs.equipment_notes`,
  `inspections.company_id` (responsible company), punch descriptions up to 2000 characters, Equipment Manager role.
  Applied to running databases by `migrations/M20261103005208_AddEquipmentTracking.cs`.
