#!/bin/bash
# Script para verificar o status de todos os serviços
# Execute: ./scripts/check-services.sh [--detailed]

DETAILED=false

if [ "$1" = "--detailed" ]; then
    DETAILED=true
fi

echo -e "\033[36m🔍 Checking PEO Platform Services Status\033[0m"
echo "========================================"
echo ""

# Lista de serviços
declare -A SERVICES
SERVICES["SQL Server"]="peo-sqlserver:1433:"
SERVICES["RabbitMQ"]="peo-rabbitmq:5672:"
SERVICES["Identity API"]="peo-identity-api:5001:http://localhost:5001/health"
SERVICES["Gestão Conteúdo API"]="peo-gestao-conteudo-api:5002:http://localhost:5002/health"
SERVICES["Gestão Alunos API"]="peo-gestao-alunos-api:5003:http://localhost:5003/health"
SERVICES["Faturamento API"]="peo-faturamento-api:5004:http://localhost:5004/health"
SERVICES["BFF"]="peo-bff:5000:http://localhost:5000/health"
SERVICES["SPA"]="peo-spa:8081:http://localhost:8081/health"

ALL_HEALTHY=true

for SERVICE_NAME in "${!SERVICES[@]}"; do
    IFS=':' read -r CONTAINER PORT HEALTH_URL <<< "${SERVICES[$SERVICE_NAME]}"

    # Check if container exists
    EXISTS=$(docker ps -a --filter "name=$CONTAINER" --format "{{.Names}}" 2>/dev/null)

    if [ -z "$EXISTS" ]; then
        echo -e "\033[31m❌ $SERVICE_NAME\033[0m - Container not found"
        ALL_HEALTHY=false
        continue
    fi

    # Check if container is running
    STATUS=$(docker inspect --format='{{.State.Status}}' "$CONTAINER" 2>/dev/null)

    if [ "$STATUS" != "running" ]; then
        echo -e "\033[33m⚠️  $SERVICE_NAME\033[0m - Status: $STATUS"
        ALL_HEALTHY=false
        continue
    fi

    # Check health
    HEALTH=$(docker inspect --format='{{.State.Health.Status}}' "$CONTAINER" 2>/dev/null)

    if [ "$HEALTH" = "healthy" ]; then
        echo -e "\033[32m✅ $SERVICE_NAME\033[0m - Healthy (Port: $PORT)"

        # Test HTTP endpoint if available and detailed mode
        if [ "$DETAILED" = true ] && [ -n "$HEALTH_URL" ]; then
            HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" "$HEALTH_URL" 2>/dev/null)
            if [ "$HTTP_CODE" = "200" ]; then
                echo -e "   └─ HTTP health check: OK"
            else
                echo -e "   └─ HTTP health check: Failed (HTTP $HTTP_CODE)"
            fi
        fi
    elif [ "$HEALTH" = "starting" ]; then
        echo -e "\033[33m⏳ $SERVICE_NAME\033[0m - Starting..."
        ALL_HEALTHY=false
    elif [ -n "$HEALTH" ]; then
        echo -e "\033[31m❌ $SERVICE_NAME\033[0m - Unhealthy: $HEALTH"
        ALL_HEALTHY=false
    else
        # No health check configured
        echo -e "\033[36m✓  $SERVICE_NAME\033[0m - Running (no health check)"
    fi
done

echo ""
echo "========================================"

if [ "$ALL_HEALTHY" = true ]; then
    echo -e "\033[32m✅ All services are healthy!\033[0m"
else
    echo -e "\033[33m⚠️  Some services are not healthy\033[0m"
    echo ""
    echo "Run 'docker-compose logs <service>' to view logs"
fi

echo ""

# Show quick access URLs
echo -e "\033[36m🔗 Quick Access URLs:\033[0m"
echo "  - SPA:              http://localhost:8081"
echo "  - BFF:              http://localhost:5000"
echo "  - Identity API:     http://localhost:5001"
echo "  - Gestão Conteúdo:  http://localhost:5002"
echo "  - Gestão Alunos:    http://localhost:5003"
echo "  - Faturamento:      http://localhost:5004"
echo "  - RabbitMQ UI:      http://localhost:15672 (peo / Peo@2025!)"
echo ""

# Show resource usage if detailed
if [ "$DETAILED" = true ]; then
    echo -e "\033[36m📊 Resource Usage:\033[0m"
    docker stats --no-stream --format "table {{.Name}}\t{{.CPUPerc}}\t{{.MemUsage}}\t{{.NetIO}}"
    echo ""
fi
