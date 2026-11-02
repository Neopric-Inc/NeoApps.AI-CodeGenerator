#!/usr/bin/env bash
# For Linux environments WITHOUT Docker (e.g. Claude Code cloud sessions, CI runners).
# Installs and starts MySQL 8, Redis and RabbitMQ with the same settings as deploy/docker-compose.infra.yml,
# so `./neoapps new ...` can generate and you can `dotnet run` the generated API.
#
#   sudo ./scripts/sandbox-deps.sh && export NEOAPPS_SKIP_INFRA=true NEOAPPS_MYSQL_PORT=3306
set -euo pipefail
export DEBIAN_FRONTEND=noninteractive

if ! command -v dotnet >/dev/null || ! dotnet --list-sdks | grep -q '^10\.'; then
  apt-get update -qq && apt-get install -y -qq dotnet-sdk-10.0 >/dev/null
fi
command -v mysqld >/dev/null || { apt-get update -qq; apt-get install -y -qq mysql-server-8.0 >/dev/null; }
command -v redis-server >/dev/null || apt-get install -y -qq redis-server >/dev/null
command -v rabbitmq-server >/dev/null || apt-get install -y -qq rabbitmq-server >/dev/null

# MySQL: case-insensitive table names (what the templates expect) — must be set at initialisation.
if ! mysqladmin ping -uroot -pneoapps --silent 2>/dev/null; then
  pkill mysqld 2>/dev/null || true; sleep 2
  if [[ "$(mysqld --verbose --help 2>/dev/null | awk '/^lower-case-table-names/{print $2}')" != "1" ]] || [[ ! -d /var/lib/mysql/mysql ]]; then
    rm -rf /var/lib/mysql/* && mysqld --initialize-insecure --user=mysql --lower-case-table-names=1
  fi
  (mysqld_safe --user=mysql --lower-case-table-names=1 --log-bin-trust-function-creators=1 >/tmp/mysqld.log 2>&1 &)
  for _ in $(seq 30); do mysqladmin ping -uroot --silent 2>/dev/null && break; sleep 1; done
  mysql -uroot -e "ALTER USER 'root'@'localhost' IDENTIFIED BY 'neoapps'" 2>/dev/null || true
fi

redis-cli ping >/dev/null 2>&1 || redis-server --daemonize yes >/dev/null
if ! rabbitmqctl status >/dev/null 2>&1; then
  rabbitmq-server -detached >/dev/null 2>&1; sleep 15
fi
rabbitmqctl add_user neoapps neoapps >/dev/null 2>&1 || true
rabbitmqctl set_permissions -p / neoapps ".*" ".*" ".*" >/dev/null 2>&1 || true

echo "MySQL 127.0.0.1:3306 (root/neoapps), Redis 6379, RabbitMQ 5672 (neoapps/neoapps) are running."
echo "Now: export NEOAPPS_SKIP_INFRA=true NEOAPPS_MYSQL_PORT=3306"
