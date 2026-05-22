# Фаза 1: создание листинга совладения (gRPC)

## Назначение
Владелец объекта создает листинг совладения через gRPC API. Сервис инициализирует листинг с определенным количеством финансовых долей, которые позже будут распределены между совладельцами.

## Входящий RPC
- **Метод**: `CreateListing`
- **Протокол**: gRPC
- **Адрес**: `coownership-service:9091` (в docker-сети) или `localhost:9091` (локально)

### Структура запроса

```protobuf
message CreateListingRequest {
  string catalog_listing_id = 3;      // UUID объекта каталога (обязателен)
  string price = 4;                   // Цена в виде decimal-строки (обязателен)
  string owner_id = 5;                // UUID владельца листинга (обязателен)
  int32 total_shares = 6;             // Количество долей (обязателен, от 2 до 10)
  string funding_deadline = 7;        // Дата дедлайна сбора (опционально, формат yyyy-MM-dd)
}
```

### Примеры полей

- `catalog_listing_id`: `"2f5f1a0f-6f1f-4ae2-b5e8-2f1b7f3b7d20"`
- `price`: `"150000.00"` (точка как разделитель, без пробелов)
- `owner_id`: `"a1b2c3d4-e5f6-4789-b012-c3d4e5f67890"`
- `total_shares`: `6`
- `funding_deadline`: `"2026-07-01"` или пустая строка "" (тогда используется +90 дней от текущей даты)

### Структура ответа

```protobuf
message CreateListingResponse {
  string listing_id = 1;              // UUID созданного листинга (сгенерирован сервисом)
}
```

Пример ответа:
```json
{
  "listing_id": "b3c4d5e6-f7a8-4901-c123-d4e5f6a7b8c9"
}
```

## Бизнес-логика создания листинга

1. **Проверка дедлайна**:
   - Если `funding_deadline` пустой или отсутствует, сервис устанавливает дедлайн на текущую дату + 90 дней (UTC).
   - Иначе используется переданное значение.

2. **Проверка дублирования**:
   - Сервис ищет листинг с тем же `catalog_listing_id`.
   - Если листинг уже существует, он возвращается как есть (идемпотентность).

3. **Инициализация долей**:
   - Сервис создает `total_shares` свободных долей.
   - Каждой доле присваивается процент от 100%, распределяется поровну с округлением остатка.
   - Пример: для 6 долей каждая получает 16% (100/6 = 16 остаток 4, первые 4 доли получают +1%).
   - Все доли изначально свободны (`ownerId = null`).

4. **Статус листинга**:
   - Новый листинг получает статус `OPEN` (принимает заявки).
   - `filledShares = 0` (еще нет распределенных долей).

## Возвращаемые значения

**Успех** (gRPC код: `OK`):
- `listing_id`: UUID листинга для дальнейших операций

**Ошибки**:
- `INVALID_ARGUMENT` — невалидные входные данные:
  - `catalog_listing_id` не валидный UUID
  - `owner_id` не валидный UUID
  - `price` не валидный decimal
  - `funding_deadline` не в формате yyyy-MM-dd
  - `total_shares` не в диапазоне 2..10

- `ALREADY_EXISTS` — листинг с таким `catalog_listing_id` уже создан

## Побочные эффекты

### В базе данных
- Таблица `coownership_listing`: создается новая запись
- Таблица `ownership_share`: создается N записей со статусом "свободна"
- **Никаких событий в Kafka** — это обработка только в Java

### Этап готовности
- Листинг остается в статусе `OPEN` и может принимать заявки (Фаза 2).
- Период (`Period`) еще не создается — это произойдет после полного заполнения всех долей.

## Валидация на Java-стороне

```java
// Проверяется:
- UUID формат для всех field'ов
- BigDecimal парсинг price
- LocalDate парсинг funding_deadline (yyyy-MM-dd)
- total_shares: 2 <= value <= 10
```

## Что важно C# стороне

1. **UUID**: передавайте в формате строки (примеры: `"2f5f1a0f-6f1f-4ae2-b5e8-2f1b7f3b7d20"`)
2. **Price**: передавайте как строку decimal с точкой (примеры: `"150000.00"`, `"1000.50"`)
3. **Funding deadline**:
   - Если хотите использовать значение +90 дней — передайте пустую строку `""`
   - Иначе передайте дату в формате `yyyy-MM-dd` (примеры: `"2026-07-01"`)
4. **Total shares**: от 2 до 10
5. **Идемпотентность**: если дважды отправить запрос с одинаковым `catalog_listing_id`, будет возвращен один и тот же `listing_id`
6. **После успешного создания**: используйте `listing_id` для следующей операции (Фаза 2 — заявки на доли)

## Пример C# кода

```csharp
using Grpc.Net.Client;
using YourNamespace.Coownership.V1;

var channel = GrpcChannel.ForAddress("http://localhost:9091");
var client = new CoownershipService.CoownershipServiceClient(channel);

var request = new CreateListingRequest
{
    CatalogListingId = "2f5f1a0f-6f1f-4ae2-b5e8-2f1b7f3b7d20",
    Price = "150000.00",
    OwnerId = "a1b2c3d4-e5f6-4789-b012-c3d4e5f67890",
    TotalShares = 6,
    FundingDeadline = "" // или "2026-07-01"
};

try
{
    var response = await client.CreateListingAsync(request);
    Console.WriteLine($"Листинг создан: {response.ListingId}");
}
catch (RpcException ex)
{
    Console.WriteLine($"Ошибка: {ex.Status.Detail}");
}
```

## Тестирование

Тесты для Phase 1 находятся в:
- `src/test/java/ru/veshvokrug/coownership/input/grpc/CoownershipGrpcServiceTest.java`
  - `createListingMapsRequestWithoutNameDescriptionAndReturnsId` — базовое создание
  - `createListingAcceptsBlankFundingDeadlineAndUsesNullInDto` — пустой дедлайн
- `src/test/java/ru/veshvokrug/coownership/service/ListingServiceIntegrationTest.java`
  - `createListingPersistsListingAndInitialFreeShares` — интеграционный тест

Все тесты **PASSED**.


