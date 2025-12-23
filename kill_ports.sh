#!/bin/bash
echo "Stopping Docker containers and removing volumes (RESET ALL DATA)..."
if docker info > /dev/null 2>&1; then
    docker-compose down --volumes
else
    echo "Docker not running, skipping container cleanup."
fi

echo "Killing processes on ports: 5001, 5002, 5003, 3000, 9091, 5173..."
lsof -ti:5001,5002,5003,3000,9091,5173 | xargs kill -9 2>/dev/null || true
echo "Cleanup complete. All data has been wiped."
