#!/bin/bash
set -e

echo "========================================="
echo "  MentorBooking - DigitalOcean Deploy"
echo "========================================="

if [ ! -f .env.production ]; then
    echo "[ERROR] .env.production not found!"
    echo "Copy .env.production.example to .env.production and fill in your values."
    exit 1
fi

echo "[1/4] Pulling latest code..."
git pull origin "$(git branch --show-current)"

echo "[2/4] Building Docker images..."
docker compose -f docker-compose.production.yml --env-file .env.production build

echo "[3/4] Starting services..."
docker compose -f docker-compose.production.yml --env-file .env.production up -d

echo "[4/4] Checking service health..."
sleep 10
docker compose -f docker-compose.production.yml ps

echo ""
echo "========================================="
echo "  Deploy complete!"
echo "  Gateway: http://$(curl -s ifconfig.me):80"
echo "  Swagger: http://$(curl -s ifconfig.me)/swagger"
echo "========================================="
