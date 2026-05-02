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
4. `SettlementCalculator` нормализует `totalIncome` до двух знаков и распределяет сумму так, чтобы сумма всех строк `settlements` совпадала с общим доходом периода.
5. Если калькулятор вернул непустой список строк, в outbox пишется событие `PERIOD_SETTLEMENT_READY`.
6. Период переводится в статус `SETTLED`.
7. Создается новый период на следующий месяц со статусом `ACTIVE`.
8. Для нового периода заново создаются слоты на основе шаблона долей.

## Когда событие `PERIOD_SETTLEMENT_READY` не создается
Событие не пишется, если:

- нет BOOKED-слотов,
- `totalIncome <= 0`,
- или калькулятор не смог построить settlement lines.

То есть закрытие периода и публикация события разделены: период все равно будет переведен в `SETTLED`.

`applyBookingConfirmed` уже должен был накопить только реальный доход за слоты, попавшие в активный период. Поэтому settlement работает с уже очищенной и нормализованной базой дохода.

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

Полный payload выглядит так:

```json
{
  "periodId": "3f1a2b4c-5d6e-7f80-1234-abcdef012345",
  "coownershipListingId": "b1e2c3d4-5f60-7a81-2345-abcdef012346",
  "rentalListingId": "9a8b7c6d-5e4f-3a21-0123-abcdef012347",
  "startDate": "2026-04-01",
  "endDate": "2026-04-30",
  "totalIncome": 1000.00,
  "settlements": [
    {
      "ownerId": "b8d5c8d1-0f7c-4c5d-85df-9b8e2bbf0a11",
      "bookedSlots": 12,
      "amount": 600.00
    },
    {
      "ownerId": "f1e2d3c4-6b70-8a91-3456-abcdef012348",
      "bookedSlots": 8,
      "amount": 400.00
    }
  ]
}
```

Примечания:
- Числа с плавающей точкой в поле `amount` имеют два знака после точки.
- `totalIncome` — число с двумя знаками (например, 1000.00). При сериализации в JSON тип `BigDecimal` представляется как число.
- `settlements` является списком объектов, порядок владельцев соответствует сохранённой итерации (в коде используется `LinkedHashMap`).
- Если округление на отдельных строках создаёт остаток, он добавляется в последнюю строку, чтобы сумма `amount` совпадала с `totalIncome`.

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
  - проверяет settlement ready payload и сохранение суммы после округления

