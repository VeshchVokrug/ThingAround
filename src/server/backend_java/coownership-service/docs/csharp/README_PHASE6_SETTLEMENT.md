# Фаза 6: ночной settlement

## Назначение
Это внутренний scheduled job, а не gRPC-контракт. Он закрывает завершенные периоды, считает распределение дохода по BOOKED-слотам и создает следующий период.

Если C# сторона интегрируется с этой фазой, то это происходит не через прямой вызов, а через Kafka-событие `PERIOD_SETTLEMENT_READY`, которое попадает в outbox и потом публикуется relay-ом.

## Расписание
- Cron: `coownership.period.settlement.cron` или legacy `COOWNERSHIP_PERIOD_SETTLEMENT_CRON`
- Значение по умолчанию: `0 0 2 * * *`
- Часовой пояс: UTC
- Защита от параллельного запуска: ShedLock, lock name `periodSettlementJob`

## Что делает scheduler
`PeriodSettlementScheduler.settleClosedPeriods()` просто вызывает доменный метод `PeriodLifecycleService.settleFinishedPeriods()`.

Если внутри возникает исключение, job логирует ошибку и не валит весь процесс планировщика.

## Алгоритм settlement
1. Берутся все периоды со статусом `ACTIVE`, у которых `endDate < today`.
2. Для каждого периода считается количество `BOOKED`-слотов по каждому владельцу.
3. `SettlementCalculator` получает:
   - `totalIncome` периода
   - map `ownerId -> bookedSlots`
4. Если калькулятор вернул непустой список строк, в outbox пишется событие `PERIOD_SETTLEMENT_READY`.
5. Период переводится в статус `SETTLED`.
6. Создается новый период на следующий месяц со статусом `ACTIVE`.
7. Для нового периода заново создаются слоты на основе шаблона долей.

## Когда событие `PERIOD_SETTLEMENT_READY` не создается
Событие не пишется, если:

- нет BOOKED-слотов,
- `totalIncome <= 0`,
- или калькулятор не смог построить settlement lines.

То есть закрытие периода и публикация события разделены: период все равно будет переведен в `SETTLED`.

## Формат payload события
Payload события содержит:

- `periodId`
- `coownershipListingId`
- `rentalListingId`
- `startDate`
- `endDate`
- `totalIncome`
- `settlements`

`settlements` — это массив объектов вида:

```json
{
  "ownerId": "b8d5c8d1-0f7c-4c5d-85df-9b8e2bbf0a11",
  "bookedSlots": 12,
  "amount": 3500.00
}
```

Сумма рассчитывается пропорционально числу BOOKED-слотов с округлением до 2 знаков и `HALF_UP`.

## Что происходит после закрытия периода
Новый месяц создается из шаблона текущего периода:

- `startDate` = первое число следующего месяца
- `endDate` = последнее число следующего месяца
- `status` = `ACTIVE`
- `totalIncome` = `0`

Если у листинга есть доли с `templateDaysMask`, новые слоты назначаются по этому шаблону. Если совпадения нет, используется fallback round-robin по доступным долям.

## Что важно C# стороне
1. Это не gRPC и не Kafka inbound, а scheduled backend job.
2. Для финансовой интеграции читать нужно событие `PERIOD_SETTLEMENT_READY`.
3. Kafka-доставка повторяемая, consumer должен быть идемпотентным по `eventId`.
4. Если settlement lines не пришли, это не ошибка: период просто закрыт.

## Тесты
- `src/test/java/ru/veshvokrug/coownership/service/PeriodLifecycleServiceTest.java`
  - проверяет создание периода и слотов при `triggerFilledOut`
  - проверяет закрытие периода без settlement-события
  - проверяет создание следующего периода

