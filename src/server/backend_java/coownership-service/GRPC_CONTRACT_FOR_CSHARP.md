# gRPC контракт для C# клиента

## Proto-файл
- Путь: `src/main/proto/coownership/v1/coownership_service.proto`
- `package`: `coownership.v1`
- `service`: `CoownershipService`

## Изменение контракта
В `CreateListingRequest` удалены поля `name` и `description`.

В proto зарезервированы старые идентификаторы:
- `reserved 1, 2`
- `reserved "name", "description"`

Повторно использовать эти теги и имена нельзя.

## Доступные RPC
- `CreateListing(CreateListingRequest) -> CreateListingResponse`
- `CreateShareApplication(CreateShareApplicationRequest) -> ShareApplicationResponse`
- `ApproveShareApplication(OwnerActionRequest) -> ShareApplicationResponse`
- `RejectShareApplication(OwnerActionRequest) -> ShareApplicationResponse`
- `GetOwnerNotifications(GetOwnerNotificationsRequest) -> GetOwnerNotificationsResponse`

## Форматы полей
- UUID: строка (пример: `2f5f1a0f-6f1f-4ae2-b5e8-2f1b7f3b7d20`)
- `price`: decimal в строке с точкой (пример: `150000.00`)
- `funding_deadline`: строка `yyyy-MM-dd`, поле необязательное
- Время в уведомлениях: `google.protobuf.Timestamp` (UTC)

## Подключение
- gRPC порт сервиса: `9091`
- Имя контейнера в сети docker: `coownership-service`

Внутри docker-сети используйте `http://coownership-service:9091`.
Локально используйте `http://localhost:9091`.

## Минимальные шаги для C#
1. Сгенерировать C# gRPC-клиент из `coownership_service.proto`.
2. Создать `GrpcChannel` на адрес сервиса.
3. Использовать `CoownershipService.CoownershipServiceClient`.

## Маппинг ошибок
Сервис возвращает бизнес-ошибки в gRPC-коды:
- `INVALID_ARGUMENT` — невалидные входные данные
- `NOT_FOUND` — сущность не найдена
- `PERMISSION_DENIED` — нет прав на операцию
- `FAILED_PRECONDITION` — бизнес-конфликт состояния
- `INTERNAL` — внутренняя ошибка

## Материалы по фазам (для C#)
- `docs/csharp/README_PHASE1_CREATE_LISTING.md` — создание листинга совладения (gRPC)
- `docs/csharp/README_PHASE2_SHARE_APPLICATIONS.md` — заявки на доли и одобрение (gRPC + polling)
- `docs/csharp/README_PHASE3_KAFKA.md` — связь с арендой через Kafka
- `docs/csharp/README_PHASE4_OUTBOX.md` — публикация событий через Outbox
- `docs/csharp/README_PHASE5_SLOTS.md` — переназначение дневных слотов
- `docs/csharp/README_PHASE6_SETTLEMENT.md` — ночной settlement и расчет доходов

