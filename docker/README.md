# Dockerfiles - PEO Platform

Este diretório contém os Dockerfiles otimizados para todos os serviços da Plataforma Educacional Online (PEO).

## 📋 Estrutura

```
docker/
├── Dockerfile.Identity          # API de autenticação
├── Dockerfile.GestaoConteudo   # API de gestão de conteúdo (cursos e aulas)
├── Dockerfile.GestaoAlunos     # API de gestão de alunos (matrículas e certificados)
├── Dockerfile.Faturamento      # API de faturamento (pagamentos)
├── Dockerfile.Bff              # Backend for Frontend
├── Dockerfile.Spa              # Blazor WebAssembly SPA
├── nginx.conf                  # Configuração nginx para SPA
└── README.md                   # Este arquivo
```

## 🏗️ Características dos Dockerfiles

Todos os Dockerfiles seguem as melhores práticas:

- **Multi-stage builds**: Build, Publish e Runtime separados para otimização
- **Cache de camadas**: Restauração de pacotes em camada separada
- **Imagens base oficiais**: Microsoft .NET 9.0 e nginx
- **Non-root user**: Execução com usuário não privilegiado
- **Health checks**: Verificação de saúde dos containers
- **Otimização de tamanho**: Apenas arquivos necessários na imagem final

## 🚀 Como fazer Build das Imagens

### Pré-requisitos
- Docker 24.0+
- 8GB RAM disponível
- Estar na raiz do projeto (onde está o arquivo Peo.sln)

### Build Individual

#### Identity API
```bash
docker build -f docker/Dockerfile.Identity -t peo-identity:latest .
```

#### Gestão de Conteúdo API
```bash
docker build -f docker/Dockerfile.GestaoConteudo -t peo-gestao-conteudo:latest .
```

#### Gestão de Alunos API
```bash
docker build -f docker/Dockerfile.GestaoAlunos -t peo-gestao-alunos:latest .
```

#### Faturamento API
```bash
docker build -f docker/Dockerfile.Faturamento -t peo-faturamento:latest .
```

#### BFF
```bash
docker build -f docker/Dockerfile.Bff -t peo-bff:latest .
```

#### SPA (Blazor WebAssembly)
```bash
docker build -f docker/Dockerfile.Spa -t peo-spa:latest .
```

### Build de Todas as Imagens

**Linux/Mac:**
```bash
#!/bin/bash
docker build -f docker/Dockerfile.Identity -t peo-identity:latest .
docker build -f docker/Dockerfile.GestaoConteudo -t peo-gestao-conteudo:latest .
docker build -f docker/Dockerfile.GestaoAlunos -t peo-gestao-alunos:latest .
docker build -f docker/Dockerfile.Faturamento -t peo-faturamento:latest .
docker build -f docker/Dockerfile.Bff -t peo-bff:latest .
docker build -f docker/Dockerfile.Spa -t peo-spa:latest .
```

**Windows (PowerShell):**
```powershell
docker build -f docker/Dockerfile.Identity -t peo-identity:latest .
docker build -f docker/Dockerfile.GestaoConteudo -t peo-gestao-conteudo:latest .
docker build -f docker/Dockerfile.GestaoAlunos -t peo-gestao-alunos:latest .
docker build -f docker/Dockerfile.Faturamento -t peo-faturamento:latest .
docker build -f docker/Dockerfile.Bff -t peo-bff:latest .
docker build -f docker/Dockerfile.Spa -t peo-spa:latest .
```

## 🧪 Testar Imagens Localmente

### Executar um container

```bash
# Exemplo: Identity API
docker run -d \
  --name peo-identity \
  -p 5001:8080 \
  -e ConnectionStrings__DefaultConnection="Server=sqlserver;Database=PeoIdentity;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True" \
  -e RabbitMQ__Host="rabbitmq" \
  peo-identity:latest

# Verificar logs
docker logs peo-identity

# Testar health check
curl http://localhost:5001/health
```

### Verificar tamanho das imagens

```bash
docker images | grep peo-
```

**Tamanhos esperados:**
- APIs (.NET): ~220-250MB
- SPA (nginx): ~50-70MB

### Verificar health check

```bash
# Ver status do health check
docker inspect --format='{{json .State.Health}}' peo-identity

# Executar health check manualmente
docker exec peo-identity curl -f http://localhost:8080/health
```

## 🏷️ Versionamento de Imagens

### Tagging para Docker Hub

```bash
# Exemplo para Identity API
docker tag peo-identity:latest seu-usuario/peo-identity:1.0.0
docker tag peo-identity:latest seu-usuario/peo-identity:latest

# Push para Docker Hub
docker push seu-usuario/peo-identity:1.0.0
docker push seu-usuario/peo-identity:latest
```

### Padrão de versionamento

- `latest`: Última versão estável
- `v1.0.0`: Versão semântica específica
- `dev`: Versão de desenvolvimento
- `{sha}`: Commit SHA específico (usado pelo CI/CD)

## 📊 Otimizações Aplicadas

### 1. Multi-stage Build
Reduz o tamanho final da imagem incluindo apenas runtime e binários compilados.

### 2. Cache de Camadas
```dockerfile
# Restaurar dependências ANTES de copiar código fonte
COPY ["*.csproj", "./"]
RUN dotnet restore

# Copiar código DEPOIS (evita invalidar cache)
COPY ["src/", "src/"]
```

### 3. .dockerignore
Exclui arquivos desnecessários do contexto de build:
- bin/obj
- .git
- testes
- documentação

### 4. Non-root User
```dockerfile
RUN groupadd -r appuser && useradd -r -g appuser appuser
USER appuser
```

### 5. Health Checks
```dockerfile
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1
```

## 🔍 Troubleshooting

### Erro: "No such file or directory"
**Causa**: Build executado fora da raiz do projeto
**Solução**: Executar na pasta que contém Peo.sln

### Erro: "Cannot connect to the Docker daemon"
**Causa**: Docker não está rodando
**Solução**: Iniciar Docker Desktop

### Build muito lento
**Causa**: Cache de camadas invalidado
**Solução**:
- Não modificar .csproj sem necessidade
- Verificar .dockerignore

### Imagem muito grande
**Causa**: Arquivos desnecessários incluídos
**Solução**:
- Verificar .dockerignore
- Usar `docker image inspect <imagem>` para analisar camadas

### Health check falhando
**Causa**: Endpoint /health não implementado
**Solução**: Implementar health checks (ver Fase 3 do plano)

## 📚 Referências

- [Docker Best Practices](https://docs.docker.com/develop/dev-best-practices/)
- [Multi-stage builds](https://docs.docker.com/build/building/multi-stage/)
- [.NET Docker Images](https://hub.docker.com/_/microsoft-dotnet)
- [Dockerfile reference](https://docs.docker.com/engine/reference/builder/)

## 🔄 Próximos Passos

1. Testar build de todas as imagens localmente
2. Implementar Health Checks (Fase 3)
3. Configurar Docker Compose (Fase 2)
4. Configurar pipeline CI/CD (Fase 5)
5. Deploy no Docker Hub

---

**Última atualização**: 09/11/2025
**Versão**: 1.0
