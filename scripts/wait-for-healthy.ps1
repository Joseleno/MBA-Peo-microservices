# Script para aguardar todos os serviços ficarem healthy
# Execute: .\scripts\wait-for-healthy.ps1

param(
    [int]$TimeoutSeconds = 300,  # 5 minutos
    [int]$CheckIntervalSeconds = 5
)

$ErrorActionPreference = "Continue"

Write-Host "🏥 Waiting for all services to become healthy..." -ForegroundColor Cyan
Write-Host "Timeout: $TimeoutSeconds seconds" -ForegroundColor Yellow
Write-Host ""

$services = @(
    "peo-sqlserver",
    "peo-rabbitmq",
    "peo-identity-api",
    "peo-gestao-conteudo-api",
    "peo-gestao-alunos-api",
    "peo-faturamento-api",
    "peo-bff",
    "peo-spa"
)

$startTime = Get-Date
$allHealthy = $false

while (-not $allHealthy) {
    $elapsed = (Get-Date) - $startTime

    if ($elapsed.TotalSeconds -gt $TimeoutSeconds) {
        Write-Host "❌ Timeout reached! Not all services became healthy within $TimeoutSeconds seconds" -ForegroundColor Red
        Write-Host ""
        Write-Host "Current status:" -ForegroundColor Yellow
        docker-compose ps
        exit 1
    }

    $unhealthyServices = @()

    foreach ($service in $services) {
        $health = docker inspect --format='{{.State.Health.Status}}' $service 2>$null

        if (-not $health) {
            # Service doesn't exist or doesn't have health check
            $status = docker inspect --format='{{.State.Status}}' $service 2>$null
            if ($status -eq "running") {
                Write-Host "✓ $service : running (no health check)" -ForegroundColor Green
            } else {
                Write-Host "⏳ $service : $status" -ForegroundColor Yellow
                $unhealthyServices += $service
            }
        }
        elseif ($health -eq "healthy") {
            Write-Host "✓ $service : healthy" -ForegroundColor Green
        }
        elseif ($health -eq "starting") {
            Write-Host "⏳ $service : starting..." -ForegroundColor Yellow
            $unhealthyServices += $service
        }
        else {
            Write-Host "⚠ $service : $health" -ForegroundColor Red
            $unhealthyServices += $service
        }
    }

    if ($unhealthyServices.Count -eq 0) {
        $allHealthy = $true
    }
    else {
        Write-Host ""
        Write-Host "Waiting for $($unhealthyServices.Count) service(s)... (elapsed: $([math]::Round($elapsed.TotalSeconds))s)" -ForegroundColor Cyan
        Write-Host ""
        Start-Sleep -Seconds $CheckIntervalSeconds
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "✓ All services are healthy!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "🚀 You can now access:" -ForegroundColor Cyan
Write-Host "  - SPA:              http://localhost:8081" -ForegroundColor White
Write-Host "  - BFF:              http://localhost:5000" -ForegroundColor White
Write-Host "  - Identity API:     http://localhost:5001" -ForegroundColor White
Write-Host "  - RabbitMQ UI:      http://localhost:15672 (user: peo, pass: Peo@2025!)" -ForegroundColor White
Write-Host ""
