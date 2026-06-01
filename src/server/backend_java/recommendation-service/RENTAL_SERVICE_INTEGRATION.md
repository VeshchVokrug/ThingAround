# Интеграция с `rental-service`

Документ описывает события, которые `rental-service` должен отправлять в Kafka для `recommendation-service`.

## Куда отправлять

- Топик: `recommendation_events`
- Ключ сообщения: `userId` (рекомендуется)

## События

### `BookingCreated`
Отправляется при создании бронирования.

### `BookingConfirmed`
Отправляется при подтверждении бронирования.

### `BookingCompleted`
Отправляется после успешного завершения аренды.

### `BookingCancelled`
Отправляется при отмене бронирования.

## Обязательные поля

Для всех событий `rental-service`:

- `eventId`
- `userId` (арендатор)
- `eventType`
- `categorySlug`
- `listingId`
- `timestamp`

## Пример сообщения

```json
{
  "eventId": "uuid",
  "userId": "user-123",
  "eventType": "BookingConfirmed",
  "categorySlug": "sports",
  "listingId": "listing-456",
  "timestamp": 1715425600000
}
```

## Пример отправки

```java
RecommendationEventDto event = new RecommendationEventDto(
    UUID.randomUUID().toString(),
    renterUserId,
    "BookingConfirmed",
    categorySlug,
    listingId,
    System.currentTimeMillis()
);
kafkaTemplate.send("recommendation_events", renterUserId, event);
```

## Проверочный список

- Событие отправляется только после успешной фиксации статуса в БД.
- `categorySlug` и `listingId` всегда заполнены.
- `eventType` соответствует одному из четырёх типов выше.
- При отмене используйте именно `BookingCancelled`.
