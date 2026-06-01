# Интеграция с `catalog-service`

Документ описывает, какие события `catalog-service` должен публиковать в Kafka для `recommendation-service`.

## Куда отправлять

- Топик: `recommendation_events`
- Ключ сообщения: `userId` (рекомендуется)
- Формат: JSON, контракт в `KAFKA_EVENTS.md`

## Какие события отправлять

### `ListingViewed`
Отправляется при просмотре карточки объявления.

Поля:
- `listingId` — обязателен
- `categorySlug` — обязателен

Рекомендация: применяйте debounce, чтобы не отправлять частые повторы (например, не чаще 1 события на 30 секунд для пары `userId + listingId`).

### `SearchPerformed`
Отправляется при поиске/фильтрации по категории.

Поля:
- `listingId` — `null`
- `categorySlug` — обязателен

### `ListingFavorited`
Отправляется при добавлении объявления в избранное.

Поля:
- `listingId` — обязателен
- `categorySlug` — обязателен

## Пример DTO

```json
{
  "eventId": "uuid",
  "userId": "user-123",
  "eventType": "ListingFavorited",
  "categorySlug": "tools",
  "listingId": "listing-456",
  "timestamp": 1715425600000
}
```

## Пример отправки (Spring Kafka)

```java
RecommendationEventDto event = new RecommendationEventDto(
    UUID.randomUUID().toString(),
    userId,
    "ListingViewed",
    categorySlug,
    listingId,
    System.currentTimeMillis()
);
kafkaTemplate.send("recommendation_events", userId, event);
```

## Проверочный список

- Событие отправляется после успешной операции в БД.
- `categorySlug` не пустой.
- `eventType` строго из контрактного списка.
- Для `SearchPerformed` обязательно передаётся категория.

## Быстрая проверка

```bash
kafka-console-consumer --topic recommendation_events \
  --bootstrap-server localhost:9092 \
  --from-beginning
```
