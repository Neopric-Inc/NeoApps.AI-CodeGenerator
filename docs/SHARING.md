# Sharing: share links, invite links, email sign-in

All built into every generated app (template `Controllers/SharesController.cs`, `Controllers/AccessController.Invites.cs`,
`Controllers/AccessController.Email.cs`; UI `ShareDialog.tsx`, `SharedLinkPage.tsx`, `admin/InviteLinks.tsx`, `JoinPage.tsx`, `Login.tsx`).

## Share links (like Google Forms)

- **Share form** on a list page (needs create rights): pick fields; "Anyone with the link" or "Only people signed in to this app";
  expiry date; maximum responses; open/closed; new link (the old one stops); delete. Reference fields take a fixed value for every
  response (e.g. the job an application is for).
  - `showFields`: for a reference with a fixed value, the linked record's chosen fields are shown above the form, with its files
    as downloads (5-minute signed links). E.g. the job's title, location, description and job description document.
  - `allowFiles`: people attach files (PDF, Word, text, images; 10 MB; 3 per response). The submit answer carries a 30-minute
    HMAC key for that one new record; files are stored and indexed like in-app uploads (list the table in `ai.documentTables`
    to see them on the record page; AI workflows and the assistant read them).
- **Share** on a record (needs update rights): a link to change only the chosen fields of that record.
- Security: a link acts as its creator *at the moment of use* (stops when they lose the right or the account), only on the chosen
  table, record and fields (key, audit, tenant, hidden and unfixed reference columns can't be shared). Token =
  `<id>.<HMAC(app secret, random nonce)>`. Writes go through `DataGuard.NonQuery` (validations, lifecycles, audit, notifications),
  shown as "<creator> via share link"; the creator is notified (the change came from a visitor). Per-IP rate limit, honeypot field.
- `"sharing": false` in ui.config.json turns it off.
- API: `GET/POST /v1/api/Shares`, `PUT/DELETE /Shares/{id}`, `POST /Shares/{id}/regenerate`; public `GET/POST /public/share/{token}`,
  `GET /public/share/{token}/file/{fileId}`, `POST /public/share/{token}/files` (multipart: file, key). Page `/f/<token>`.

## Invite links

- Members → **Invite link** (administrators): a role (and tenant with tenancy), optional note, how many people (default 1),
  expiry (default a week); on/off, new link, delete. Administrator and Public roles can't be given by link.
- Without emails: people open `/join/<token>`, choose a username and password, and join with the role.
- With emails (when the app can send email): each address gets its own single-use link by email; opening it creates the account
  (username = email, no usable password) and signs them in.
- API: `GET/POST /v1/api/Access/invites` (`emails` for email invites), `PUT /Access/invites/{id}` (`is_open`, `regenerate`),
  `DELETE`; public `GET/POST /public/invite/{token}`.

## Sign in with an emailed code

- The sign-in page offers "Sign in with an email code instead" when the app can send email (NeoApps Cloud apps through the
  NeoApps relay; self-hosted with `Notifications:Email:*` and `Notifications:AppUrl`).
- 6 digits, 10 minutes, 5 tries, only the newest works, 5 codes per email per hour; stored as HMACs; the answer never says
  whether an email belongs to a member; deactivated members get nothing.
- Members have a sign-in email when they joined by email invite, or an administrator sets it in Members.
- API: `GET /public/email-code/available`, `POST /public/email-code`, `POST /public/email-code/verify`,
  `PUT /Access/members/{id}/email`. Tables: `member_invites`, `user_emails`, `login_codes`, `share_links` (built-in names).
