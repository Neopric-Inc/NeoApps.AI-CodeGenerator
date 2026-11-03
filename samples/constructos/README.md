# ConstructOS sample

An operating system for a commercial general contractor: 39 tables in one app, covering four modules that share
projects, companies and cost codes.

- **Preconstruction:** spec sections, bid packages, scope items, invitations, bids, bid line items, scope gaps
- **Field:** schedule, daily logs + manpower, RFIs, submittals + reviews, inspections, punch list, safety, meetings + action items, timesheets
- **Financials:** budget, subcontracts + schedule of values, purchase orders, change events, change orders + lines, pay apps + lines, lien waivers, invoices
- **Closeout:** closeout log, warranties, warranty claims

Roles: Estimator, Project Manager, Superintendent, Accounting, Subcontractor, Owner Rep (plus Administrator).

AI workflows (need `ANTHROPIC_API_KEY`): build bid packages from specs, extract a sub bid and find gaps, level all
bids on a package, review a submittal against its spec section, find drawing conflicts and draft RFIs, check a pay
app for over-billing, price a change event, process a daily log, and extract meeting action items.

It is also the size stress test for the generator: `preview` warns above 15 tables, but all 39 generate with empty
warnings and the API and UI build.

```bash
mkdir -p workspace && cp -r samples/constructos workspace/ConstructOS
./neoapps new ConstructOS --schema workspace/ConstructOS/schema.sql --ui workspace/ConstructOS/ui.config.json [--deploy]
python3 workspace/ConstructOS/seed.py <admin-username> <admin-password>   # demo job + one login per role
```

Known limit: roles are per table, not per row, so the Subcontractor role sees every company's bids and pay apps.
Restricting it to its own company needs row-level filtering in the generated API.
