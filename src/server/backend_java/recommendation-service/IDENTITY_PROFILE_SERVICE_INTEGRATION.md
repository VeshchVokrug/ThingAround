# Интеграция Recommendation Service с identity-profile-service

Этот документ описывает, какие события identity-profile-service должен публиковать в Kafka для сервиса рекомендаций.

## Событие

### UserCategoriesUpdated
- **Источник:** identity-profile-service
- **Вес интереса:** 3.0
- **Вес популярности:** 0.0

Событие отправляется, когда пользователь явно обновляет свои любимые категории в профиле.

## Куда отправлять

- Топик: `recommendation_events`
- Ключ сообщения: `userId` (рекомендуется)

## Когда отправлять

Событие публикуется, когда пользователь сохраняет или обновляет любимые категории в профиле.

Рекомендуемый подход:
- отправлять по одному событию на каждую выбранную категорию;
- отправлять только после успешного сохранения профиля.

## Формат сообщения

```json
{
  "eventId": "uuid",
  "userId": "user-123",
  "eventType": "UserCategoriesUpdated",
  "categorySlug": "electronics",
  "listingId": null,
  "timestamp": 1715425600000
}
```

## Важные правила

- `listingId` для этого события должен быть `null`.
- `categorySlug` обязателен.
- Если пользователь выбрал несколько категорий, отправьте несколько сообщений.
- `eventType` должен быть строго `UserCategoriesUpdated`.

## Пример отправки

```java
RecommendationEventDto event = new RecommendationEventDto(
    UUID.randomUUID().toString(),
    userId,
    "UserCategoriesUpdated",
    categorySlug,
    null,
    System.currentTimeMillis()
);
kafkaTemplate.send("recommendation_events", userId, event);
```
