# Фаза 2: заявки на доли и их одобрение (gRPC)

## Назначение
Пользователи подают заявки на покупку долей в уже созданном листинге (Фаза 1). Владелец листинга получает уведомления и может одобрить или отклонить заявку. После одобрения заявки и полного распределения всех долей запускается Фаза 3.

## Общая схема Фазы 2

```
1. User A подает заявку на 2 доли через gRPC CreateShareApplication
   ↓
2. Владелец получает уведомление
   ↓
3. Владелец одобряет/отклоняет заявку через ApproveShareApplication или RejectShareApplication
   ↓
4. Если одобрено и все доли распределены → листинг переводится в статус FILLED
   ↓
5. Фаза 3: автоматически запускается создание периода и COOWNERSHIP_FILLED_OUT событие
```

## Вход 1: Подача заявки (CreateShareApplication)

### RPC метод

```protobuf
rpc CreateShareApplication(CreateShareApplicationRequest) returns (ShareApplicationResponse);
```

### Структура запроса

```protobuf
message CreateShareApplicationRequest {
  string listing_id = 1;              // UUID листинга (обязателен)
  string applicant_id = 2;            // UUID заявителя (обязателен)
  int32 shares_count = 3;             // Количество долей (обязателен, > 0)
}
```

### Примеры полей

- `listing_id`: UUID листинга из Фазы 1
- `applicant_id`: UUID пользователя, подающего заявку
- `shares_count`: от 1 до (total_shares - already_filled)

### Структура ответа

```protobuf
message ShareApplicationResponse {
  string id = 1;                      // UUID заявки (сгенерирован сервисом)
  string listing_id = 2;              // UUID листинга
  string applicant_id = 3;            // UUID заявителя
  int32 shares_count = 4;             // Количество долей в заявке
  string status = 5;                  // Статус: PENDING, APPROVED, REJECTED
}
```

### Пример ответа

```json
{
  "id": "c5d6e7f8-0a1b-4c23-d456-e7f8a9b0c1d2",
  "listing_id": "b3c4d5e6-f7a8-4901-c123-d4e5f6a7b8c9",
  "applicant_id": "user-uuid-1234",
  "shares_count": 2,
  "status": "PENDING"
}
```

## Бизнес-логика подачи заявки

1. **Проверка листинга**:
   - Листинг должен существовать
   - Статус листинга должен быть `OPEN` (еще не заполнены все доли)

2. **Проверка заявителя**:
   - Заявитель не может быть владельцем листинга
   - От одного заявителя может быть только одна активная заявка одновременно

3. **Проверка доступности долей**:
   - Запрашиваемое количество долей не должно превышать количество свободных долей
   - Формула: `requested_shares <= available_shares`

4. **Создание заявки**:
   - Статус заявки: `PENDING`
   - Создается уведомление для владельца листинга

5. **События**:
   - В Outbox сохраняется событие `SHARE_APPLICATION_CREATED`
   - Это событие позже публикуется в Kafka (Фаза 4)

## Возвращаемые значения CreateShareApplication

**Успех** (gRPC код: `OK`):
- Объект `ShareApplicationResponse` со статусом `PENDING`

**Ошибки**:
- `INVALID_ARGUMENT`:
  - `listing_id` не валидный UUID
  - `applicant_id` не валидный UUID
  - `shares_count <= 0`
  
- `NOT_FOUND`:
  - Листинг не найден

- `FAILED_PRECONDITION`:
  - Листинг закрыт для заявок (статус != OPEN)
  - Заявитель — это владелец листинга
  - Заявка от этого заявителя уже существует
  - Нет достаточного количества свободных долей

## Вход 2: Одобрение заявки (ApproveShareApplication)

### RPC метод

```protobuf
rpc ApproveShareApplication(OwnerActionRequest) returns (ShareApplicationResponse);
```

### Структура запроса

```protobuf
message OwnerActionRequest {
  string application_id = 1;          // UUID заявки (обязателен)
  string owner_id = 2;                // UUID владельца листинга (обязателен)
}
```

### Пример запроса

```json
{
  "application_id": "c5d6e7f8-0a1b-4c23-d456-e7f8a9b0c1d2",
  "owner_id": "a1b2c3d4-e5f6-4789-b012-c3d4e5f67890"
}
```

### Структура ответа

```json
{
  "id": "c5d6e7f8-0a1b-4c23-d456-e7f8a9b0c1d2",
  "listing_id": "b3c4d5e6-f7a8-4901-c123-d4e5f6a7b8c9",
  "applicant_id": "user-uuid-1234",
  "shares_count": 2,
  "status": "APPROVED"
}
```

