#!/bin/bash

# Function to cleanup services and child processes
cleanup() {
    echo ""
    echo "Stopping services..."
    
    # Kill the background React client if it's running
    if [ -n "$PID_CLIENT" ]; then
        kill $PID_CLIENT 2>/dev/null
    fi

    # Stop Docker services
    docker-compose down
}

# Trap exit signals to ensure cleanup runs
trap cleanup EXIT INT TERM

# Start fresh
echo "Cleaning up any old Docker resources..."
docker-compose down --remove-orphans 2>/dev/null

# Clean up local React client if running
lsof -ti:5173 | xargs kill -9 2>/dev/null || true

echo "Starting System via Docker Compose..."
echo "  - PostgreSQL, RabbitMQ, MongoDB"
echo "  - Order, Stock, Notification APIs"
echo "  - Prometheus, Grafana"
docker-compose up -d --build

echo "Waiting for services to be ready..."
sleep 10

echo "Starting React Client..."
echo "Client run on: http://localhost:5173"
cd src/client

# Ensure dependencies are installed
if [ ! -d "node_modules" ]; then
    echo "Installing client dependencies..."
    npm install
fi

npm run dev &
PID_CLIENT=$!
cd ../..

echo "=================================================="
echo " SYSTEM READY"
echo "=================================================="
echo " Grafana:      http://localhost:3000"
echo " Prometheus:   http://localhost:9091"
echo " RabbitMQ:     http://localhost:15672"
echo " React App:    http://localhost:5173"
echo " Order API:    http://localhost:5001/swagger"
echo " Stock API:    http://localhost:5002/swagger"
echo " Notif API:    http://localhost:5003/swagger"
echo "=================================================="
echo "Press Enter to stop..."
read

# The trap will handle the cleanup when the script exits after 'read'
