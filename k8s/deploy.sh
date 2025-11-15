#!/bin/bash
# Deploy script para PEO Platform no Kubernetes

set -e

echo "🚀 PEO Platform - Kubernetes Deployment"
echo "========================================"

# Cores
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Verificar kubectl
if ! command -v kubectl &> /dev/null; then
    echo -e "${RED}❌ kubectl não encontrado. Instale kubectl primeiro.${NC}"
    exit 1
fi

echo -e "${GREEN}✅ kubectl encontrado${NC}"

# Verificar conexão com cluster
if ! kubectl cluster-info &> /dev/null; then
    echo -e "${RED}❌ Não foi possível conectar ao cluster Kubernetes${NC}"
    exit 1
fi

echo -e "${GREEN}✅ Conectado ao cluster Kubernetes${NC}"

# Verificar se SEU_DOCKER_USERNAME foi substituído
if grep -r "SEU_DOCKER_USERNAME" deployments/ &> /dev/null; then
    echo -e "${YELLOW}⚠️  ATENÇÃO: Substitua 'SEU_DOCKER_USERNAME' pelos seus nomes de imagens Docker!${NC}"
    echo -e "${YELLOW}   Execute: find deployments/ -name '*.yaml' -exec sed -i 's/SEU_DOCKER_USERNAME/seu-usuario/g' {} +${NC}"
    read -p "Continuar mesmo assim? (y/N) " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        exit 1
    fi
fi

# 1. Namespace
echo ""
echo "📦 1/6 Criando namespace..."
kubectl apply -f namespace.yaml
echo -e "${GREEN}✅ Namespace criado${NC}"

# 2. ConfigMaps e Secrets
echo ""
echo "🔧 2/6 Criando ConfigMaps e Secrets..."
kubectl apply -f configmaps/
kubectl apply -f secrets/
echo -e "${GREEN}✅ ConfigMaps e Secrets criados${NC}"

# 3. Infraestrutura
echo ""
echo "🏗️  3/6 Deployando infraestrutura (SQL Server + RabbitMQ)..."
kubectl apply -f infrastructure/
echo -e "${YELLOW}⏳ Aguardando infraestrutura ficar pronta (pode levar até 5 minutos)...${NC}"

kubectl wait --for=condition=ready pod -l app=sqlserver -n peo-platform --timeout=300s || true
kubectl wait --for=condition=ready pod -l app=rabbitmq -n peo-platform --timeout=300s || true

echo -e "${GREEN}✅ Infraestrutura pronta${NC}"

# 4. Microserviços
echo ""
echo "🎯 4/6 Deployando microserviços..."
kubectl apply -f deployments/
kubectl apply -f services/
echo -e "${YELLOW}⏳ Aguardando microserviços ficarem prontos...${NC}"
sleep 10
echo -e "${GREEN}✅ Microserviços deployados${NC}"

# 5. Ingress
echo ""
echo "🌐 5/6 Configurando Ingress..."
kubectl apply -f ingress/
echo -e "${GREEN}✅ Ingress configurado${NC}"

# 6. HPA
echo ""
echo "📈 6/6 Configurando Auto-Scaling (HPA)..."
kubectl apply -f hpa/
echo -e "${GREEN}✅ HPA configurado${NC}"

# Status
echo ""
echo "========================================"
echo "✨ Deploy completo!"
echo "========================================"
echo ""
echo "📊 Status dos recursos:"
echo ""

kubectl get pods -n peo-platform
echo ""
kubectl get svc -n peo-platform
echo ""
kubectl get hpa -n peo-platform
echo ""

echo "🌐 Acesso:"
echo "  - SPA: http://peo.local"
echo "  - BFF: http://peo.local/api"
echo ""
echo "💡 Para ver logs:"
echo "  kubectl logs -f -n peo-platform deployment/bff"
echo ""
echo "📝 Para mais comandos, veja k8s/README.md"