## Бизнес-логика одобрения

1. **Проверка прав**:
   - `owner_id` должен быть владельцем листинга
   - Если `owner_id` не совпадает — ошибка `PERMISSION_DENIED`

2. **Проверка заявки**:
   - Заявка должна существовать
   - Статус заявки должен быть `PENDING`
   - Если статус уже `APPROVED` — возвращается текущее состояние

3. **Распределение долей**:
   - Сервис отбирает N свободных долей (где ownerId = null)
   - Каждой доле устанавливается `ownerId = applicant_id`
   - Доли помечаются как `locked = false` (готовы к использованию)

4. **Обновление листинга**:
   - Счетчик `filledShares += shares_count`
   - Если `filledShares >= totalShares` — листинг переходит в статус `FILLED`

5. **Истинны все доли, переход в FILLED**:
   - Все оставшиеся свободные доли помечаются `locked = true`
   - Выполняется `PeriodLifecycleService.triggerFilledOut(listing)`
   - **Это создает Period (Фаза 3) и публикует COOWNERSHIP_FILLED_OUT событие**

6. **Уведомления**:
   - Создается уведомление для заявителя: `SHARE_APPLICATION_APPROVED`
   - В Outbox сохраняется событие `SHARE_APPLICATION_APPROVED`

## Возвращаемые значения ApproveShareApplication

**Успех** (gRPC код: `OK`):
- Объект `ShareApplicationResponse` со статусом `APPROVED`

**Ошибки**:
- `INVALID_ARGUMENT`:
  - `application_id` не валидный UUID
  - `owner_id` не валидный UUID

- `NOT_FOUND`:
  - Заявка не найдена
  - Листинг не найден (странно, но проверяется)

- `PERMISSION_DENIED`:
  - `owner_id` не является владельцем листинга

- `FAILED_PRECONDITION`:
  - Листинг закрыт (статус != OPEN)
  - Недостаточно свободных долей (ошибка race condition, например)

## Вход 3: Отклонение заявки (RejectShareApplication)

### RPC метод

```protobuf
rpc RejectShareApplication(OwnerActionRequest) returns (ShareApplicationResponse);
```

используется тот же `OwnerActionRequest`.

### Структура ответа

```json
{
  "id": "c5d6e7f8-0a1b-4c23-d456-e7f8a9b0c1d2",
  "listing_id": "b3c4d5e6-f7a8-4901-c123-d4e5f6a7b8c9",
  "applicant_id": "user-uuid-1234",
  "shares_count": 2,
  "status": "REJECTED"
}
```

## Бизнес-логика отклонения

1. **Проверка прав**:
   - `owner_id` должен быть владельцем листинга
   - Если нет — ошибка `PERMISSION_DENIED`

2. **Проверка статуса**:
   - Если STATUS = `APPROVED` — нельзя отклонить (ошибка `FAILED_PRECONDITION`)
   - Если STATUS = `REJECTED` — возвращается текущее состояние, ошибок нет
   - Если STATUS = `PENDING` — может быть отклонено

3. **Обновление статуса**:
   - Заявка переходит в статус `REJECTED`

4. **Уведомления**:
   - Создается уведомление для заявителя: `SHARE_APPLICATION_REJECTED`
   - В Outbox сохраняется событие `SHARE_APPLICATION_REJECTED`

## Возвращаемые значения RejectShareApplication

**Успех** (gRPC код: `OK`):
- Объект `ShareApplicationResponse` со статусом `REJECTED`

**Ошибки**: аналогичны `ApproveShareApplication`

## 📬 Уведомления для владельца (GetOwnerNotifications)

### RPC метод

```protobuf
rpc GetOwnerNotifications(GetOwnerNotificationsRequest) returns (GetOwnerNotificationsResponse);
```

### Структура запроса

```protobuf
message GetOwnerNotificationsRequest {
  string owner_id = 1;                // UUID владельца (обязателен)
}
```

### Структура ответа

