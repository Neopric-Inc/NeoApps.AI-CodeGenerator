# AuditDesk

Client-engagement app for a small accounting firm: engagements move Prepare → Plan → Fieldwork → Report → Signed off,
clients upload requested documents in their own portal, AI drafts findings from those documents, and a partner signs off.

- `auditdesk.sql`: 11 tables plus sample data (4 staff, 4 clients, 2 client contacts, 4 engagements in different phases)
- `auditdesk.ui.json`: roles, client and staff row scopes, hidden fields, status rules with per-role edit locks
  (`editableIn`), validations, numbering, append-only sign-offs, audit log, 8 dashboard cards, 3 AI workflows
- `acceptance-test.sh`: 22 API checks (scopes, locks, phase gate, numbering, validation, audit log)

Generate and test locally:

```bash
./neoapps new AuditDesk --schema samples/auditdesk/auditdesk.sql --ui samples/auditdesk/auditdesk.ui.json
# run the API, then with the admin login printed above:
ADMIN_U=<admin> ADMIN_P=<password> API=http://localhost:5100/v1/api bash samples/auditdesk/acceptance-test.sh
```

The test creates sign-in members for the sample people (password `Passw0rd2026`): priya.shah (Partner),
marcus.lee (Manager), ana.torres (Staff, leads ENG-2026-00001 and -00003), dana.brooks (Client, Northwind Outfitters).

Needs a generator with lifecycle `editableIn` (#138), lifecycle-aware Edit buttons (#139), several dashboard cards per
table (#137) and sample-row ownership (#135).
