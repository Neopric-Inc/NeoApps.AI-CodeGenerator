#!/usr/bin/env bash
# NeoApps.AI installer (macOS / Linux / WSL).
#
#   curl -fsSL https://raw.githubusercontent.com/Neopric-Inc/NeoApps.AI-CodeGenerator/main/install.sh | bash
#
# What it does:
#   1. checks for git, the .NET 10 SDK and Docker
#   2. downloads the generator to ~/.neoapps (or updates it: fast-forward only, never discards local changes)
#   3. builds the CLI and puts `neoapps` on your PATH (~/.local/bin)
#   4. connects the NeoApps MCP server to Claude Code and Claude Desktop when they are installed
#
# Options (or env vars):
#   --dir <path>     where the generator lives          (NEOAPPS_DIR, default ~/.neoapps)
#   --bin <path>     where the `neoapps` command goes   (NEOAPPS_BIN, default ~/.local/bin)
#   --ref <branch>   branch or tag to install           (NEOAPPS_REF, default main)
#   --no-mcp         don't register the MCP server with Claude
#   --no-update      don't pull an existing install
#   --infra          also start the shared MySQL / Redis / RabbitMQ containers now
#
# Re-run it any time to update.
set -euo pipefail

REPO_URL="${NEOAPPS_REPO:-https://github.com/Neopric-Inc/NeoApps.AI-CodeGenerator.git}"
DIR="${NEOAPPS_DIR:-$HOME/.neoapps}"
BIN="${NEOAPPS_BIN:-$HOME/.local/bin}"
REF="${NEOAPPS_REF:-main}"
MCP=1; UPDATE=1; INFRA=0
[[ "${NEOAPPS_NO_UPDATE:-}" == 1 ]] && UPDATE=0
while [[ $# -gt 0 ]]; do
  case "$1" in
    --dir) DIR="$2"; shift 2 ;;
    --bin) BIN="$2"; shift 2 ;;
    --ref) REF="$2"; shift 2 ;;
    --no-mcp) MCP=0; shift ;;
    --no-update) UPDATE=0; shift ;;
    --infra) INFRA=1; shift ;;
    -h|--help) sed -n '2,22p' "${BASH_SOURCE[0]:-/dev/null}" 2>/dev/null || true; exit 0 ;;
    *) echo "Unknown option: $1" >&2; exit 2 ;;
  esac
done

ok()   { printf '\033[32m✔\033[0m %s\n' "$*"; }
warn() { printf '\033[33m!\033[0m %s\n' "$*"; }
die()  { printf '\033[31m✘\033[0m %s\n' "$*" >&2; exit 1; }

# 1. Prerequisites ------------------------------------------------------------------------------
command -v git >/dev/null || die "git is required. macOS: xcode-select --install   Debian/Ubuntu: sudo apt install git"

DOTNET=""
for c in "$(command -v dotnet || true)" /usr/local/share/dotnet/dotnet /opt/homebrew/bin/dotnet "$HOME/.dotnet/dotnet" /usr/lib/dotnet/dotnet /usr/share/dotnet/dotnet; do
  [[ -n "$c" && -x "$c" ]] || continue
  if "$c" --list-sdks 2>/dev/null | grep -q '^10\.'; then DOTNET="$c"; break; fi
done
[[ -n "$DOTNET" ]] || die ".NET 10 SDK not found. macOS: brew install --cask dotnet-sdk   Others: https://dotnet.microsoft.com/download/dotnet/10.0"
DOTNET_ROOT="$(cd "$(dirname "$(readlink -f "$DOTNET" 2>/dev/null || echo "$DOTNET")")" && pwd)"
ok ".NET 10 SDK: $DOTNET"

if command -v docker >/dev/null && docker info >/dev/null 2>&1; then
  ok "Docker is running"
else
  warn "Docker is not running. Start Docker Desktop before generating apps (or see 'No Docker' in the README)."
fi

# 2. Download or update -------------------------------------------------------------------------
if [[ -d "$DIR/.git" ]]; then
  if [[ "$UPDATE" == 0 ]]; then
    ok "Using $DIR (not updated)"
  elif [[ -n "$(git -C "$DIR" status --porcelain --untracked-files=no)" ]]; then
    warn "$DIR has local changes, so it was not updated. Commit or stash them and re-run to update."
  else
    git -C "$DIR" fetch -q origin "$REF"
    if git -C "$DIR" merge -q --ff-only FETCH_HEAD 2>/dev/null; then
      ok "Updated $DIR to the latest $REF"
    else
      warn "$DIR has commits that aren't on $REF, so it was not updated."
    fi
  fi
elif [[ -e "$DIR" ]]; then
  die "$DIR exists but isn't a NeoApps checkout. Move it aside or pass --dir <other path>."
