# Build an app on an existing database

`neoapps import-db` turns an existing MySQL, PostgreSQL, SQL Server or SQLite database into a NeoApps app and copies
its data in. Tested end to end with the [Chinook sample database](https://github.com/lerocha/chinook-database)
(11 tables, 15,607 rows) from SQLite, PostgreSQL and MySQL.

## 1. Point it at the source

The address goes in an environment variable, so the password never lands in a file or a chat:

```bash
export NEOAPPS_SOURCE_DB='postgres://user:password@host:5432/chinook'      # PostgreSQL (?schema=sales, ?sslmode=require)
export NEOAPPS_SOURCE_DB='mysql://user:password@host:3306/chinook'         # MySQL / MariaDB
export NEOAPPS_SOURCE_DB='sqlserver://user:password@host:1433/Chinook?TrustServerCertificate=true'   # SQL Server (?schema=dbo)
# SQLite: pass the file instead: --from chinook.sqlite
```

Encode special characters in the password (`@` → `%40`, `:` → `%3A`). Use a read-only user: the importer only reads.

## 2. Map

```bash
./neoapps import-db Chinook map            # or: --from chinook.sqlite
```

Writes to `workspace/Chinook/`:

- `import.map.json`: each source table and column and its name in the app. Edit `target` to rename, or set
  `"skip": true` on a table or column the app doesn't need. Re-run `map` after editing.
- `schema.sql`: the app's tables, following the NeoApps rules (snake_case, `<singular>_id` keys, audit columns,
  no built-in names). Don't edit it by hand; edit the map.
- `ui.config.json`: starter labels and list columns (only if there isn't one). Edit it like any other app.

How it maps:

| Source | App |
|---|---|
| `InvoiceLine`, `InvoiceLineId` | `invoice_line`, `invoice_line_id` |
| single whole-number key | kept as the key, values kept, so links hold |
| composite or text key (`PlaylistTrack(PlaylistId, TrackId)`) | new `<singular>_id` key; the old key stays as a unique pair |
| no key | new key; each copy replaces the table's rows |
| foreign key to a single whole-number key | a link (picker, related lists) |
| other foreign keys | plain values (listed in the warnings) |
| `users`, `roles`, other built-in names | `members`, `member_roles`, `app_<name>` |
| varchar/nvarchar/text, int, bigint, decimal/numeric/money, float, date, datetime/timestamp, bool/bit, uuid, json, enum | VARCHAR/TEXT, INT, BIGINT, DECIMAL, DOUBLE, DATE, DATETIME, TINYINT(1), CHAR(36), JSON, VARCHAR(100) |
| binary (blob, bytea, varbinary, image) | skipped (apps keep files as attachments) |

Apps work best with 3-15 tables: skip the ones nobody needs on screen.

## 3. Build the app

```bash
./neoapps preview Chinook
./neoapps new Chinook --schema workspace/Chinook/schema.sql --ui workspace/Chinook/ui.config.json
```

## 4. Copy the data

```bash
./neoapps import-db Chinook copy
```

Copies every table in link order and prints source and app counts per table. (`--to-env VAR` copies into the
mysql:// address in VAR instead of this computer's app; NeoApps Cloud uses it.) Ids are kept. Rows belong to the
app's owner; "Created by" shows `import`. Safe to run again: existing rows are updated by key, new rows added.
Rows deleted in the source are **not** deleted in the app.

## 5. When the source changes

```bash
./neoapps import-db Chinook map                                # keeps your names, adds new tables/columns
./neoapps evolve Chinook --name "source changes" --plan        # shows the migration
./neoapps evolve Chinook --name "source changes"               # applies it, data kept
./neoapps import-db Chinook copy
```

`copy` refuses to run while the app is missing a mapped column, so you can't copy into an out-of-date app.

## Claude (MCP)

The `neoapps_import_db` tool does the same (`step: "map" | "copy"`, `sourceEnv` = the variable's name, or
`sqliteFile`). The user sets the variable in their own terminal; never ask for a password in the chat.

## Limits

- Runs against the local app database. To put the result on NeoApps Cloud, publish the plan; copying data into a
  live cloud app isn't supported yet.
- Views, procedures and triggers are not copied. Check constraints and defaults are not carried over (values are).
- Links only for single whole-number keys; others stay as values.
- SQL Server was tested for address parsing only here. To try it locally:
  `docker run -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD='Str0ng!Pass' -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest`,
  load `Chinook_SqlServer.sql` with `sqlcmd`, then
  `NEOAPPS_SOURCE_DB='sqlserver://sa:Str0ng%21Pass@localhost:1433/Chinook?TrustServerCertificate=true'`.
