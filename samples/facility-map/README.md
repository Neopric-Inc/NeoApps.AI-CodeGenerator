# FacilityMap

Two apartment buildings drawn as a live 3D floor plan. Every unit and shared zone (lobby, laundry room, boiler room) is
coloured by its worst open ticket: red = safety hold, orange = urgent repair, yellow = routine maintenance, green = all clear.
Click a unit for its appliances and open tickets.

- **Roles:** Tenant (own unit only, reports issues), Handyman (updates repairs and appliances), Safety Inspector (the only
  role that can clear a safety hold), Property Manager (full admin, but can't clear a safety hold either).
- **Rules** (`ui.config.json`): `rowScopes` (tenant → their unit), `editableFields`, a ticket `lifecycle` whose Resolved step
  checks `severity ne "Safety Hold"` for everyone but the inspector, severity read-only after creation (`immutable`),
  ticket numbers, audit log of every change and refusal.
- **Live:** `"live": {"rooms": {"facility": "members"}, "tables": {"tickets": "facility", ...}}`. After any committed
  change to those tables, the server tells everyone in room `facility` which record changed; each screen re-reads it through
  the API, so a tenant never receives other units' data. Measured locally: about 25 ms from save to signal.
- **Screen:** `custom/FacilityMap.tsx` (Three.js + `joinRoom()`), at `/map`; `?wall=1` (or the Wall display button) fills
  the screen for an office TV.

```bash
./neoapps new FacilityMap --schema samples/facility-map/facility-map.sql --ui samples/facility-map/ui.config.json
# copy samples/facility-map/custom to workspace/FacilityMap/custom first to include the 3D screen
ADMIN_U=... ADMIN_P=... API=http://localhost:5130/v1/api DB=facility_map_db bash samples/facility-map/acceptance-test.sh
```

The acceptance script creates `jordan.lee` (Tenant, unit 302), `sam.rivera` (Handyman), `dana.kim` (Safety Inspector) and
`pat.morgan` (Property Manager) and runs 18 checks, including the live signal arriving within a second.
