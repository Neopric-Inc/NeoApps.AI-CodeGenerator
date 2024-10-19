#!/bin/bash

# Install Azure CLI
curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash

# Login to Azure (this will use the VM's managed identity)
az login --identity

# Get the NSG associated with the VM's network interface
RESOURCE_GROUP=$(az vm show --name $HOSTNAME --resource-group myResourceGroup --query "resourceGroup" -o tsv)
NIC_NAME=$(az vm show --name $HOSTNAME --resource-group $RESOURCE_GROUP --query "networkProfile.networkInterfaces[0].id" -o tsv | cut -d'/' -f9)
NSG_NAME=$(az network nic show --name $NIC_NAME --resource-group $RESOURCE_GROUP --query "networkSecurityGroup.id" -o tsv | cut -d'/' -f9)

# Add a rule to allow inbound traffic on port 8501
az network nsg rule create \
  --resource-group $RESOURCE_GROUP \
  --nsg-name $NSG_NAME \
  --name AllowStreamlit \
  --priority 1001 \
  --destination-port-ranges 8501 \
  --direction Inbound \
  --access Allow \
  --protocol Tcp \
  --description "Allow Streamlit"