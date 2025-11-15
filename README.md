[![.NET](https://github.com/jonataspc/MBA-Peo-microservices/actions/workflows/dotnet.yml/badge.svg)](https://github.com/jonataspc/MBA-Peo-microservices/actions/workflows/dotnet.yml)
[![Docker Publish](https://github.com/jonataspc/MBA-Peo-microservices/actions/workflows/docker-publish.yml/badge.svg)](https://github.com/jonataspc/MBA-Peo-microservices/actions/workflows/docker-publish.yml)

# PEO - Plataforma de Educação Online

## Apresentação

Bem-vindo ao repositório do projeto **PEO - Plataforma Educacional com Pipeline CI/CD, Docker e Kubernetes**. Este projeto é uma entrega do **MBA DevXpert Full Stack .NET** e é referente ao **quinto módulo (DevOps)** do MBA Desenvolvedor.IO.

O objetivo principal é evoluir a Plataforma Educacional Distribuída desenvolvida no módulo anterior, transformando-a em um **ecossistema DevOps completo**, com automação de build, testes, integração, entrega e orquestração em ambiente Kubernetes.

### Autores

- **Eduardo Gimenes**
- **Filipe Alan Elias**
- **Jonatas Cruz**
- **Joseleno Santos**
- **Leandro Andreotti**
- **Paulo Cesar Carneiro**
- **Marcelo Menezes**

---

## Proposta do Projeto

O projeto implementa:

1. **Controle de Versão**: GitHub com branching model e Pull Requests
2. **Containerização**: 6 microsserviços com Dockerfiles otimizados (multi-stage builds)
3. **Orquestração Kubernetes**: Deployments, Services, ConfigMaps, Secrets, HPA, Ingress
4. **Pipeline CI/CD**: GitHub Actions com build, testes, deploy automático no Docker Hub
5. **Resiliência**: Polly (Retry, Circuit Breaker, Timeout), Health Checks
6. **Automação Local**: Docker Compose para desenvolvimento rápido

---

## Tecnologias Utilizadas

### Stack Principal
- **Linguagem**: C# / .NET 9.0
- **Frameworks**: ASP.NET Core Web API, Blazor WebAssembly, Entity Framework Core, MediatR, MassTransit
- **Bibliotecas**: MudBlazor, NSwag, Polly, FluentValidation

### DevOps & Infraestrutura
- **Containerização**: Docker, Docker Compose
- **Orquestração**: Kubernetes
- **CI/CD**: GitHub Actions
- **Registry**: Docker Hub
- **Banco de Dados**: SQL Server (prod), SQLite (dev)
- **Mensageria**: RabbitMQ
- **Autenticação**: ASP.NET Core Identity + JWT
- **Documentação**: Swagger/OpenAPI

---

## Estrutura do Projeto

```
MBA-Peo-microservices/
├── .github/workflows/          # CI/CD Pipelines
│   ├── dotnet.yml             # Build, testes, cobertura
│   └── docker-publish.yml     # Deploy Docker Hub
├── docker/                     # Dockerfiles (6 serviços)
├── k8s/                        # Kubernetes manifests
│   ├── deployments/           # 6 Deployments
│   ├── services/              # 6 Services
│   ├── infrastructure/        # SQL Server + RabbitMQ
│   ├── configmaps/            # Configurações
│   ├── secrets/               # Credenciais
│   ├── ingress/               # NGINX Ingress
│   ├── hpa/                   # Auto-scaling
│   └── README.md              # Guia Kubernetes
├── src/                        # Código-fonte
│   ├── Peo.Identity.WebApi/
│   ├── Peo.GestaoConteudo.WebApi/
│   ├── Peo.GestaoAlunos.WebApi/
│   ├── Peo.Faturamento.WebApi/
│   ├── Peo.Web.Bff/
│   └── Peo.Web.Spa/
├── tests/                      # Testes
├── docker-compose.yml          # Orquestração local
└── README.md                   # Este arquivo
```

---

## Funcionalidades Implementadas

### APIs (Bounded Contexts)

1. **Identity API**: Autenticação, cadastro, JWT
2. **Gestão Conteúdo API**: CRUD cursos e aulas
3. **Gestão Alunos API**: Matrículas, progresso, certificados
4. **Faturamento API**: Processamento pagamentos
5. **BFF**: Orquestração de chamadas
6. **SPA**: Interface Blazor WebAssembly

### DevOps

- ✅ **Containerização**: 6 Dockerfiles com multi-stage builds, Alpine Linux
- ✅ **Docker Compose**: Orquestração completa (8 serviços)
- ✅ **Kubernetes**: 25 manifestos YAML (Deployments, Services, HPA, Ingress)
- ✅ **CI/CD**: 2 workflows (build/test + docker publish)
- ✅ **Resiliência**: Polly (Retry 3x, Circuit Breaker, Timeout 30s) + 22 testes
- ✅ **Health Checks**: Liveness/Readiness probes
- ✅ **Auto-Scaling**: HPA baseado em CPU/Memory (70%/80%)

---

## Como Executar o Projeto

### Pré-requisitos

- **.NET SDK 9.0+**
- **Docker Desktop**
- **Git**
- **kubectl** (para Kubernetes)
- **Minikube/Kind** (para cluster local)

---

### Opção 1: Docker Compose (Recomendado para Dev)

Forma mais rápida de rodar todo o ecossistema localmente.

```bash
# 1. Clone
git clone https://github.com/jonataspc/MBA-Peo-microservices.git
cd MBA-Peo-microservices

# 2. Inicie
docker-compose up -d

# 3. Aguarde (~2 min)
docker-compose ps

# 4. Acesse
# SPA:        http://localhost:8081
# BFF:        http://localhost:5000
# Identity:   http://localhost:5001
# RabbitMQ:   http://localhost:15672 (peo/Peo@2025!)

# 5. Logs
docker-compose logs -f

# 6. Parar
docker-compose down
```

---

### Opção 2: Kubernetes (Produção-like)

```bash
# 1. Iniciar Minikube
minikube start --cpus=4 --memory=8192
minikube addons enable ingress
minikube addons enable metrics-server

# 2. Configurar imagens (substitua SEU_USUARIO)
cd k8s
find deployments/ -name "*.yaml" -exec sed -i 's/SEU_DOCKER_USERNAME/seu-usuario/g' {} +

# 3. Deploy automatizado
./deploy.sh

# 4. OU deploy manual
kubectl apply -f namespace.yaml
kubectl apply -f configmaps/
kubectl apply -f secrets/
kubectl apply -f infrastructure/
kubectl wait --for=condition=ready pod -l app=sqlserver -n peo-platform --timeout=300s
kubectl wait --for=condition=ready pod -l app=rabbitmq -n peo-platform --timeout=300s
kubectl apply -f deployments/
kubectl apply -f services/
kubectl apply -f ingress/
kubectl apply -f hpa/

# 5. Verificar
kubectl get pods -n peo-platform
kubectl get svc -n peo-platform
kubectl get hpa -n peo-platform

# 6. Configurar hosts
# Adicionar ao /etc/hosts (ou C:\Windows\System32\drivers\etc\hosts):
# <minikube-ip> peo.local

# 7. Acessar
# http://peo.local

# 8. Logs
kubectl logs -f -n peo-platform deployment/bff

# 9. Limpar
kubectl delete namespace peo-platform
```

**Documentação completa**: [k8s/README.md](./k8s/README.md)

---

### Opção 3: Desenvolvimento (.NET CLI)

```bash
# Clone
git clone https://github.com/jonataspc/MBA-Peo-microservices.git
cd MBA-Peo-microservices

# Aspire AppHost
cd src/Peo.AppHost
dotnet run --launch-profile "https"

# Dashboard: https://localhost:17005
```

**Credenciais**:
- Email: `admin@admin.com`
- Senha: `@dmin!`

---

## DevOps - CI/CD Pipeline

### Workflow 1: .NET (`dotnet.yml`)

Executa em **push/PR** para `main`:

1. Build com `-WarnAsError`
2. Testes unitários + integração
3. Cobertura com dotCover
4. Upload de artifacts

**Ver relatório**: Actions → Workflow → Download `code-coverage-report`

### Workflow 2: Docker Hub (`docker-publish.yml`)

Executa em:
- **Push `main`**: Build + push `latest`
- **Tags `v*.*.*`**: Build + push com versionamento semântico

**Features**:
- Matrix strategy (6 imagens paralelas)
- Multi-plataforma (`linux/amd64`, `linux/arm64`)
- Tags: `latest`, `v1.0.0`, `1.0`, `1`, `main-sha`
- Cache GitHub Actions

**Configurar Secrets** (Settings → Secrets and variables → Actions):
```
DOCKER_USERNAME = seu-usuario-docker-hub
DOCKER_PASSWORD = seu-token-docker-hub
```

**Executar**:
```bash
git tag v1.0.0
git push origin v1.0.0
```

---

## Kubernetes - Recursos

### Arquitetura

```
NGINX Ingress (peo.local)
  │
  ├─> SPA Service :80 (2 replicas)
  ├─> BFF Service :8080 (3 replicas, HPA 3-15)
  ├─> Identity Service :8080 (2 replicas, HPA 2-10)
  ├─> Gestão Conteúdo Service (2 replicas, HPA 2-10)
  ├─> Gestão Alunos Service (2 replicas, HPA 2-10)
  └─> Faturamento Service (2 replicas, HPA 2-10)

Infraestrutura (StatefulSets)
  ├─> SQL Server (1 replica, 10Gi PVC)
  └─> RabbitMQ (1 replica, 5Gi PVC)
```

### Auto-Scaling (HPA)

- **Trigger**: CPU > 70% OU Memory > 80%
- **Scale UP**: Dobra pods a cada 30s
- **Scale DOWN**: Reduz 50% a cada 60s (após 5min estabilidade)

### Health Checks

- **Liveness**: `GET /alive` (APIs), `sqlcmd` (SQL), `rabbitmq-diagnostics` (RabbitMQ)
- **Readiness**: `GET /health` (inclui verificação de dependências)

---

## Resiliência

### Polly Policies

```csharp
// Retry: 3 tentativas, backoff exponencial
options.Retry.MaxRetryAttempts = 3;

// Circuit Breaker: Abre após 50% falhas em 30s
options.CircuitBreaker.FailureRatio = 0.5;

// Timeout: 30 segundos
options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(30);
```

**Testes**: 22 testes automatizados em `Peo.Tests.Resilience/`

---

## Documentação da API

Swagger disponível em cada API:

- **Identity**: http://localhost:5001/swagger
- **Gestão Conteúdo**: http://localhost:5002/swagger
- **Gestão Alunos**: http://localhost:5003/swagger
- **Faturamento**: http://localhost:5004/swagger
- **BFF**: http://localhost:5000/swagger

---

## Testes

```bash
# Todos os testes
dotnet test

# Com cobertura
./scripts/run-tests-with-coverage.ps1
./scripts/report.html
```

**CI/CD**: Executados automaticamente em cada push/PR

---

## Troubleshooting

### Docker Compose

```bash
# Ver logs
docker-compose logs <service>

# Reiniciar
docker-compose down -v && docker-compose up -d
```

### Kubernetes

```bash
# Pods com erro
kubectl describe pod -n peo-platform <pod-name>
kubectl logs -n peo-platform <pod-name> --previous

# HPA não escala
kubectl top pods -n peo-platform
kubectl get deployment metrics-server -n kube-system

# Ingress não funciona
minikube addons enable ingress
kubectl get pods -n ingress-nginx
```

---

## Documentação Adicional

- **[Kubernetes README](./k8s/README.md)**: Guia completo Kubernetes
- **[Docker Compose Guide](./docker/DOCKER_COMPOSE_GUIDE.md)**: Detalhes Docker Compose
- **[PLANO_IMPLEMENTACAO_DEVOPS.md](./PLANO_IMPLEMENTACAO_DEVOPS.md)**: Plano de implementação
- **[Documentação Arquitetura](./docs/README.md)**: Decisões técnicas

---

## Matriz de Avaliação (PDF)

| Critério | Peso | Status |
|----------|------|--------|
| Funcionalidades DevOps | 30% | ✅ Completo (Pipeline CI/CD, Docker Hub) |
| Qualidade do Código | 30% | ✅ Completo (Dockerfiles, YAMLs, versionado) |
| Integração Kubernetes | 10% | ✅ Completo (25 manifestos, cluster funcional) |
| Observabilidade | 10% | ✅ Completo (Health checks, logs, métricas) |
| Documentação | 10% | ✅ Completo (README, Swagger, k8s/README.md) |
| Resolução de Feedbacks | 10% | ✅ Implementado (FEEDBACK.md) |

---

## Avaliação

- Este projeto é parte de um curso acadêmico e não aceita contribuições externas.
- Para feedbacks ou dúvidas utilize o recurso de **Issues** do GitHub.
- O arquivo **FEEDBACK.md** é um resumo das avaliações do instrutor e deverá ser modificado apenas por ele.

---

## Contato

**Repositório**: https://github.com/jonataspc/MBA-Peo-microservices

**Projeto Acadêmico** - MBA DevXpert Full Stack .NET - Desenvolvedor.IO
