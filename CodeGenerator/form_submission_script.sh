#!/bin/bash

# This script expects the submitted response as a command-line argument
submitted_response="$1"

# Check if Properties/launchSettings.json exists
if [ ! -f "Properties/launchSettings.json" ]; then
    echo "Error: Properties/launchSettings.json not found."
    echo "Current directory contents:"
    ls -la
    echo "Properties folder contents (if it exists):"
    ls -la Properties 2>/dev/null || echo "Properties folder not found"
    exit 1
fi

# Update launchSettings.json using sed
echo "Updating launchSettings.json..."
sed -i.bak 's|"PARAMETER": ".*"|"PARAMETER": "'"$submitted_response"'"|' Properties/launchSettings.json

# Check if the update was successful
if [ $? -eq 0 ]; then
    echo "launchSettings.json updated successfully."
else
    echo "Error updating launchSettings.json"
    exit 1
fi

# Display the updated contents of launchSettings.json
echo "Updated launchSettings.json contents:"
cat Properties/launchSettings.json

# Run dotnet
echo "Running dotnet..."
dotnet run