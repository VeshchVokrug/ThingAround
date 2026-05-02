# Фаза 3: запуск совладения через Kafka

## Назначение
Эта фаза не использует gRPC. Сервис `coownership-service` слушает входящие Kafka-события от соседних сервисов и связывает активный период совладения с арендным листингом.

На этой стадии сервис не отвечает клиенту синхронным response. Он получает сообщение, проверяет идемпотентность и, если событие новое, выполняет доменное действие внутри транзакции.

## Какие топики слушает сервис

### `rental-listing-created`
- Назначение: Catalog сообщает, что для листинга совладения создан арендный листинг.
- Настройка: `coownership.kafka.inbound.rental-listing-created-topic`
- Consumer group: `coownership-service` по умолчанию через `coownership.kafka.inbound.group-id`

### `booking-confirmed`
- Назначение: Rental/booking сервис сообщает о подтвержденном бронировании.
- Настройка: `coownership.kafka.inbound.booking-confirmed-topic`
- Consumer group: `coownership-service` по умолчанию через `coownership.kafka.inbound.group-id`

## Формат сообщений

### `rental-listing-created`
Сообщение должно быть JSON-объектом с полями:

```json
{
  "eventId": "2f5f1a0f-6f1f-4ae2-b5e8-2f1b7f3b7d20",
  "coownershipListingId": "6d1f09d4-7d3d-4f1c-8f88-4fd70f6f0b0c",
  "rentalListingId": "8a7f52b0-2f5f-45db-8d9d-0c80f26d2ed5"
}
```

- `eventId` — стабильный UUID события для дедупликации.
- `coownershipListingId` — UUID листинга совладения.
- `rentalListingId` — UUID листинга аренды.

### `booking-confirmed`
Сообщение должно быть JSON-объектом с полями:

```json
{
  "eventId": "2f5f1a0f-6f1f-4ae2-b5e8-2f1b7f3b7d20",
  "rentalListingId": "8a7f52b0-2f5f-45db-8d9d-0c80f26d2ed5",
  "startDate": "2026-05-10",
  "endDate": "2026-05-14",
  "totalPrice": 12000.50
}
```

- `startDate`, `endDate` — `yyyy-MM-dd`.
- `totalPrice` — числовое значение JSON, которое Jackson читает как `BigDecimal`.

## Идемпотентность
- Для каждого события используется таблица `processed_events`.
- Ключ дедупликации: `(event_id, consumer_name)`.
- Для фазы 3 используются consumer name:
  - `coownership-rental-listing-created`
  - `coownership-booking-confirmed`
- Повторное сообщение с тем же `eventId` не должно повторно выполнять бизнес-логику.
- Перед проверкой и записью в `processed_events` обработка сериализуется транзакционным lock'ом по ключу `consumerName:eventId`, чтобы конкурентная доставка не выполнила доменное действие дважды.

## Что делает сервис после получения сообщения

### `rental-listing-created`
1. JSON разбирается в record `RentalListingCreatedEvent`.
2. Через `InboundEventIdempotencyService.executeOnce(...)` проверяется, что событие еще не обработано.
3. Если для `coownershipListingId` есть активный период, в нем проставляется `rentalListingId`.
4. Если активного периода нет, операция завершается без ошибки и без изменений.

### `booking-confirmed`
1. JSON разбирается в record `BookingConfirmedEvent`.
2. Идемпотентность проверяется тем же способом.
3. Если найден активный период по `rentalListingId`, в `BOOKED` переводятся только те слоты, которые реально попали в диапазон дат и принадлежат этому периоду.
4. `totalIncome` периода увеличивается только на часть `totalPrice`, соответствующую реально затронутым слотам периода.
5. Если активного периода нет, сообщение не меняет состояние и считается обработанным.
6. Если по указанному диапазону дат внутри периода нет слотов, состояние периода не меняется, и событие считается обработанным без ошибки.

## Ошибки и ограничения
- Некорректный JSON не валит партицию: событие логируется и пропускается без выброса исключения наружу.
- Невалидные UUID или неверный формат дат также приводят к soft-fail-поведению на уровне consumer'а: сообщение логируется как некорректное и не запускает бизнес-логику.
- Эта фаза не возвращает gRPC-коды и не формирует response payload.

## Что важно C# стороне
1. Если C# сервис является источником события, `eventId` должен сохраняться стабильным при повторных доставках.
2. Имена полей должны совпадать с контрактом буквально.
3. Формат дат должен быть ISO-8601 `yyyy-MM-dd`.
4. Числовые значения денег лучше передавать как JSON number, а не строку.
5. Это Kafka-интеграция, не gRPC.

## Тесты
- `src/test/java/ru/veshvokrug/coownership/input/kafka/CoownershipInboundKafkaConsumerTest.java`
  - проверяет обработку `rental-listing-created`
  - проверяет обработку `booking-confirmed`
  - проверяет, что некорректный JSON не вызывает падение consumer'а

