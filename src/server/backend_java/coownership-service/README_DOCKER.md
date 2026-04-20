# CoOwnership Service - Docker Setup
## ⚡ Запуск за 30 секунд
```bash
make up
```
## 🔌 Адреса
- **API:** http://localhost:6767/api/v1/co-ownership
- **Swagger:** http://localhost:6767/api/v1/co-ownership/swagger-ui.html
- **БД:** localhost:5432 (admin/admin)
## 📋 Основные команды
```bash
make up        # Запустить
make down      # Остановить
make logs      # Логи сервиса
make ps        # Статус
make shell-db  # Подключиться к БД
make help      # Все команды
```
## 📦 Что запускается
- PostgreSQL 17
- Java REST API (6767)
## 🐛 Проблемы?
```bash
make restart   # Перезапустить
make rebuild   # Пересобрать
```
---
Готово! Фронтенд подключайся на `http://localhost:6767/api/v1/co-ownership`
