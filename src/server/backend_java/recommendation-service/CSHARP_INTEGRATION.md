# Интеграция с C# сервисом

Этот документ описывает три способа интеграции Java `recommendation-service` с C# микросервисами.

## Три способа интеграции

### 1. gRPC (рекомендуется для низкой задержки)

**Порт:** 50054
**Язык определения:** Protocol Buffers (`.proto` файлы)

#### RPC методы

```protobuf
service RecommendationService {
  // Получить рекомендации для пользователя
  rpc GetRecommendations(GetRecommendationsRequest) returns (GetRecommendationsResponse);
  
  // Опубликовать событие
  rpc PublishRecommendationEvent(PublishRecommendationEventRequest) returns (PublishRecommendationEventResponse);
}
```

#### Пример использования из C#

```csharp
using Grpc.Net.Client;
using Recommendation.Protos;

// Подключение
var channel = GrpcChannel.ForAddress("http://localhost:50054");
var client = new RecommendationService.RecommendationServiceClient(channel);

// Получить рекомендации
var request = new GetRecommendationsRequest 
{ 
    UserId = "user-123",
    Size = 10
};
var response = await client.GetRecommendationsAsync(request);
Console.WriteLine($"Recommendations: {string.Join(",", response.Listings)}");

// Опубликовать событие
var eventRequest = new PublishRecommendationEventRequest
{
    EventId = Guid.NewGuid().ToString(),
    UserId = "user-123",
    EventType = "ListingViewed",
    CategorySlug = "sports",
    ListingId = "listing-456",
    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
};
var eventResponse = await client.PublishRecommendationEventAsync(eventRequest);
```

#### Генерация C# кода из proto

```bash
# Установить protobuf компилятор
dotnet tool install -g grpc_csharp_plugin

# Генерировать код
protoc -I /path/to/recommendation_service.proto \
    --csharp_out=/path/to/csharp/project \
    --grpc_csharp_out=/path/to/csharp/project \
    recommendation_service.proto
```

### 2. REST API (совместимость, медленнее чем gRPC)

**Порт:** 8084
**Базовый URL:** `http://localhost:8084`

#### Получить рекомендации

```http
GET /api/v2/recommendations?userId={userId}&size={size}
```

Ответ:
```json
{
  "userId": "user-123",
  "listings": ["listing-1", "listing-2"],
  "count": 2,
  "timestamp": 1715425600000
}
```

#### Опубликовать событие

```http
POST /api/v2/recommendations/events
Content-Type: application/json

{
  "eventId": "uuid",
  "userId": "user-123",
  "eventType": "ListingViewed",
  "categorySlug": "sports",
  "listingId": "listing-456",
  "timestamp": 1715425600000
}
```

Ответ:
```json
{
  "success": true,
  "message": "Event published successfully"
}
```

#### Пример на C#

```csharp
using System.Net.Http.Json;

var client = new HttpClient { BaseAddress = new Uri("http://localhost:8084") };

// Получить рекомендации
var response = await client.GetAsync("/api/v2/recommendations?userId=user-123&size=10");
var json = await response.Content.ReadAsAsync<dynamic>();

// Опубликовать событие
var eventData = new
{
    eventId = Guid.NewGuid().ToString(),
    userId = "user-123",
    eventType = "ListingViewed",
    categorySlug = "sports",
    listingId = "listing-456",
    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
};
var eventResponse = await client.PostAsJsonAsync("/api/v2/recommendations/events", eventData);
```

### 3. RabbitMQ (для асинхронной интеграции)

**Host:** localhost
**Port:** 5672
**Username:** guest
**Password:** guest

#### Очереди

- **Входящие события:** `recommendation.events.queue`
  - Exchange: `recommendation.events.exchange`
  - Routing Key: `recommendation.event.*`

- **Исходящие ответы:** `recommendations.response.queue`
  - Exchange: `recommendations.response.exchange`
  - Routing Key: `recommendations.response`

#### Формат сообщения в RabbitMQ

```json
{
  "eventId": "uuid",
  "userId": "user-123",
  "eventType": "ListingViewed",
  "categorySlug": "sports",
  "listingId": "listing-456",
  "timestamp": 1715425600000
}
```

#### Пример на C# с RabbitMQ

```csharp
using RabbitMQ.Client;
using System.Text.Json;

var factory = new ConnectionFactory() { HostName = "localhost" };
using var connection = factory.CreateConnection();
using var channel = connection.CreateModel();

// Объявить exchange и очередь
channel.ExchangeDeclare("recommendation.events.exchange", ExchangeType.Direct);
channel.QueueDeclare("recommendation.events.queue", true, false, false);

// Опубликовать событие
var eventData = new
{
    eventId = Guid.NewGuid().ToString(),
    userId = "user-123",
    eventType = "ListingViewed",
    categorySlug = "sports",
    listingId = "listing-456",
    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
};

var json = JsonSerializer.Serialize(eventData);
var body = Encoding.UTF8.GetBytes(json);

channel.BasicPublish(
    exchange: "recommendation.events.exchange",
    routingKey: "recommendation.event.listing_viewed",
    basicProperties: null,
    body: body
);
```

## Рекомендации по выбору способа

| Способ | Вариант использования | Задержка | Сложность |
|--------|----------------------|----------|-----------|
| **gRPC** | Синхронные запросы, низкая задержка | ⚡ Низкая | 🟡 Средняя |
| **REST** | Несинхронные запросы, простая интеграция | 🟡 Средняя | 🟢 Низкая |
| **RabbitMQ** | Асинхронные события, высокая отказоустойчивость | 🔴 Высокая | 🟡 Средняя |

## Проверка подключения

```bash
# Проверить REST
curl http://localhost:8084/actuator/health

# Проверить gRPC (используя grpcurl)
grpcurl -plaintext localhost:50054 list

# Проверить RabbitMQ
rabbitmq-diagnostics -q ping
```

## Безопасность

- Используйте TLS для gRPC в production: `.UseTransportSecurity()`
- Используйте аутентификацию для RabbitMQ
- Добавьте валидацию и авторизацию для всех эндпоинтов

## Примеры интеграции

- **Синхронная выдача рекомендаций:** используйте gRPC `GetRecommendations`
- **Сбор событий из C#:** используйте gRPC `PublishRecommendationEvent` или REST POST
- **Интеграция нескольких сервисов:** используйте RabbitMQ для асинхронной обработки


