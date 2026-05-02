# Фаза 4: исходящие события через Outbox

## Назначение
Эта фаза тоже не использует gRPC. Доменные операции записывают событие сначала в таблицу `outbox`, а отдельный relay позже публикует его в Kafka.

Такой подход нужен, чтобы изменения в бизнес-таблицах и запись события были атомарными в одной транзакции.

## Что хранится в `outbox`
У сущности `OutboxMessage` есть следующие поля:

- `id` — UUID записи, он же `eventId` в Kafka-envelope.
- `event_type` — строковый тип события.
- `payload` — JSON с полезной нагрузкой события.
- `published_at` — момент успешной публикации.
- `attempt_count` — число неудачных попыток.
- `next_attempt_at` — время следующей попытки публикации.
- `last_error` — текст последней ошибки публикации.

## Конфигурация relay
- Topic: `coownership-events` (`coownership.kafka.outbox.topic` или legacy `COOWNERSHIP_KAFKA_OUTBOX_TOPIC`)
- Интервал запуска: `coownership.outbox.relay-interval-ms` (по умолчанию `5000`)
- Размер батча: `coownership.outbox.batch-size` (по умолчанию `100`)
- Backoff между попытками: `coownership.outbox.retry-delay-seconds` (по умолчанию `30`)

## Как выглядит Kafka-envelope
В Kafka публикуется не голый payload, а envelope:

```json
{
  "eventId": "0d67e9b5-4d7c-4cb4-8f72-9c9bb5d2bb2b",
  "eventType": "SHARE_APPLICATION_CREATED",
  "payload": {
    "applicationId": "...",
    "listingId": "...",
    "ownerId": "...",
    "applicantId": "...",
    "sharesCount": 2,
    "status": "PENDING"
  }
}
```

- `eventId` — UUID outbox-записи.
- `eventType` — доменное имя события.
- `payload` — JSON-модель конкретного события.

Kafka key при отправке равен `eventId`. Это удобно для дедупликации и для сохранения порядка по ключу.

## Какие события публикуются через outbox
Текущая реализация пишет в outbox следующие доменные события:

- `SHARE_APPLICATION_CREATED`
- `SHARE_APPLICATION_APPROVED`
- `SHARE_APPLICATION_REJECTED`
- `COOWNERSHIP_FILLED_OUT`
- `SLOT_OWNERSHIP_CHANGED`
- `SLOT_REASSIGNED`
- `PERIOD_SETTLEMENT_READY`

## Поведение relay
`OutboxRelayScheduler` запускает `OutboxRelayService.publishNextBatch()` по фиксированной задержке.

Алгоритм:
1. Берутся записи, у которых `published_at IS NULL` и `next_attempt_at <= now`.
2. Сортировка идет по `next_attempt_at`, затем по `id`.
3. Каждая запись сериализуется в envelope.
4. Envelope отправляется в Kafka синхронно через `KafkaTemplate.send(...).get()`.
5. При успехе `published_at` заполняется, `last_error` очищается.
6. При ошибке:
   - `attempt_count` увеличивается на 1
   - `last_error` обрезается до 255 символов
   - `next_attempt_at` сдвигается на retry delay

## Важные свойства доставки
- Доставка является `at-least-once`.
- Одно и то же событие может быть опубликовано повторно после ошибки или повторного запуска relay.
- С consumer-side нужна идемпотентность по `eventId`.

## Что важно C# стороне
1. Считать Kafka-сообщения повторно доставляемыми.
2. Дедуплицировать обработку по `eventId`.
3. Не полагаться на отсутствие дублей на уровне брокера.
4. `payload` нужно читать как JSON-объект, а не как плоский набор полей envelope.

## Тесты
- `src/test/java/ru/veshvokrug/coownership/service/outbox/OutboxRelayServiceTest.java`
  - проверяет успешную публикацию envelope
  - проверяет retry-сценарий и обновление счетчиков

