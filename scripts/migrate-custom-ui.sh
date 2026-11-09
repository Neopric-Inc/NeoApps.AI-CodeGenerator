#!/usr/bin/env bash
# Upgrades an app's custom screens (workspace/<App>/custom) to the current admin UI: React 19 + MUI 9.
# Runs MUI's codemods (style props -> sx, Grid item/xs -> size, TextField/ListItemText props -> slotProps)
# and renames icons that MUI removed. Re-run safe. Review the diff, then regenerate (neoapps evolve/frontend).
set -euo pipefail
dir="${1:?usage: scripts/migrate-custom-ui.sh workspace/<App>/custom}"
[ -d "$dir" ] || { echo "No such folder: $dir" >&2; exit 1; }
tmp="$(mktemp -d)"
trap 'rm -rf "$tmp"' EXIT
echo "Installing MUI codemods..."
npm --prefix "$tmp" install --silent --no-audit --no-fund @mui/codemod@9 >/dev/null
mods="$tmp/node_modules/@mui/codemod"
jscs="$tmp/node_modules/.bin/jscodeshift"
backup="$dir.before-ui-upgrade-$(date +%Y%m%d%H%M%S)"
cp -R "$dir" "$backup"
for t in deprecations/all v7.0.0/grid-props v9.0.0/system-props; do
  echo "== $t"
  "$jscs" --transform "$mods/$t/index.js" --extensions tsx,ts,jsx,js --parser tsx --ignore-pattern '**/node_modules/**' "$dir" | grep -E " (ok|errors)$" || true
done
# icons removed in @mui/icons-material 9
find "$dir" -type f \( -name '*.tsx' -o -name '*.ts' -o -name '*.jsx' -o -name '*.js' \) | while read -r f; do
  perl -pi -e 's#icons-material/(Delete|People|ChatBubble|Star|Error)Outline"#icons-material/$1OutlineOutlined"#g; s/(?<![\w.])JSX\.Element/React.JSX.Element/g' "$f"
done
echo "Done. Previous version kept in $backup"
