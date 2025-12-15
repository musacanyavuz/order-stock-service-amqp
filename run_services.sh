#!/bin/bash

# Kill background processes on exit
trap "kill 0" EXIT


# Kill any existing processes on ports
echo "Cleaning up old processes..."
lsof -ti:5001,5002,5003,3000,5173 | xargs kill -9 2>/dev/null || true

echo "Ensuring infrastructure is up..."
docker-compose up -d
echo "Waiting for infrastructure to be ready..."
sleep 15 # Wait for DBs and RabbitMQ to be fully ready

echo "Starting Order API..."
dotnet run --project src/Order.API/Order.API.csproj --urls "http://localhost:5001" &
PID_ORDER=$!

echo "Starting Stock API..."
dotnet run --project src/Stock.API/Stock.API.csproj --urls "http://localhost:5002" &
PID_STOCK=$!

echo "Starting Notification API..."
dotnet run --project src/Notification.API/Notification.API.csproj --urls "http://localhost:5003" &
PID_NOTIF=$!

echo "Starting React Client..."
cd src/client && npm run dev &
PID_CLIENT=$!

echo "All services started. Press Enter to stop..."
read

echo "Stopping services..."
