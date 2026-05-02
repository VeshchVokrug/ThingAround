# Coownership Service (Docker)

Сервис работает в общей инфраструктуре (`db`, `kafka`, `gateway`) и отдает gRPC API для gateway/других сервисов.

## Быстрый запуск
```bash
docker network create thingaround-network || true
make up
```

## Порты
- gRPC: `localhost:9091`

## Переменные окружения
- `COOWNERSHIP_DB_NAME` (default: `postgres`)
- `COOWNERSHIP_DB_USER` (default: `admin`)
- `COOWNERSHIP_DB_PASSWORD` (default: `admin`)
- `COOWNERSHIP_KAFKA_BOOTSTRAP_SERVERS` (default: `kafka:9092`)

## Команды
```bash
make up
make down
make logs
make ps
```
