#!/usr/bin/env bash
# Setup for an existing clone: same as install.sh, but uses this checkout instead of ~/.neoapps and doesn't pull.
# Builds the CLI, puts `neoapps` on PATH, and connects the MCP server to Claude Code / Claude Desktop.
#
#   ./scripts/setup.sh            # build + register
#   ./scripts/setup.sh --infra    # ...and start the shared Docker services now
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
exec bash "$ROOT/install.sh" --dir "$ROOT" --no-update "$@"
