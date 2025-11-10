# Script para verificar o status de todos os serviços
# Execute: .\scripts\check-services.ps1

param(
    [switch]$Detailed
)

Write-Host "🔍 Checking PEO Platform Services Status" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Lista de serviços
$services = @(
    @{Name="SQL Server"; Container="peo-sqlserver"; Port=1433},
    @{Name="RabbitMQ"; Container="peo-rabbitmq"; Port=5672},
    @{Name="Identity API"; Container="peo-identity-api"; Port=5001; HealthUrl="http://localhost:5001/health"},
    @{Name="Gestão Conteúdo API"; Container="peo-gestao-conteudo-api"; Port=5002; HealthUrl="http://localhost:5002/health"},
    @{Name="Gestão Alunos API"; Container="peo-gestao-alunos-api"; Port=5003; HealthUrl="http://localhost:5003/health"},
    @{Name="Faturamento API"; Container="peo-faturamento-api"; Port=5004; HealthUrl="http://localhost:5004/health"},
    @{Name="BFF"; Container="peo-bff"; Port=5000; HealthUrl="http://localhost:5000/health"},
    @{Name="SPA"; Container="peo-spa"; Port=8081; HealthUrl="http://localhost:8081/health"}
)

$allHealthy = $true

foreach ($service in $services) {
    $containerName = $service.Container
    $serviceName = $service.Name

    # Check if container exists
    $exists = docker ps -a --filter "name=$containerName" --format "{{.Names}}" 2>$null

    if (-not $exists) {
        Write-Host "❌ $serviceName" -NoNewline -ForegroundColor Red
        Write-Host " - Container not found" -ForegroundColor Gray
        $allHealthy = $false
        continue
    }

    # Check if container is running
    $status = docker inspect --format='{{.State.Status}}' $containerName 2>$null

    if ($status -ne "running") {
        Write-Host "⚠️  $serviceName" -NoNewline -ForegroundColor Yellow
        Write-Host " - Status: $status" -ForegroundColor Gray
        $allHealthy = $false
        continue
    }

    # Check health
    $health = docker inspect --format='{{.State.Health.Status}}' $containerName 2>$null

    if ($health -eq "healthy") {
        Write-Host "✅ $serviceName" -NoNewline -ForegroundColor Green
        Write-Host " - Healthy (Port: $($service.Port))" -ForegroundColor Gray

        # Test HTTP endpoint if available
        if ($Detailed -and $service.HealthUrl) {
            try {
                $response = Invoke-WebRequest -Uri $service.HealthUrl -TimeoutSec 2 -ErrorAction SilentlyContinue
                if ($response.StatusCode -eq 200) {
                    Write-Host "   └─ HTTP health check: OK" -ForegroundColor DarkGreen
                }
            } catch {
                Write-Host "   └─ HTTP health check: Failed" -ForegroundColor DarkYellow
            }
        }
    }
    elseif ($health -eq "starting") {
        Write-Host "⏳ $serviceName" -NoNewline -ForegroundColor Yellow
        Write-Host " - Starting..." -ForegroundColor Gray
        $allHealthy = $false
    }
    elseif ($health) {
        Write-Host "❌ $serviceName" -NoNewline -ForegroundColor Red
        Write-Host " - Unhealthy: $health" -ForegroundColor Gray
        $allHealthy = $false
    }
    else {
        # No health check configured
        Write-Host "✓  $serviceName" -NoNewline -ForegroundColor Cyan
        Write-Host " - Running (no health check)" -ForegroundColor Gray
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan

if ($allHealthy) {
    Write-Host "✅ All services are healthy!" -ForegroundColor Green
} else {
    Write-Host "⚠️  Some services are not healthy" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Run 'docker-compose logs <service>' to view logs" -ForegroundColor Gray
}

Write-Host ""

# Show quick access URLs
Write-Host "🔗 Quick Access URLs:" -ForegroundColor Cyan
Write-Host "  - SPA:              http://localhost:8081" -ForegroundColor White
Write-Host "  - BFF:              http://localhost:5000" -ForegroundColor White
Write-Host "  - Identity API:     http://localhost:5001" -ForegroundColor White
Write-Host "  - Gestão Conteúdo:  http://localhost:5002" -ForegroundColor White
Write-Host "  - Gestão Alunos:    http://localhost:5003" -ForegroundColor White
Write-Host "  - Faturamento:      http://localhost:5004" -ForegroundColor White
Write-Host "  - RabbitMQ UI:      http://localhost:15672 (peo / Peo@2025!)" -ForegroundColor White
Write-Host ""

# Show resource usage if detailed
if ($Detailed) {
    Write-Host "📊 Resource Usage:" -ForegroundColor Cyan
    docker stats --no-stream --format "table {{.Name}}\t{{.CPUPerc}}\t{{.MemUsage}}\t{{.NetIO}}"
    Write-Host ""
}
