#!/bin/bash

# Function to check if a command exists
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# Check for required commands
for cmd in docker streamlit dotnet sed; do
    if ! command_exists $cmd; then
        echo "Error: $cmd is not installed. Please install it and try again."
        exit 1
    fi
done

# Print initial directory contents
echo "Current directory contents:"
ls -la
echo "-------------------------"
cd ..
cd Prerequisites
# Check if .env file exists
if [ ! -f ".env" ]; then
    echo "Error: .env file not found in the current directory."
    echo "Please make sure you're in the correct directory and the .env file exists."
    exit 1
fi

# Start Redis
echo "Starting Redis..."
docker run -d --name redis -p 6379:6379 --env-file .env \
-v redis-data:/data redis:latest \
redis-server --requirepass "$(grep REDIS_PASSWORD .env | cut -d '=' -f2)" --appendonly yes

# Start RabbitMQ
echo "Starting RabbitMQ..."
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 --env-file .env \
-v rabbitmq-data:/var/lib/rabbitmq rabbitmq:management

cd ..
cd CodeGenerator

# Check if app.py exists
if [ ! -f "app.py" ]; then
    echo "Error: app.py not found in the current directory."
    echo "Please make sure you're in the correct directory and app.py exists."
    exit 1
fi

# Run Streamlit app
echo "Running Streamlit app..."
streamlit run app.py &

# Wait for user input
echo "Please use the Streamlit app to enter details and upload the .sql file."
echo "Once you've submitted the form, press Enter to continue."
read -p "Press Enter to continue..."

