#!/usr/bin/env bash
# Creates throwaway Azure resources for testing NeoApps on Azure: Azure SQL (free offer, serverless, auto-pause),
# Blob storage (container "documents") and Service Bus (queue "neoapps-jobs").
# Connection strings go to ~/secrets/neoapps-azure-test.env (chmod 600); nothing secret is printed.
#
#   az login                                   # once
#   ./scripts/azure/test-resources.sh          # create (or re-run: reuses what exists, refreshes your IP rule)
#   ./scripts/azure/test-resources.sh --delete # remove everything it created
#
# Options (env): LOCATION (default canadacentral), RG (default neoapps-test), SUBSCRIPTION (default: current).
set -euo pipefail

LOCATION="${LOCATION:-canadacentral}"
RG="${RG:-neoapps-test}"
ENV_FILE="$HOME/secrets/neoapps-azure-test.env"
TAGS="purpose=neoapps-test"

command -v az >/dev/null || { echo "Install the Azure CLI first: brew install azure-cli"; exit 1; }
az account show >/dev/null 2>&1 || { echo "Run 'az login' first."; exit 1; }
[ -n "${SUBSCRIPTION:-}" ] && az account set --subscription "$SUBSCRIPTION"
echo "Subscription: $(az account show --query name -o tsv)"

if [ "${1:-}" = "--delete" ]; then
  read -r -p "Delete resource group '$RG' and everything in it? Type the name to confirm: " ans
  [ "$ans" = "$RG" ] || { echo "Not deleted."; exit 1; }
  az group delete -n "$RG" --yes --no-wait
  rm -f "$ENV_FILE"
  echo "Deleting $RG in the background; removed $ENV_FILE."
  exit 0
fi

az group create -n "$RG" -l "$LOCATION" --tags $TAGS -o none
# One random suffix per resource group, kept as a tag so re-runs reuse the same names.
SUFFIX=$(az group show -n "$RG" --query "tags.suffix" -o tsv)
if [ -z "$SUFFIX" ] || [ "$SUFFIX" = "None" ]; then
  SUFFIX=$(LC_ALL=C tr -dc 'a-z0-9' </dev/urandom | head -c 6)
  az group update -n "$RG" --set tags.suffix="$SUFFIX" -o none
fi
SQL_SERVER="neoapps-sql-$SUFFIX"; SQL_DB="neoapps_test"; SQL_ADMIN="neoappsadmin"
STORAGE="neoappsst$SUFFIX"; BUS="neoapps-bus-$SUFFIX"

# --- Azure SQL -------------------------------------------------------------------------------
if az sql server show -g "$RG" -n "$SQL_SERVER" -o none 2>/dev/null; then
  echo "SQL server $SQL_SERVER exists."
  if [ -f "$ENV_FILE" ]; then SQL_PASSWORD=$(sed -n 's/^NEOAPPS_AZURESQL_PASSWORD=//p' "$ENV_FILE"); fi
  if [ -z "${SQL_PASSWORD:-}" ]; then
    SQL_PASSWORD="$(LC_ALL=C tr -dc 'A-Za-z0-9' </dev/urandom | head -c 28)Aa1-"
    az sql server update -g "$RG" -n "$SQL_SERVER" -p "$SQL_PASSWORD" -o none
    echo "Reset the admin password (env file was missing)."
  fi
else
  SQL_PASSWORD="$(LC_ALL=C tr -dc 'A-Za-z0-9' </dev/urandom | head -c 28)Aa1-"
  echo "Creating SQL server $SQL_SERVER (2-3 min)..."
  az sql server create -g "$RG" -n "$SQL_SERVER" -l "$LOCATION" -u "$SQL_ADMIN" -p "$SQL_PASSWORD" \
    --minimal-tls-version 1.2 -o none
fi

