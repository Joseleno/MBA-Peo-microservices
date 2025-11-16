# Guia Docker Compose - PEO Platform

Este guia explica como executar toda a plataforma PEO localmente usando Docker Compose.

## Índice

- [Pré-requisitos](#pré-requisitos)
- [Arquitetura](#arquitetura)
- [Quick Start](#quick-start)
- [Comandos Úteis](#comandos-úteis)
- [Configuração](#configuração)
- [Portas dos Serviços](#portas-dos-serviços)
- [Health Checks](#health-checks)
- [Troubleshooting](#troubleshooting)
- [Desenvolvimento](#desenvolvimento)

---

## Pré-requisitos

### Software Necessário

- **Docker Desktop** 4.20+ ou **Docker Engine** 24+
- **Docker Compose** 2.20+ (incluído no Docker Desktop)
- **Git** (para clonar o repositório)
- **Mínimo 8GB RAM** disponível para containers
- **20GB de espaço em disco** livre

### Verificar Instalação

```bash
docker --version
# Docker version 24.0.0 ou superior

docker-compose --version
# Docker Compose version v2.20.0 ou superior
```

---

## Arquitetura

A plataforma PEO é composta por:

### Microserviços (4 APIs)
- **Identity API** (porta 5001) - Autenticação e autorização
- **Gestão de Conteúdo API** (porta 5002) - Gerenciamento de cursos
- **Gestão de Alunos API** (porta 5003) - Gerenciamento de alunos
- **Faturamento API** (porta 5004) - Processamento de pagamentos

### Gateway e Frontend
- **BFF** (porta 5000) - Backend for Frontend / API Gateway
- **SPA** (porta 8081) - Blazor WebAssembly Frontend

### Infraestrutura
- **SQL Server** (porta 1433) - Banco de dados relacional
- **RabbitMQ** (portas 5672, 15672) - Message broker

### Diagrama de Comunicação

```
┌─────────────┐
│   Browser   │
└──────┬──────┘
       │
       │ http://localhost:8081
       ▼
┌─────────────┐
│     SPA     │ (Blazor WASM)
└──────┬──────┘
       │
       │ http://localhost:5000
       ▼
┌─────────────┐
│     BFF     │ (API Gateway)
└──────┬──────┘
       │
       ├──────────┬──────────┬──────────┐
       ▼          ▼          ▼          ▼
┌──────────┐┌──────────┐┌──────────┐┌──────────┐
│Identity  ││Gestão    ││Gestão    ││Fatura-   │
│API       ││Conteúdo  ││Alunos    ││mento     │
│:5001     ││API :5002 ││API :5003 ││API :5004 │
└────┬─────┘└────┬─────┘└────┬─────┘└────┬─────┘
     │           │           │           │
     └───────────┴───────────┴───────────┘
                 │           │
         ┌───────┴───┐   ┌───┴─────┐
         │ SQL Server│   │RabbitMQ │
         │   :1433   │   │  :5672  │
         └───────────┘   └─────────┘
```

---

## Quick Start

### 1. Clone o Repositório

```bash
git clone <repository-url>
cd MBA-Peo-microservices
```

### 2. Configure Variáveis de Ambiente (Opcional)

```bash
# Copie o arquivo de exemplo
cp .env.example .env

# Edite .env se necessário (opcional para desenvolvimento local)
# As configurações padrão já funcionam out-of-the-box
```

### 3. Inicie Todos os Serviços

```bash
# Modo detached (background)
docker-compose up -d

# Ou com logs visíveis (foreground)
docker-compose up
```

**Primeira execução**: O processo pode levar 5-10 minutos para:
- Fazer download das imagens base
- Buildar todas as imagens dos microserviços
- Inicializar os bancos de dados
- Aguardar health checks

### 4. Verifique o Status

```bash
docker-compose ps
```

Saída esperada:
```
NAME                      STATUS                    PORTS
peo-bff                   Up (healthy)              0.0.0.0:5000->8080/tcp
peo-faturamento-api       Up (healthy)              0.0.0.0:5004->8080/tcp
peo-gestao-alunos-api     Up (healthy)              0.0.0.0:5003->8080/tcp
peo-gestao-conteudo-api   Up (healthy)              0.0.0.0:5002->8080/tcp
peo-identity-api          Up (healthy)              0.0.0.0:5001->8080/tcp
peo-rabbitmq              Up (healthy)              5672/tcp, 15672/tcp
peo-spa                   Up (healthy)              0.0.0.0:8081->80/tcp
peo-sqlserver             Up (healthy)              0.0.0.0:1433->1433/tcp
```

### 5. Acesse a Aplicação

Abra seu navegador e acesse:

- **SPA (Frontend)**: http://localhost:8081
- **BFF (API Gateway)**: http://localhost:5000
- **RabbitMQ Management**: http://localhost:15672
  - Usuário: `peo`
  - Senha: `Peo@2025!`

---

## Comandos Úteis

### Gerenciamento de Serviços

```bash
# Iniciar todos os serviços
docker-compose up -d

# Parar todos os serviços (mantém dados)
docker-compose stop

# Parar e remover containers (mantém volumes)
docker-compose down

# Parar, remover containers E volumes (APAGA DADOS)
docker-compose down -v

# Reiniciar todos os serviços
docker-compose restart

# Reiniciar apenas um serviço específico
docker-compose restart identity-api
```

### Visualizar Logs

```bash
# Logs de todos os serviços
docker-compose logs -f

# Logs de um serviço específico
docker-compose logs -f identity-api

# Logs das últimas 100 linhas
docker-compose logs --tail=100

# Logs de múltiplos serviços
docker-compose logs -f identity-api bff
```

### Build e Rebuild

```bash
# Rebuild de todas as imagens
docker-compose build

# Rebuild sem cache (build limpo)
docker-compose build --no-cache

# Rebuild de um serviço específico
docker-compose build identity-api

# Up com rebuild forçado
docker-compose up -d --build
```

### Escalar Serviços

```bash
# Escalar Identity API para 3 instâncias
docker-compose up -d --scale identity-api=3

# Escalar BFF para 2 instâncias
docker-compose up -d --scale bff=2
```

### Executar Comandos em Containers

```bash
# Shell interativo no container
docker-compose exec identity-api sh

# Executar comando único
docker-compose exec sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "Peo@2025!Strong"

# Ver processos em execução
docker-compose top
```

### Limpeza

```bash
# Remover containers parados
docker-compose rm

# Remover volumes não utilizados
docker volume prune

# Remover tudo (containers, networks, volumes, imagens)
docker-compose down -v --rmi all

# Limpeza geral do Docker
docker system prune -a --volumes
```

---

## Configuração

### Variáveis de Ambiente

As variáveis de ambiente podem ser configuradas de 3 formas:

1. **Arquivo .env** (recomendado para desenvolvimento)
2. **docker-compose.override.yml** (sobrescreve docker-compose.yml)
3. **Variáveis de ambiente do sistema**

### Exemplo de .env

```env
# Database
SQL_SERVER_SA_PASSWORD=Peo@2025!Strong

# RabbitMQ
RABBITMQ_DEFAULT_USER=peo
RABBITMQ_DEFAULT_PASS=Peo@2025!

# JWT
JWT_SECRET_KEY=PEO-Platform-2025-SuperSecretKey-MinLength32Chars!
JWT_ISSUER=PEO.Platform
JWT_AUDIENCE=PEO.Clients
JWT_EXPIRATION_MINUTES=60

# PayPal (para Faturamento)
PAYPAL_MODE=sandbox
PAYPAL_CLIENT_ID=YOUR_CLIENT_ID
PAYPAL_CLIENT_SECRET=YOUR_CLIENT_SECRET
```

### Customizar Portas

Edite o arquivo `docker-compose.yml` ou use `docker-compose.override.yml`:

```yaml
services:
  identity-api:
    ports:
      - "6001:8080"  # Mudar porta externa para 6001
```

---

## Portas dos Serviços

| Serviço | Porta Externa | Porta Interna | Descrição |
|---------|---------------|---------------|-----------|
| Identity API | 5001 | 8080 | API de autenticação |
| Gestão Conteúdo API | 5002 | 8080 | API de cursos |
| Gestão Alunos API | 5003 | 8080 | API de alunos |
| Faturamento API | 5004 | 8080 | API de pagamentos |
| BFF | 5000 | 8080 | API Gateway |
| SPA | 8081 | 80 | Frontend Blazor |
| SQL Server | 1433 | 1433 | Banco de dados |
| RabbitMQ (AMQP) | 5672 | 5672 | Message broker |
| RabbitMQ (Management) | 15672 | 15672 | UI de gerenciamento |

### URLs Swagger (quando disponíveis)

- Identity API: http://localhost:5001/swagger
- Gestão Conteúdo: http://localhost:5002/swagger
- Gestão Alunos: http://localhost:5003/swagger
- Faturamento: http://localhost:5004/swagger
- BFF: http://localhost:5000/swagger

---

## Health Checks

Todos os serviços implementam health checks para garantir disponibilidade.

### Verificar Status de Health

```bash
# Ver status de todos os containers
docker-compose ps

# Inspecionar health check de um serviço
docker inspect --format='{{json .State.Health}}' peo-identity-api | jq

# Logs de health check
docker-compose logs identity-api | grep health
```

### Endpoints de Health Check

Cada API expõe um endpoint `/health`:

```bash
curl http://localhost:5001/health  # Identity API
curl http://localhost:5002/health  # Gestão Conteúdo
curl http://localhost:5003/health  # Gestão Alunos
curl http://localhost:5004/health  # Faturamento
curl http://localhost:5000/health  # BFF
curl http://localhost:8081/health  # SPA
```

### Aguardar Serviços Ficarem Healthy

```bash
# Script para aguardar todos os serviços
./scripts/wait-for-healthy.sh
```

---

## Troubleshooting

### Problema: Containers não iniciam

**Sintomas**: `docker-compose up` falha ou containers ficam reiniciando

**Soluções**:

1. Verificar logs do container com problema:
```bash
docker-compose logs identity-api
```

2. Verificar recursos disponíveis:
```bash
docker system df
docker stats
```

3. Limpar recursos antigos:
```bash
docker-compose down -v
docker system prune -a
```

### Problema: SQL Server não conecta

**Sintomas**: APIs falham ao conectar no banco

**Soluções**:

1. Verificar se SQL Server está healthy:
```bash
docker-compose ps sqlserver
```

2. Testar conexão manualmente:
```bash
docker-compose exec sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "Peo@2025!Strong" -Q "SELECT @@VERSION"
```

3. Verificar senha no .env ou docker-compose.yml

### Problema: RabbitMQ não conecta

**Soluções**:

1. Verificar status:
```bash
docker-compose ps rabbitmq
docker-compose logs rabbitmq
```

2. Acessar Management UI:
```
http://localhost:15672
User: peo
Pass: Peo@2025!
```

3. Verificar credenciais nas variáveis de ambiente

### Problema: Porta já em uso

**Sintomas**: `Error starting userland proxy: listen tcp4 0.0.0.0:5001: bind: address already in use`

**Soluções**:

1. Encontrar processo usando a porta:
```bash
# Windows
netstat -ano | findstr :5001

# Linux/Mac
lsof -i :5001
```

2. Matar processo ou mudar porta no docker-compose.yml

### Problema: Build muito lento

**Soluções**:

1. Usar cache de builds:
```bash
docker-compose build --parallel
```

2. Aumentar recursos do Docker Desktop:
- Settings > Resources > CPU/Memory

3. Habilitar BuildKit:
```bash
$env:DOCKER_BUILDKIT=1  # PowerShell
export DOCKER_BUILDKIT=1  # Bash
```

### Problema: Containers ficam "unhealthy"

**Soluções**:

1. Aumentar `start_period` nos health checks (docker-compose.yml)
2. Verificar logs para entender por que o serviço não responde
3. Verificar dependências (depends_on)

---

## Desenvolvimento

### Hot Reload / Live Reload

Para desenvolvimento com hot reload, monte volumes locais:

```yaml
# docker-compose.override.yml
services:
  identity-api:
    volumes:
      - ./src/Peo.Identity.WebApi:/app
    environment:
      - DOTNET_USE_POLLING_FILE_WATCHER=true
```

### Debug Remoto

Para debug remoto, adicione:

```yaml
services:
  identity-api:
    environment:
      - DOTNET_RUNNING_IN_CONTAINER=true
    ports:
      - "5001:8080"
      - "4000:4000"  # Debugger port
```

### Rodar Apenas Infraestrutura

Para rodar apenas SQL Server e RabbitMQ (útil se quiser rodar APIs localmente):

```bash
docker-compose up -d sqlserver rabbitmq
```

### Conectar ao SQL Server com SSMS

- **Server**: localhost,1433
- **Authentication**: SQL Server Authentication
- **Login**: sa
- **Password**: Peo@2025!Strong

### Acessar Databases

```bash
# Listar databases
docker-compose exec sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "Peo@2025!Strong" -Q "SELECT name FROM sys.databases"

# Query em database específica
docker-compose exec sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "Peo@2025!Strong" -d PeoIdentity -Q "SELECT * FROM Users"
```

### Backup de Dados

```bash
# Backup de todos os volumes
docker run --rm -v peo-sqlserver-data:/data -v $(pwd)/backups:/backup alpine tar czf /backup/sqlserver-backup.tar.gz /data

# Restore
docker run --rm -v peo-sqlserver-data:/data -v $(pwd)/backups:/backup alpine tar xzf /backup/sqlserver-backup.tar.gz -C /
```

---

## Performance e Monitoramento

### Monitorar Recursos

```bash
# CPU e memória em tempo real
docker stats

# Uso de disco por container
docker system df -v
```

### Limitar Recursos

```yaml
services:
  identity-api:
    deploy:
      resources:
        limits:
          cpus: '0.5'
          memory: 512M
        reservations:
          memory: 256M
```

---

## Próximos Passos

Após ter o ambiente rodando com Docker Compose:

1. **Implementar Health Checks** nas APIs (Fase 3)
2. **Configurar Polly** para resiliência (Fase 3)
3. **Deploy em Kubernetes** (Fase 4)
4. **CI/CD com GitHub Actions** (Fase 5)

---

## Suporte

- **Issues**: https://github.com/your-org/peo-platform/issues
- **Documentação**: https://docs.peo-platform.com
- **Slack**: #peo-platform-support

---

**Última atualização**: 09/11/2025