else
  git clone -q --depth 1 --branch "$REF" "$REPO_URL" "$DIR"
  ok "Downloaded the generator to $DIR"
fi
[[ -f "$DIR/CodeGenerator/CodeGenerator.csproj" || -d "$DIR/CodeGenerator" ]] || die "$DIR doesn't look like the NeoApps repository."

# 3. Build the CLI and put `neoapps` on PATH ----------------------------------------------------
"$DOTNET" build "$DIR/src/NeoApps.Mcp" -c Release -nologo -v:q >/dev/null || die "Building the NeoApps CLI failed. Run: $DOTNET build $DIR/src/NeoApps.Mcp -c Release"
DLL="$DIR/src/NeoApps.Mcp/bin/Release/net10.0/neoapps-mcp.dll"
[[ -f "$DLL" ]] || die "Build finished but $DLL is missing."
ok "Built the NeoApps CLI"

mkdir -p "$BIN"
cat > "$BIN/neoapps" <<EOF
#!/usr/bin/env bash
# Installed by NeoApps install.sh: runs the generator in $DIR from any folder.
export NEOAPPS_HOME="\${NEOAPPS_HOME:-$DIR}"
export DOTNET_ROOT="\${DOTNET_ROOT:-$DOTNET_ROOT}"
export PATH="$DOTNET_ROOT:\$PATH"
exec "\$NEOAPPS_HOME/neoapps" "\$@"
EOF
chmod +x "$BIN/neoapps"
ok "Installed the 'neoapps' command in $BIN"
case ":$PATH:" in
  *":$BIN:"*) ;;
  *) warn "$BIN is not on your PATH. Add this to ~/.zshrc or ~/.bashrc:  export PATH=\"$BIN:\$PATH\"" ;;
esac

# 4. Connect coding agents (MCP) -----------------------------------------------------------------
PATH_FOR_APP="$DOTNET_ROOT:/usr/local/bin:/opt/homebrew/bin:/usr/bin:/bin"
if [[ "$MCP" == 1 ]]; then
  # Claude Code
  if command -v claude >/dev/null; then
    claude mcp remove -s user neoapps >/dev/null 2>&1 || true
    if claude mcp add -s user neoapps -e NEOAPPS_HOME="$DIR" -e DOTNET_ROOT="$DOTNET_ROOT" -e PATH="$PATH_FOR_APP" -- "$DOTNET" "$DLL" >/dev/null 2>&1; then
      ok "Connected to Claude Code (MCP server 'neoapps')"
    else
      warn "Couldn't register with Claude Code. Run: claude mcp add -s user neoapps -e NEOAPPS_HOME=$DIR -- $DOTNET $DLL"
    fi
  fi
  # Claude Desktop
  case "$(uname -s)" in
    Darwin) CFG="$HOME/Library/Application Support/Claude/claude_desktop_config.json" ;;
    *)      CFG="${XDG_CONFIG_HOME:-$HOME/.config}/Claude/claude_desktop_config.json" ;;
  esac
  if [[ -d "$(dirname "$CFG")" ]]; then
    if command -v python3 >/dev/null; then
      python3 - "$CFG" "$DOTNET" "$DLL" "$DIR" "$DOTNET_ROOT" "$PATH_FOR_APP" <<'PY'
import json, os, sys
cfg, dotnet, dll, home, droot, path = sys.argv[1:]
data = {}
if os.path.exists(cfg) and os.path.getsize(cfg) > 0:
    with open(cfg) as f:
        data = json.load(f)
data.setdefault("mcpServers", {})["neoapps"] = {
    "command": dotnet, "args": [dll],
    "env": {"NEOAPPS_HOME": home, "DOTNET_ROOT": droot, "PATH": path},
}
with open(cfg, "w") as f:
    json.dump(data, f, indent=2)
PY
      ok "Connected to Claude Desktop (restart it to load the tools)"
    else
      warn "python3 not found; add the MCP snippet below to $CFG by hand."
    fi
  fi
fi

# 5. Optional: start shared services now ---------------------------------------------------------
if [[ "$INFRA" == 1 ]]; then
  docker compose -f "$DIR/deploy/docker-compose.infra.yml" -p neoapps-infra up -d --wait
  ok "MySQL (3307), Redis (6379) and RabbitMQ (5672, UI 15672) are running"
fi

cat <<EOF

Done. Try it:
  neoapps new TodoApp --schema $DIR/samples/todo-app.sql --deploy
  neoapps help

Other MCP clients (Cursor, Windsurf, VS Code...): add this server to their MCP config:
  { "mcpServers": { "neoapps": { "command": "$DOTNET", "args": ["$DLL"],
      "env": { "NEOAPPS_HOME": "$DIR", "DOTNET_ROOT": "$DOTNET_ROOT", "PATH": "$PATH_FOR_APP" } } } }
EOF
