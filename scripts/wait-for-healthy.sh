#!/bin/bash
# Script para aguardar todos os serviços ficarem healthy
# Execute: ./scripts/wait-for-healthy.sh

set +e

TIMEOUT_SECONDS=${1:-300}  # Default: 5 minutos
CHECK_INTERVAL=${2:-5}     # Default: 5 segundos

echo -e "\033[36m🏥 Waiting for all services to become healthy...\033[0m"
echo -e "\033[33mTimeout: $TIMEOUT_SECONDS seconds\033[0m"
echo ""

SERVICES=(
    "peo-sqlserver"
    "peo-rabbitmq"
    "peo-identity-api"
    "peo-gestao-conteudo-api"
    "peo-gestao-alunos-api"
    "peo-faturamento-api"
    "peo-bff"
    "peo-spa"
)

START_TIME=$(date +%s)
ALL_HEALTHY=false

while [ "$ALL_HEALTHY" = false ]; do
    CURRENT_TIME=$(date +%s)
    ELAPSED=$((CURRENT_TIME - START_TIME))

    if [ $ELAPSED -gt $TIMEOUT_SECONDS ]; then
        echo -e "\033[31m❌ Timeout reached! Not all services became healthy within $TIMEOUT_SECONDS seconds\033[0m"
        echo ""
        echo -e "\033[33mCurrent status:\033[0m"
        docker-compose ps
        exit 1
    fi

    UNHEALTHY_COUNT=0

    for SERVICE in "${SERVICES[@]}"; do
        HEALTH=$(docker inspect --format='{{.State.Health.Status}}' "$SERVICE" 2>/dev/null)

        if [ -z "$HEALTH" ]; then
            # Service doesn't exist or doesn't have health check
            STATUS=$(docker inspect --format='{{.State.Status}}' "$SERVICE" 2>/dev/null)
            if [ "$STATUS" = "running" ]; then
                echo -e "\033[32m✓ $SERVICE : running (no health check)\033[0m"
            else
                echo -e "\033[33m⏳ $SERVICE : $STATUS\033[0m"
                UNHEALTHY_COUNT=$((UNHEALTHY_COUNT + 1))
            fi
        elif [ "$HEALTH" = "healthy" ]; then
            echo -e "\033[32m✓ $SERVICE : healthy\033[0m"
        elif [ "$HEALTH" = "starting" ]; then
            echo -e "\033[33m⏳ $SERVICE : starting...\033[0m"
            UNHEALTHY_COUNT=$((UNHEALTHY_COUNT + 1))
        else
            echo -e "\033[31m⚠ $SERVICE : $HEALTH\033[0m"
            UNHEALTHY_COUNT=$((UNHEALTHY_COUNT + 1))
        fi
    done

    if [ $UNHEALTHY_COUNT -eq 0 ]; then
        ALL_HEALTHY=true
    else
        echo ""
        echo -e "\033[36mWaiting for $UNHEALTHY_COUNT service(s)... (elapsed: ${ELAPSED}s)\033[0m"
        echo ""
        sleep $CHECK_INTERVAL
    fi
done

echo ""
echo -e "\033[32m========================================"
echo "✓ All services are healthy!"
echo "========================================\033[0m"
echo ""
echo -e "\033[36m🚀 You can now access:\033[0m"
echo "  - SPA:              http://localhost:8081"
echo "  - BFF:              http://localhost:5000"
echo "  - Identity API:     http://localhost:5001"
echo "  - RabbitMQ UI:      http://localhost:15672 (user: peo, pass: Peo@2025!)"
echo ""
