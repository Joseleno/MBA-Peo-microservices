# Script para build de todas as imagens Docker (PowerShell)
# Execute na raiz do projeto: .\scripts\build-images.ps1

param(
    [string]$Version = "latest"
)

$ErrorActionPreference = "Stop"

Write-Host "🐋 Building Docker Images - PEO Platform" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "📦 Version: $Version" -ForegroundColor Yellow
Write-Host ""

# Build Identity API
Write-Host "Building Identity API..." -ForegroundColor Blue
docker build -f docker/Dockerfile.Identity -t peo-identity:$Version .
Write-Host "✓ Identity API built successfully" -ForegroundColor Green
Write-Host ""

# Build Gestão de Conteúdo API
Write-Host "Building Gestão de Conteúdo API..." -ForegroundColor Blue
docker build -f docker/Dockerfile.GestaoConteudo -t peo-gestao-conteudo:$Version .
Write-Host "✓ Gestão de Conteúdo API built successfully" -ForegroundColor Green
Write-Host ""

# Build Gestão de Alunos API
Write-Host "Building Gestão de Alunos API..." -ForegroundColor Blue
docker build -f docker/Dockerfile.GestaoAlunos -t peo-gestao-alunos:$Version .
Write-Host "✓ Gestão de Alunos API built successfully" -ForegroundColor Green
Write-Host ""

# Build Faturamento API
Write-Host "Building Faturamento API..." -ForegroundColor Blue
docker build -f docker/Dockerfile.Faturamento -t peo-faturamento:$Version .
Write-Host "✓ Faturamento API built successfully" -ForegroundColor Green
Write-Host ""

# Build BFF
Write-Host "Building BFF..." -ForegroundColor Blue
docker build -f docker/Dockerfile.Bff -t peo-bff:$Version .
Write-Host "✓ BFF built successfully" -ForegroundColor Green
Write-Host ""

# Build SPA
Write-Host "Building Blazor SPA..." -ForegroundColor Blue
docker build -f docker/Dockerfile.Spa -t peo-spa:$Version .
Write-Host "✓ Blazor SPA built successfully" -ForegroundColor Green
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "✓ All images built successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "📋 Built images:" -ForegroundColor Yellow
docker images | Select-String "peo-"
Write-Host ""
Write-Host "🚀 Next steps:" -ForegroundColor Cyan
Write-Host "  - Test images: docker run -d -p 8080:8080 peo-identity:$Version"
Write-Host "  - Run with compose: docker-compose up"
Write-Host "  - Push to registry: docker push <registry>/peo-identity:$Version"
