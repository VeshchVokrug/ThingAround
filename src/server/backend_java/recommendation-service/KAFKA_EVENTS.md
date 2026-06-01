# Kafka-события для recommendation-service

Этот документ описывает контракт событий, которые сервис рекомендаций читает из топика `recommendation_events`.

## Топик

- Имя: `recommendation_events`
- Гарантия доставки: at-least-once
- Рекомендуемый ключ сообщения: `userId`

## Формат сообщения

```json
{
  "eventId": "uuid-v4",
  "userId": "user-123",
  "eventType": "ListingViewed",
  "categorySlug": "sports",
  "listingId": "listing-456",
  "timestamp": 1715425600000
}
```

## Поля

| Поле | Тип | Обязательное | Описание |
|---|---|---|---|
| `eventId` | String | Да | Уникальный идентификатор события |
| `userId` | String | Да | Идентификатор пользователя |
| `eventType` | String | Да | Тип события (список ниже) |
| `categorySlug` | String | Да* | Категория объявления/поиска |
| `listingId` | String | Зависит от `eventType` | Идентификатор объявления |
| `timestamp` | Long | Да | Время события в epoch millis |

`*` Если `categorySlug` отсутствует или пустой, событие игнорируется.

## Валидация `listingId`

- Для `SearchPerformed` и `UserCategoriesUpdated` поле `listingId` должно быть `null`.
- Для всех остальных `eventType` поле `listingId` обязательно и должно быть не пустым.

## Типы событий и веса

| eventType | Источник | Вес интереса пользователя | Вес популярности объявления |
|---|---|---:|---:|
| `UserCategoriesUpdated` | identity-profile-service | 3.0 | 0.0 |
| `ListingViewed` | catalog-service | 0.1 | 1.0 |
| `SearchPerformed` | catalog-service | 0.3 | 0.0 |
| `ListingFavorited` | catalog-service | 0.7 | 5.0 |
| `BookingCreated` | rental-service | 1.5 | 10.0 |
| `BookingConfirmed` | rental-service | 2.0 | 20.0 |
| `BookingCompleted` | rental-service | 2.5 | 25.0 |
| `BookingCancelled` | rental-service | -1.0 | -10.0 |

## Правила публикации

- Отправляйте событие только после успешной бизнес-операции в исходном сервисе.
- Для `ListingViewed` рекомендуется debounce/дедупликация (например, не чаще одного события на 30 секунд для пары `userId + listingId`).
- Для `UserCategoriesUpdated` можно отправлять по одному событию на каждую выбранную категорию.
- Если `listingId` не относится к событию (`SearchPerformed`, `UserCategoriesUpdated`), передавайте `null`. Для остальных типов передавайте непустой `listingId`.

## Минимальная проверка перед отправкой

- `eventId` не пустой
- `userId` не пустой
- `eventType` из разрешённого списка
- `timestamp > 0`
- `categorySlug` заполнен (иначе событие не будет учитываться)

## Пример создания топика

```bash
kafka-topics --create \
  --topic recommendation_events \
  --bootstrap-server localhost:9092 \
  --partitions 3 \
  --replication-factor 1
```

## Быстрая отладка

```bash
kafka-console-consumer --topic recommendation_events \
  --bootstrap-server localhost:9092 \
  --from-beginning
```
