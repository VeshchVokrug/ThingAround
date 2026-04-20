.PHONY: help up down restart logs logs-service ps shell-db

help: ## Справка
	@grep -E '^[a-z-]+:.*?## ' $(MAKEFILE_LIST) | awk 'BEGIN {FS = ":.*?## "}; {printf "%-20s %s\n", $$1, $$2}'

up: ## Запустить сервис
	docker-compose up -d
	@sleep 3 && docker-compose ps

down: ## Остановить сервис
	docker-compose down

restart: down up ## Перезапустить

rebuild: ## Пересобрать образ
	docker-compose build --no-cache
	docker-compose up -d

logs: ## Логи (все контейнеры)
	docker-compose logs -f

logs-service: ## Логи сервиса
	docker-compose logs -f coownership-service

ps: ## Статус контейнеров
	@docker-compose ps
	@echo ""
	@curl -s http://localhost:6767/api/v1/co-ownership/actuator/health | jq '.status' 2>/dev/null || echo "Сервис не готов"

shell-db: ## Подключиться к БД
	docker exec -it coownership-postgres psql -U admin -d coownership

clean: ## Удалить всё (DATA LOSS!)
	docker-compose down -v

.DEFAULT_GOAL := help