MYIP=$(curl -fsS https://api.ipify.org)
az sql server firewall-rule create -g "$RG" -s "$SQL_SERVER" -n my-ip --start-ip-address "$MYIP" --end-ip-address "$MYIP" -o none
echo "Firewall allows your IP $MYIP."

if ! az sql db show -g "$RG" -s "$SQL_SERVER" -n "$SQL_DB" -o none 2>/dev/null; then
  echo "Creating database $SQL_DB (free offer: pauses when idle, no charge within the monthly free limit)..."
  az sql db create -g "$RG" -s "$SQL_SERVER" -n "$SQL_DB" -e GeneralPurpose -f Gen5 -c 2 \
      --compute-model Serverless --auto-pause-delay 60 --use-free-limit --free-limit-exhaustion-behavior AutoPause --tags $TAGS -o none \
  || { echo "Free offer not available here; creating smallest serverless database (auto-pause after 1 hour)."
       az sql db create -g "$RG" -s "$SQL_SERVER" -n "$SQL_DB" -e GeneralPurpose -f Gen5 -c 1 --min-capacity 0.5 \
         --compute-model Serverless --auto-pause-delay 60 --backup-storage-redundancy Local --tags $TAGS -o none; }
fi

# --- Blob storage ----------------------------------------------------------------------------
az storage account show -g "$RG" -n "$STORAGE" -o none 2>/dev/null || {
  echo "Creating storage account $STORAGE..."
  az storage account create -g "$RG" -n "$STORAGE" -l "$LOCATION" --sku Standard_LRS --kind StorageV2 \
    --min-tls-version TLS1_2 --allow-blob-public-access false --tags $TAGS -o none; }
STORAGE_CS=$(az storage account show-connection-string -g "$RG" -n "$STORAGE" --query connectionString -o tsv)
az storage container create -n documents --connection-string "$STORAGE_CS" -o none

# --- Service Bus -----------------------------------------------------------------------------
az servicebus namespace show -g "$RG" -n "$BUS" -o none 2>/dev/null || {
  echo "Creating Service Bus $BUS..."
  az servicebus namespace create -g "$RG" -n "$BUS" -l "$LOCATION" --sku Basic --tags $TAGS -o none; }
az servicebus queue show -g "$RG" --namespace-name "$BUS" -n neoapps-jobs -o none 2>/dev/null \
  || az servicebus queue create -g "$RG" --namespace-name "$BUS" -n neoapps-jobs --max-delivery-count 5 -o none
BUS_CS=$(az servicebus namespace authorization-rule keys list -g "$RG" --namespace-name "$BUS" \
  -n RootManageSharedAccessKey --query primaryConnectionString -o tsv)

# --- Env file (secrets only here) --------------------------------------------------------------
mkdir -p "$HOME/secrets" && chmod 700 "$HOME/secrets"
umask 077
cat > "$ENV_FILE" <<EOF
# NeoApps Azure test resources (resource group $RG). Load with: set -a; . $ENV_FILE; set +a
NEOAPPS_DB_PROVIDER=azuresql
NEOAPPS_AZURESQL_SERVER=$SQL_SERVER.database.windows.net
NEOAPPS_AZURESQL_DATABASE=$SQL_DB
NEOAPPS_AZURESQL_USER=$SQL_ADMIN
NEOAPPS_AZURESQL_PASSWORD=$SQL_PASSWORD
NEOAPPS_AZURESQL="Server=tcp:$SQL_SERVER.database.windows.net,1433;Database=$SQL_DB;User ID=$SQL_ADMIN;Password=$SQL_PASSWORD;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30"
AZURE_STORAGE_CONNECTION_STRING="$STORAGE_CS"
AZURE_STORAGE_CONTAINER=documents
SERVICEBUS_CONNECTION_STRING="$BUS_CS"
SERVICEBUS_QUEUE=neoapps-jobs
EOF
chmod 600 "$ENV_FILE"

echo
echo "Done. Resource group: $RG ($LOCATION)"
echo "  SQL:     $SQL_SERVER.database.windows.net / $SQL_DB (admin $SQL_ADMIN)"
echo "  Storage: $STORAGE (container documents)"
echo "  Bus:     $BUS (queue neoapps-jobs)"
echo "Connection strings saved to $ENV_FILE (not shown). If your IP changes, run this script again."
echo "Remove everything later with: $0 --delete"
