#!/usr/bin/env bash
# One-time setup on macOS/Linux: builds the NeoApps MCP server, registers it with Claude Desktop,
# and (optionally) starts the shared MySQL/Redis/RabbitMQ containers.
#
#   ./scripts/setup.sh            # build + register with Claude Desktop
#   ./scripts/setup.sh --infra    # ...and start the Docker services now
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
INSTALL_DIR="${NEOAPPS_MCP_DIR:-$HOME/.neoapps-mcp}"
ok()   { printf '\033[32m✔\033[0m %s\n' "$*"; }
warn() { printf '\033[33m!\033[0m %s\n' "$*"; }
die()  { printf '\033[31m✘\033[0m %s\n' "$*" >&2; exit 1; }

# 1. Find a dotnet that has the .NET 10 SDK (Homebrew/older installs can shadow it on PATH).
DOTNET=""
for c in /usr/local/share/dotnet/dotnet /opt/homebrew/bin/dotnet "$HOME/.dotnet/dotnet" "$(command -v dotnet || true)"; do
  [[ -n "$c" && -x "$c" ]] || continue
  if "$c" --list-sdks 2>/dev/null | grep -q '^10\.'; then DOTNET="$c"; break; fi
done
[[ -n "$DOTNET" ]] || die ".NET 10 SDK not found. macOS: brew install --cask dotnet-sdk  (others: https://dotnet.microsoft.com/download/dotnet/10.0)"
DOTNET_ROOT="$(cd "$(dirname "$(readlink -f "$DOTNET" 2>/dev/null || echo "$DOTNET")")" && pwd)"
ok ".NET 10 SDK: $DOTNET"

# 2. Docker (needed to run the shared services and the generated apps).
if command -v docker >/dev/null && docker info >/dev/null 2>&1; then
  ok "Docker is running"
else
  warn "Docker is not running. Install/start Docker Desktop before generating apps."
fi

# 3. Build the MCP server / CLI.
"$DOTNET" publish "$ROOT/src/NeoApps.Mcp" -c Release -o "$INSTALL_DIR" -nologo -v:q >/dev/null
ok "MCP server published to $INSTALL_DIR"

# 4. Register with Claude Desktop.
case "$(uname -s)" in
  Darwin) CFG="$HOME/Library/Application Support/Claude/claude_desktop_config.json" ;;
  *)      CFG="${XDG_CONFIG_HOME:-$HOME/.config}/Claude/claude_desktop_config.json" ;;
esac
PATH_FOR_APP="$DOTNET_ROOT:/usr/local/bin:/opt/homebrew/bin:/usr/bin:/bin"
if command -v python3 >/dev/null; then
  mkdir -p "$(dirname "$CFG")"
  python3 - "$CFG" "$DOTNET" "$INSTALL_DIR/neoapps-mcp.dll" "$ROOT" "$DOTNET_ROOT" "$PATH_FOR_APP" <<'PY'
import json, os, sys
cfg, dotnet, dll, home, droot, path = sys.argv[1:]
data = {}
if os.path.exists(cfg) and os.path.getsize(cfg) > 0:
    with open(cfg) as f:
        data = json.load(f)
data.setdefault("mcpServers", {})["neoapps"] = {
    "command": dotnet,
    "args": [dll],
    "env": {"NEOAPPS_HOME": home, "DOTNET_ROOT": droot, "PATH": path},
}
with open(cfg, "w") as f:
    json.dump(data, f, indent=2)
PY
  ok "Registered 'neoapps' in $CFG"
else
  warn "python3 not found; add this to $CFG by hand:"
  cat <<JSON
{ "mcpServers": { "neoapps": { "command": "$DOTNET", "args": ["$INSTALL_DIR/neoapps-mcp.dll"],
  "env": { "NEOAPPS_HOME": "$ROOT", "DOTNET_ROOT": "$DOTNET_ROOT", "PATH": "$PATH_FOR_APP" } } } }
JSON
fi

# 5. Optionally start the shared services now (otherwise they start on first use).
if [[ "${1:-}" == "--infra" ]]; then
  docker compose -f "$ROOT/deploy/docker-compose.infra.yml" -p neoapps-infra up -d --wait
  ok "MySQL (3307), Redis (6379) and RabbitMQ (5672, UI 15672) are running"
fi

cat <<EOF

Done. Next:
  • Restart Claude Desktop, then ask:  "Build me a todo app and run it locally."
  • Or use the CLI:  ./neoapps new TodoApp --schema samples/todo-app.sql --deploy
EOF
