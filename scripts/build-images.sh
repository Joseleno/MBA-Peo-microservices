#!/bin/bash
# Script para build de todas as imagens Docker
# Execute na raiz do projeto: ./scripts/build-images.sh

set -e  # Exit on error

echo "🐋 Building Docker Images - PEO Platform"
echo "========================================"
echo ""

# Colors
GREEN='\033[0;32m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Version tag (default: latest)
VERSION=${1:-latest}

echo "📦 Version: $VERSION"
echo ""

# Build Identity API
echo -e "${BLUE}Building Identity API...${NC}"
docker build -f docker/Dockerfile.Identity -t peo-identity:$VERSION .
echo -e "${GREEN}✓ Identity API built successfully${NC}"
echo ""

# Build Gestão de Conteúdo API
echo -e "${BLUE}Building Gestão de Conteúdo API...${NC}"
docker build -f docker/Dockerfile.GestaoConteudo -t peo-gestao-conteudo:$VERSION .
echo -e "${GREEN}✓ Gestão de Conteúdo API built successfully${NC}"
echo ""

# Build Gestão de Alunos API
echo -e "${BLUE}Building Gestão de Alunos API...${NC}"
docker build -f docker/Dockerfile.GestaoAlunos -t peo-gestao-alunos:$VERSION .
echo -e "${GREEN}✓ Gestão de Alunos API built successfully${NC}"
echo ""

# Build Faturamento API
echo -e "${BLUE}Building Faturamento API...${NC}"
docker build -f docker/Dockerfile.Faturamento -t peo-faturamento:$VERSION .
echo -e "${GREEN}✓ Faturamento API built successfully${NC}"
echo ""

# Build BFF
echo -e "${BLUE}Building BFF...${NC}"
docker build -f docker/Dockerfile.Bff -t peo-bff:$VERSION .
echo -e "${GREEN}✓ BFF built successfully${NC}"
echo ""

# Build SPA
echo -e "${BLUE}Building Blazor SPA...${NC}"
docker build -f docker/Dockerfile.Spa -t peo-spa:$VERSION .
echo -e "${GREEN}✓ Blazor SPA built successfully${NC}"
echo ""

echo "========================================"
echo -e "${GREEN}✓ All images built successfully!${NC}"
echo ""
echo "📋 Built images:"
docker images | grep "peo-"
echo ""
echo "🚀 Next steps:"
echo "  - Test images: docker run -d -p 8080:8080 peo-identity:$VERSION"
echo "  - Run with compose: docker-compose up"
echo "  - Push to registry: docker push <registry>/peo-identity:$VERSION"
