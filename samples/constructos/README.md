# ConstructOS sample

An operating system for a commercial general contractor: 41 tables in one app, covering four modules that share
projects, companies and cost codes.

- **Preconstruction:** spec sections, bid packages, scope items, invitations, bids, bid line items, scope gaps
- **Field:** schedule, daily logs + manpower, equipment + daily usage, RFIs, submittals + reviews, inspections, punch list, safety, meetings + action items, timesheets
- **Financials:** budget, subcontracts + schedule of values, purchase orders, change events, change orders + lines, pay apps + lines, lien waivers, invoices
- **Closeout:** closeout log, warranties, warranty claims

Roles: Estimator, Project Manager, Superintendent, Accounting, Subcontractor, Owner Rep, Equipment Manager (plus Administrator).

AI workflows (need `ANTHROPIC_API_KEY`): build bid packages from specs, extract a sub bid and find gaps, level all
bids on a package, review a submittal against its spec section, find drawing conflicts and draft RFIs, check a pay
app for over-billing, price a change event, process a daily log (manpower, equipment usage, punch, safety), and
extract meeting action items.

It is also the size stress test for the generator: `preview` warns above 15 tables, but all 41 generate with empty
warnings and the API and UI build.

## Run it

```bash
mkdir -p workspace && cp -r samples/constructos workspace/ConstructOS
./neoapps new ConstructOS --schema workspace/ConstructOS/schema.sql --ui workspace/ConstructOS/ui.config.json [--deploy]
python3 workspace/ConstructOS/seed.py <admin-username> <admin-password>   # demo job + one login per role
```

`new` builds the database from the full schema and records the migration in `migrations/` as already applied.

## Replay the upgrade (v1 → v2, keeping the data)

v2 added equipment tracking after v1 was in use. `v1/` holds the original requirements, `migrations/` the change as
`neoapps evolve` wrote it (FluentMigrator, Up/Down: two tables, the Equipment Manager role, two columns, a relation,
a wider column, grants for every existing role and member, and the new screens in saved menus).

```bash
mkdir -p workspace/ConstructOS && cp samples/constructos/v1/* samples/constructos/seed.py samples/constructos/verify_upgrade.py workspace/ConstructOS/
./neoapps new ConstructOS --schema workspace/ConstructOS/schema.sql --ui workspace/ConstructOS/ui.config.json
./neoapps preview ConstructOS --save                                   # record v1
# start the API (see the main README), then load data on v1:
python3 workspace/ConstructOS/seed.py <admin-username> <admin-password>

./neoapps evolve ConstructOS --schema samples/constructos/schema.sql --ui samples/constructos/ui.config.json \
  --name "add equipment tracking" --plan                               # the plan and the migration code
./neoapps evolve ConstructOS --schema samples/constructos/schema.sql --ui samples/constructos/ui.config.json \
  --name "add equipment tracking"                                      # backup, migrate, regenerate on the kept data
# restart the API, then:
python3 workspace/ConstructOS/verify_upgrade.py <admin-username> <admin-password>
```

To upgrade another copy of the v1 database (staging, production), deploy the v2 API: it applies the pending migration
on startup before serving requests.

Known limit: roles are per table, not per row, so the Subcontractor role sees every company's bids and pay apps.
Restricting it to its own company needs row-level filtering in the generated API.
