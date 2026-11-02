# SplitShare sample

Splitwise-style shared expenses: groups, members, expenses, per-member splits, settlements and comments, with
Admin / Member / Viewer roles, an AI assistant ("who owes whom in Roommates?") and an AI split workflow.

`custom/` shows the custom-screens extension point: a **My balances** page (net balance per group and the fewest
payments to settle up, with one-click "Record payment"), a personal panel on the dashboard, a **Split now** button
on expenses (equal / exact / percentage / shares, read from Notes) and an admin **Create login** button on members
that links a login to a person via `members.login_username`.

```bash
mkdir -p workspace && cp -r samples/splitshare workspace/SplitShare
./neoapps new SplitShare --schema workspace/SplitShare/schema.sql --ui workspace/SplitShare/ui.config.json [--deploy]
python3 workspace/SplitShare/seed.py <admin-username> <admin-password>   # optional demo data + demo logins
```
