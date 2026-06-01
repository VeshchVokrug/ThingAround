# recommendation-service

Сервис персональных рекомендаций для платформы аренды вещей.

Он получает события активности из Kafka, обновляет агрегаты в Redis и возвращает список `listingId` для пользователя.

## Что делает сервис

- Слушает Kafka-топик `recommendation_events`.
- Агрегирует интерес пользователя по категориям (`user:{userId}:cat_weights`, Redis Hash).
- Агрегирует популярность объявлений по категориям (`pop:{categorySlug}`, Redis ZSet).
- Раз в сутки применяет затухание весов (time decay).
- Отдаёт рекомендации по REST: `GET /api/v1/recommendations?userId={userId}&size={size}`.

## Ключевая бизнес-логика

1. Берём топ-K категорий пользователя по весу.
2. Для каждой категории берём топ-M объявлений по популярности.
3. Собираем результат в round-robin (чтобы выдача была смешанной по категориям).
4. Удаляем дубликаты `listingId` и ограничиваем размер ответа `size`.
5. Кэшируем результат в Redis на 5 минут (ключ `rec:{userId}:{size}`).

## События Kafka

Полный контракт событий: `KAFKA_EVENTS.md`.

Топик: `recommendation_events`.

Базовые поля:

```json
{
  "eventId": "uuid",
  "userId": "user-123",
  "eventType": "ListingViewed",
  "categorySlug": "sports",
  "listingId": "listing-456",
  "timestamp": 1715425600000
}
```

## Redis-структуры

- `user:{userId}:cat_weights` — Hash (`categorySlug` -> `double`)
- `pop:{categorySlug}` — ZSet (`listingId` -> `score`)
- `rec:{userId}:{size}` — String (JSON-массив `listingId`)

## REST API

### Получить рекомендации

`GET /api/v1/recommendations?userId={userId}&size={size}`

- `userId` — обязательный и не пустой
- `size` — необязательный, если `<= 0`, используется значение по умолчанию из конфига

Ошибки:
- `400 Bad Request` — если `userId` отсутствует или пустой

Пример:

```bash
curl -X GET "http://localhost:8084/api/v1/recommendations?userId=user-123&size=10"
```

Ответ:

```json
{
  "userId": "user-123",
  "listings": ["listing-1", "listing-2"],
  "count": 2,
  "timestamp": 1715425600000
}
```

## Настройки

Основные параметры в `src/main/resources/application.yaml`:

```yaml
recommendation:
  weights:
    user-interest-half-life-days: 30
    listing-popularity-half-life-days: 14
    min-category-weight-threshold: 0.001
    top-categories-count: 5
    top-listings-per-category: -1
    default-recommendation-size: 20
    recommendation-cache-ttl-seconds: 300
```

## Локальный запуск

```bash
cd /Users/dimamarch/ThingAround/src/server/backend_java/recommendation-service
mvn clean test
mvn spring-boot:run
```

## Проверка работоспособности

```bash
curl "http://localhost:8084/actuator/health"
curl "http://localhost:8084/api/v1/recommendations?userId=user-123&size=10"
```

## OpenAPI / Swagger UI

- OpenAPI JSON: `GET /v3/api-docs`
- Swagger UI: `GET /swagger-ui/index.html`


## Тесты

```bash
cd /Users/dimamarch/ThingAround/src/server/backend_java/recommendation-service
mvn test
```

## Документы по интеграции

- `KAFKA_EVENTS.md` — единый контракт Kafka-событий.
- `CATALOG_SERVICE_INTEGRATION.md` — что и когда отправляет `catalog-service`.
- `RENTAL_SERVICE_INTEGRATION.md` — что и когда отправляет `rental-service`.
- `IDENTITY_PROFILE_SERVICE_INTEGRATION.md` — что и когда отправляет `identity-profile-service`.