```protobuf
message GetOwnerNotificationsResponse {
  repeated ShareApplicationNotification notifications = 1;
}

message ShareApplicationNotification {
  string id = 1;                      // UUID уведомления
  string recipient_id = 2;            // UUID получателя уведомления
  string application_id = 3;          // UUID заявки
  string listing_id = 4;              // UUID листинга
  string owner_id = 5;                // UUID владельца листинга
  string applicant_id = 6;            // UUID заявителя
  int32 shares_count = 7;             // Количество долей в заявке
  string application_status = 8;      // Статус заявки (PENDING/APPROVED/REJECTED)
  string event_type = 9;              // Тип события (SHARE_APPLICATION_CREATED/APPROVED/REJECTED)
  google.protobuf.Timestamp created_at = 10;   // Время создания уведомления (UTC)
  google.protobuf.Timestamp expires_at = 11;   // Время истечения уведомления (UTC)
}
```

### Пример ответа

```json
{
  "notifications": [
    {
      "id": "notif-uuid-1",
      "recipient_id": "owner-uuid",
      "application_id": "app-uuid-1",
      "listing_id": "listing-uuid",
      "owner_id": "owner-uuid",
      "applicant_id": "user-uuid-1",
      "shares_count": 2,
      "application_status": "PENDING",
      "event_type": "SHARE_APPLICATION_CREATED",
      "created_at": "2026-05-01T10:00:00Z",
      "expires_at": "2026-05-08T10:00:00Z"
    }
  ]
}
```

## Уведомления и их жизненный цикл

- **Создание**: когда подается заявка на доли
- **Время жизни**: 7 дней (configurable через properties)
- **Автоудаление**: ночной job `NotificationPurgeScheduler` удаляет истекшие уведомления
- **Polling**: C# сторона регулярно вызывает `GetOwnerNotifications` для получения новых заявок

## Жизненный цикл заявки

```
PENDING
  ├─ [Одобрено] → APPROVED + доли назначены заявителю + может быть FILLED
  └─ [Отклонено] → REJECTED
```

## Параллелизм и блокировки

- **Листинг**: используется `PESSIMISTIC_WRITE` lock для предотвращения race conditions
- **Доли**: выбираются с locking для гарантии, что одни доли не будут выданы нескольким заявкам
- **Сериализация**: если два одобрения пытаются выдать оставшиеся доли параллельно, гарантируется правильное распределение

## Что важно C# стороне

1. **Workflow**:
   ```
   CreateListing (Фаза 1)
      ↓
   CreateShareApplication (Фаза 2) → PENDING
      ↓
   GetOwnerNotifications (polling для владельца)
      ↓
   ApproveShareApplication или RejectShareApplication (действие владельца)
      ↓
   Если все доли распределены → triggerFilledOut (автоматически) → Фаза 3
   ```

2. **Обработка ошибок**:
   - Ловите `RpcException` с проверкой `ex.Status.Code`
   - `INVALID_ARGUMENT` — проверьте формат данных
   - `NOT_FOUND` — сущность удалена или неверный ID
   - `PERMISSION_DENIED` — неверный owner_id
   - `FAILED_PRECONDITION` — нарушено бизнес-требование

3. **UUID**: все IDs как строки в формате `"xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"`

4. **Timestamp**: используется `google.protobuf.Timestamp` (UTC)

5. **Polling**: рекомендуется вызывать `GetOwnerNotifications` каждые 5-30 секунд для получения новых заявок

## Тестирование

Тесты для Phase 2 находятся в:
- `src/test/java/ru/veshvokrug/coownership/input/grpc/CoownershipGrpcServiceTest.java`
  - `createShareApplicationMapsRequestAndReturnsApplicationId` — создание заявки
  - `approveShareApplicationInvokesServiceAndReturnsUpdatedApplication` — одобрение
  - `rejectShareApplicationInvokesServiceAndReturnsUpdatedApplication` — отклонение
  
- `src/test/java/ru/veshvokrug/coownership/service/ListingServiceTest.java`
  - Множество тестов для бизнес-логики

- `src/test/java/ru/veshvokrug/coownership/service/ListingServiceIntegrationTest.java`
  - `createShareApplicationCreatesPendingNotificationForOwnerPolling` — создание и уведомление
  - `approveShareApplicationsInParallelSerializesListingUpdateAndFillsAllShares` — параллельная обработка

Все тесты **PASSED**.

## Переход в Фазу 3

Когда последняя заявка одобрена и `filledShares == totalShares`:
- Листинг переходит в статус `FILLED`
- Автоматически создается Period с периодом текущего месяца
- Period получает статус `ACTIVE`
- Создаются Slots (дни периода с заявленным владельцем)
- В Outbox сохраняется событие `COOWNERSHIP_FILLED_OUT`
- Фаза 3: Kafka consumer обработает событие `rental-listing-created` и свяжет слоты с арендой


