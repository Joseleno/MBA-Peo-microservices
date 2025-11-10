# Makefile - PEO Platform
# Comandos úteis para gerenciamento da plataforma

.PHONY: help up down restart logs build clean status wait test

# Default target
.DEFAULT_GOAL := help

# Colors for output
BLUE := \033[0;34m
GREEN := \033[0;32m
YELLOW := \033[0;33m
RED := \033[0;31m
NC := \033[0m # No Color

##@ General

help: ## Exibir esta mensagem de ajuda
	@echo "$(BLUE)PEO Platform - Comandos Make$(NC)"
	@echo ""
	@awk 'BEGIN {FS = ":.*##"; printf "$(YELLOW)Uso:$(NC)\n  make $(GREEN)<target>$(NC)\n\n"} /^[a-zA-Z_-]+:.*?##/ { printf "  $(GREEN)%-15s$(NC) %s\n", $$1, $$2 } /^##@/ { printf "\n$(YELLOW)%s$(NC)\n", substr($$0, 5) } ' $(MAKEFILE_LIST)

##@ Docker Compose

up: ## Iniciar todos os serviços em background
	@echo "$(BLUE)🚀 Starting all services...$(NC)"
	docker-compose up -d

up-build: ## Iniciar todos os serviços com rebuild
	@echo "$(BLUE)🚀 Starting all services with rebuild...$(NC)"
	docker-compose up -d --build

down: ## Parar e remover todos os containers (mantém volumes)
	@echo "$(YELLOW)⏬ Stopping all services...$(NC)"
	docker-compose down

down-volumes: ## Parar e remover containers e volumes (APAGA DADOS!)
	@echo "$(RED)⚠️  Stopping all services and removing volumes...$(NC)"
	docker-compose down -v

restart: ## Reiniciar todos os serviços
	@echo "$(BLUE)🔄 Restarting all services...$(NC)"
	docker-compose restart

restart-api: ## Reiniciar apenas as APIs
	@echo "$(BLUE)🔄 Restarting APIs...$(NC)"
	docker-compose restart identity-api gestao-conteudo-api gestao-alunos-api faturamento-api

##@ Logs

logs: ## Ver logs de todos os serviços
	docker-compose logs -f

logs-api: ## Ver logs apenas das APIs
	docker-compose logs -f identity-api gestao-conteudo-api gestao-alunos-api faturamento-api bff

logs-identity: ## Ver logs do Identity API
	docker-compose logs -f identity-api

logs-conteudo: ## Ver logs do Gestão Conteúdo API
	docker-compose logs -f gestao-conteudo-api

logs-alunos: ## Ver logs do Gestão Alunos API
	docker-compose logs -f gestao-alunos-api

logs-faturamento: ## Ver logs do Faturamento API
	docker-compose logs -f faturamento-api

logs-bff: ## Ver logs do BFF
	docker-compose logs -f bff

logs-spa: ## Ver logs do SPA
	docker-compose logs -f spa

logs-infra: ## Ver logs da infraestrutura (SQL + RabbitMQ)
	docker-compose logs -f sqlserver rabbitmq

##@ Build

build: ## Build de todas as imagens
	@echo "$(BLUE)🔨 Building all images...$(NC)"
	docker-compose build

build-parallel: ## Build de todas as imagens em paralelo
	@echo "$(BLUE)🔨 Building all images in parallel...$(NC)"
	docker-compose build --parallel

build-no-cache: ## Build de todas as imagens sem cache
	@echo "$(BLUE)🔨 Building all images without cache...$(NC)"
	docker-compose build --no-cache

build-identity: ## Build apenas Identity API
	docker-compose build identity-api

build-bff: ## Build apenas BFF
	docker-compose build bff

build-spa: ## Build apenas SPA
	docker-compose build spa

##@ Status e Health

status: ## Verificar status de todos os serviços
	@echo "$(BLUE)🔍 Checking services status...$(NC)"
	@docker-compose ps

status-detailed: ## Verificar status detalhado com health checks
	@echo "$(BLUE)🔍 Checking detailed services status...$(NC)"
	@./scripts/check-services.sh --detailed

wait: ## Aguardar todos os serviços ficarem healthy
	@echo "$(BLUE)⏳ Waiting for all services to become healthy...$(NC)"
	@./scripts/wait-for-healthy.sh

stats: ## Ver estatísticas de uso de recursos
	@echo "$(BLUE)📊 Resource usage:$(NC)"
	docker stats --no-stream

##@ Database

db-connect: ## Conectar ao SQL Server via sqlcmd
	docker-compose exec sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "Peo@2025!Strong"

db-list: ## Listar databases
	@echo "$(BLUE)📚 Listing databases...$(NC)"
	@docker-compose exec sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "Peo@2025!Strong" -Q "SELECT name FROM sys.databases" -h -1

db-backup: ## Backup de volumes do SQL Server
	@echo "$(BLUE)💾 Creating SQL Server backup...$(NC)"
	@docker run --rm -v peo-sqlserver-data:/data -v $$(pwd)/backups:/backup alpine tar czf /backup/sqlserver-backup-$$(date +%Y%m%d-%H%M%S).tar.gz /data
	@echo "$(GREEN)✓ Backup created in ./backups/$(NC)"

##@ RabbitMQ

rabbitmq-ui: ## Abrir RabbitMQ Management UI no navegador
	@echo "$(BLUE)🐰 Opening RabbitMQ Management UI...$(NC)"
	@echo "URL: http://localhost:15672"
	@echo "User: peo"
	@echo "Pass: Peo@2025!"

rabbitmq-status: ## Ver status do RabbitMQ
	docker-compose exec rabbitmq rabbitmqctl status

rabbitmq-queues: ## Listar filas do RabbitMQ
	docker-compose exec rabbitmq rabbitmqctl list_queues

##@ Testing

test-health: ## Testar endpoints de health check
	@echo "$(BLUE)🏥 Testing health endpoints...$(NC)"
	@echo "Identity API:" && curl -f http://localhost:5001/health || echo "$(RED)Failed$(NC)"
	@echo "Gestão Conteúdo API:" && curl -f http://localhost:5002/health || echo "$(RED)Failed$(NC)"
	@echo "Gestão Alunos API:" && curl -f http://localhost:5003/health || echo "$(RED)Failed$(NC)"
	@echo "Faturamento API:" && curl -f http://localhost:5004/health || echo "$(RED)Failed$(NC)"
	@echo "BFF:" && curl -f http://localhost:5000/health || echo "$(RED)Failed$(NC)"

test-spa: ## Testar se SPA está respondendo
	@echo "$(BLUE)🌐 Testing SPA...$(NC)"
	@curl -f http://localhost:8081 > /dev/null 2>&1 && echo "$(GREEN)✓ SPA is responding$(NC)" || echo "$(RED)✗ SPA is not responding$(NC)"

##@ Cleanup

clean: ## Limpar containers e networks (mantém volumes e imagens)
	@echo "$(YELLOW)🧹 Cleaning containers and networks...$(NC)"
	docker-compose down

clean-all: ## Limpar tudo (containers, networks, volumes, imagens)
	@echo "$(RED)🧹 Cleaning everything (containers, networks, volumes, images)...$(NC)"
	docker-compose down -v --rmi all

clean-volumes: ## Limpar apenas volumes
	@echo "$(RED)🧹 Cleaning volumes...$(NC)"
	docker volume rm peo-sqlserver-data peo-rabbitmq-data

prune: ## Limpar recursos não utilizados do Docker
	@echo "$(YELLOW)🧹 Pruning unused Docker resources...$(NC)"
	docker system prune -f

prune-all: ## Limpar todos os recursos não utilizados do Docker (incluindo volumes)
	@echo "$(RED)🧹 Pruning all unused Docker resources...$(NC)"
	docker system prune -a -f --volumes

##@ Development

dev-up: ## Iniciar apenas infraestrutura (SQL + RabbitMQ) para dev local
	@echo "$(BLUE)🚀 Starting infrastructure for local development...$(NC)"
	docker-compose up -d sqlserver rabbitmq

dev-down: ## Parar infraestrutura de desenvolvimento
	@echo "$(YELLOW)⏬ Stopping infrastructure...$(NC)"
	docker-compose stop sqlserver rabbitmq

shell-identity: ## Abrir shell no container Identity API
	docker-compose exec identity-api sh

shell-bff: ## Abrir shell no container BFF
	docker-compose exec bff sh

shell-spa: ## Abrir shell no container SPA
	docker-compose exec spa sh

##@ URLs

urls: ## Exibir todas as URLs de acesso
	@echo "$(BLUE)🔗 PEO Platform URLs:$(NC)"
	@echo ""
	@echo "$(GREEN)Frontend:$(NC)"
	@echo "  - SPA:              http://localhost:8081"
	@echo ""
	@echo "$(GREEN)APIs:$(NC)"
	@echo "  - BFF:              http://localhost:5000"
	@echo "  - Identity API:     http://localhost:5001"
	@echo "  - Gestão Conteúdo:  http://localhost:5002"
	@echo "  - Gestão Alunos:    http://localhost:5003"
	@echo "  - Faturamento:      http://localhost:5004"
	@echo ""
	@echo "$(GREEN)Infrastructure:$(NC)"
	@echo "  - RabbitMQ UI:      http://localhost:15672 (peo / Peo@2025!)"
	@echo "  - SQL Server:       localhost:1433 (sa / Peo@2025!Strong)"
	@echo ""
