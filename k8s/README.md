# Kubernetes Deployment - PEO Platform

Manifestos Kubernetes para deploy da Plataforma de Educação Online (PEO).

## 📋 Pré-requisitos

- Kubernetes cluster (minikube, kind, AKS, EKS, GKE)
- `kubectl` instalado e configurado
- NGINX Ingress Controller
- Metrics Server (para HPA)

## 🚀 Deploy Rápido

### 1. Configurar Imagens Docker

**IMPORTANTE**: Edite todos os arquivos em `deployments/` e substitua `SEU_DOCKER_USERNAME` pelo seu username do Docker Hub:

```bash
# Linux/Mac
find deployments/ -name "*.yaml" -exec sed -i 's/SEU_DOCKER_USERNAME/seu-usuario/g' {} +

# Windows PowerShell
Get-ChildItem deployments/*.yaml | ForEach-Object { (Get-Content $_) -replace 'SEU_DOCKER_USERNAME', 'seu-usuario' | Set-Content $_ }
```

### 2. Deploy Completo

```bash
# 1. Criar namespace
kubectl apply -f namespace.yaml

# 2. Criar ConfigMaps e Secrets
kubectl apply -f configmaps/
kubectl apply -f secrets/

# 3. Deploy Infraestrutura (SQL Server + RabbitMQ)
kubectl apply -f infrastructure/

# Aguardar infraestrutura ficar pronta
kubectl wait --for=condition=ready pod -l app=sqlserver -n peo-platform --timeout=300s
kubectl wait --for=condition=ready pod -l app=rabbitmq -n peo-platform --timeout=300s

# 4. Deploy Microserviços
kubectl apply -f deployments/
kubectl apply -f services/

# 5. Deploy Ingress
kubectl apply -f ingress/

# 6. Deploy HPA (Horizontal Pod Autoscaler)
kubectl apply -f hpa/
```

### 3. Verificar Status

```bash
# Ver todos os pods
kubectl get pods -n peo-platform

# Ver services
kubectl get svc -n peo-platform

# Ver HPA
kubectl get hpa -n peo-platform

# Ver ingress
kubectl get ingress -n peo-platform
```

## 🏗️ Estrutura

```
k8s/
├── namespace.yaml                  # Namespace peo-platform
├── configmaps/
│   └── app-config.yaml            # Configurações não-sensíveis
├── secrets/
│   └── app-secrets.yaml           # Senhas e connection strings
├── infrastructure/
│   ├── sqlserver-statefulset.yaml # SQL Server (StatefulSet)
│   ├── sqlserver-service.yaml
│   ├── rabbitmq-statefulset.yaml  # RabbitMQ (StatefulSet)
│   └── rabbitmq-service.yaml
├── deployments/
│   ├── identity-deployment.yaml
│   ├── gestao-conteudo-deployment.yaml
│   ├── gestao-alunos-deployment.yaml
│   ├── faturamento-deployment.yaml
│   ├── bff-deployment.yaml
│   └── spa-deployment.yaml
├── services/
│   ├── identity-service.yaml
│   ├── gestao-conteudo-service.yaml
│   ├── gestao-alunos-service.yaml
│   ├── faturamento-service.yaml
│   ├── bff-service.yaml
│   └── spa-service.yaml
├── ingress/
│   └── ingress.yaml               # NGINX Ingress
└── hpa/
    ├── identity-hpa.yaml          # Auto-scaling
    ├── gestao-conteudo-hpa.yaml
    ├── gestao-alunos-hpa.yaml
    ├── faturamento-hpa.yaml
    └── bff-hpa.yaml
```

## 🔧 Configuração Detalhada

### Replicas

- **APIs**: 2 réplicas iniciais (auto-scale até 10)
- **BFF**: 3 réplicas iniciais (auto-scale até 15)
- **SPA**: 2 réplicas fixas
- **Infraestrutura**: 1 réplica (StatefulSet)

### Recursos

**APIs**:
- Requests: 256Mi RAM, 250m CPU
- Limits: 512Mi RAM, 500m CPU

**BFF**:
- Requests: 256Mi RAM, 250m CPU
- Limits: 512Mi RAM, 500m CPU

**SPA**:
- Requests: 128Mi RAM, 100m CPU
- Limits: 256Mi RAM, 200m CPU

**SQL Server**:
- Requests: 2Gi RAM, 1000m CPU
- Limits: 4Gi RAM, 2000m CPU

**RabbitMQ**:
- Requests: 512Mi RAM, 250m CPU
- Limits: 1Gi RAM, 500m CPU

### Auto-Scaling (HPA)

- **Trigger**: CPU > 70% ou Memory > 80%
- **Scale Up**: Dobra pods a cada 30s (ou +2 pods)
- **Scale Down**: Reduz 50% a cada 60s (após 5min de estabilidade)

## 🌐 Acesso

### Minikube

```bash
# Habilitar Ingress
minikube addons enable ingress
minikube addons enable metrics-server

# Obter IP do Minikube
minikube ip

# Adicionar ao /etc/hosts (Linux/Mac) ou C:\Windows\System32\drivers\etc\hosts (Windows)
<MINIKUBE_IP> peo.local
```

### Acessar aplicação

- **SPA**: http://peo.local
- **BFF**: http://peo.local/api
- **APIs**: http://peo.local/identity, /gestao-conteudo, /gestao-alunos, /faturamento

## 🔍 Troubleshooting

### Pods não iniciam

```bash
# Ver logs
kubectl logs -n peo-platform <pod-name>

# Descrever pod
kubectl describe pod -n peo-platform <pod-name>
```

### Infraestrutura não fica pronta

```bash
# Verificar PVC
kubectl get pvc -n peo-platform

# Verificar se storage class está disponível
kubectl get storageclass
```

### HPA não funciona

```bash
# Verificar Metrics Server
kubectl get deployment metrics-server -n kube-system

# Ver métricas
kubectl top pods -n peo-platform
```

## 🗑️ Cleanup

```bash
# Remover tudo
kubectl delete namespace peo-platform

# Ou remover por categoria
kubectl delete -f hpa/
kubectl delete -f ingress/
kubectl delete -f services/
kubectl delete -f deployments/
kubectl delete -f infrastructure/
kubectl delete -f secrets/
kubectl delete -f configmaps/
kubectl delete -f namespace.yaml
```

## 📊 Monitoramento

```bash
# Watch pods
kubectl get pods -n peo-platform -w

# Ver eventos
kubectl get events -n peo-platform --sort-by='.lastTimestamp'

# Logs em tempo real
kubectl logs -f -n peo-platform deployment/identity-api
kubectl logs -f -n peo-platform deployment/bff
```

## 🔐 Segurança

**⚠️ IMPORTANTE**:
- `app-secrets.yaml` contém senhas em **plain text** para facilitar setup local
- **NUNCA** commite secrets em produção
- Use **Sealed Secrets**, **External Secrets**, ou **Vault** em produção
- Senhas atuais são apenas para desenvolvimento

**Produção**:
```bash
# Criar secret via CLI (base64 encoding automático)
kubectl create secret generic peo-secrets \
  --from-literal=SA_PASSWORD='SuaSenhaForte' \
  --from-literal=RABBITMQ_DEFAULT_PASS='OutraSenhaForte' \
  -n peo-platform
```

## 📈 Performance

- **Health Checks**: Liveness + Readiness probes em todos os serviços
- **Resource Limits**: Evita consumo descontrolado
- **HPA**: Auto-scaling baseado em CPU/Memory
- **Persistent Volumes**: Dados de SQL Server e RabbitMQ persistem
