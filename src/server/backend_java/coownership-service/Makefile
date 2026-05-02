.PHONY: help up down restart logs logs-service ps rebuild clean

help: ## Справка
	@grep -E '^[a-z-]+:.*?## ' $(MAKEFILE_LIST) | awk 'BEGIN {FS = ":.*?## "}; {printf "%-20s %s\n", $$1, $$2}'

up: ## Запустить сервис (ожидаются db/kafka в общей сети thingaround-network)
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
	@echo "gRPC: localhost:9091"

clean: ## Удалить всё (DATA LOSS!)
	docker-compose down -v

.DEFAULT_GOAL := help
