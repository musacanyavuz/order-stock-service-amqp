#!/bin/bash
echo "Killing processes on ports: 5001, 5002, 5003, 3000, 9090, 5173..."
lsof -ti:5001,5002,5003,3000,9090,5173 | xargs kill -9 2>/dev/null || true

echo "Stopping Docker containers and removing volumes (RESET ALL DATA)..."
docker-compose down --volumes
echo "Cleanup complete. All data has been wiped."
