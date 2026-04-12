# Gateway

`Gateway` - HTTP API-шлюз для операций аутентификации и профиля в проекте ThingAround.

## Назначение

- Публикует REST API по префиксу `/api/v1/*`
- Проверяет JWT Bearer токены по ключу `Gateway/Configs/public.pem`
- Проксирует бизнес-операции в `IdentityProfileService` по gRPC
- Использует Redis для проверки blacklist токенов

## API методы

### Auth (`/api/v1/identity/auth`)

- `POST /register` - регистрация пользователя
  - Вход: `AuthRequest` (`email`, `password`)
  - Выход: `AuthResponse` (`accessToken`, `refreshToken`, `refreshTokenExpiresHours`)
- `POST /login` - вход пользователя
  - Вход: `AuthRequest`
  - Выход: `AuthResponse`
- `POST /refresh` - обновление пары токенов
  - Вход: `RefreshRequest` (`accessToken`, `refreshToken`)
  - Выход: `AuthResponse`
- `POST /logout` - отзыв refresh-токена
  - Вход: `LogoutRequest` (`refreshToken`)
  - Выход: `204 No Content`

### Profile (`/api/v1/identity/profile`)

- `GET /` - профиль текущего пользователя
  - Выход: `ProfileResponse`
- `GET /{userId}` - публичный профиль по `GUID`
  - Выход: `ProfileResponse`
- `POST /` - создание профиля
  - Вход: `CreateProfileRequest` (`name`, `bio`, `favoriteCategories`)
  - Выход: `ProfileResponse`
- `PUT /` - обновление профиля
  - Вход: `UpdateProfileRequest` (`name?`, `bio?`, `avatarPath?`)
  - Выход: `ProfileResponse`
- `POST /favorite-categories` - добавить избранные категории
  - Вход: `CategoriesRequest` (`categories`)
  - Выход: `ProfileResponse`
- `DELETE /favorite-categories` - удалить избранные категории
  - Вход: `CategoriesRequest`
  - Выход: `ProfileResponse`

Ошибки в API возвращаются в формате `ApiErrorResponse`:

- `statusCode` - HTTP-код
- `message` - описание ошибки

## Запуск локально (dotnet)

Предусловия:

- .NET SDK 10
- Доступный Redis (`localhost:6379`)
- Запущенный `IdentityProfileService` на `http://localhost:5000`
- Файл `Gateway/Configs/public.pem`

Из корня `backend_C_sharp`:

```powershell
dotnet restore .\Gateway\Gateway.csproj
dotnet run --project .\Gateway\Gateway.csproj
```

После запуска:

- API: `http://localhost:5005`
- Swagger: `http://localhost:5005/swagger`

## Запуск через Docker Compose

Сервис интегрирован в `docker-compose.dev.yaml` под именем `gateway`.

Из корня `backend_C_sharp`:

```powershell
docker compose -f .\docker-compose.dev.yaml build gateway
docker compose -f .\docker-compose.dev.yaml up -d gateway
```

По текущему `docker-compose.dev.yaml` наружу опубликован порт `8080`:

- API в Docker: `http://localhost:8080`
- Swagger в Docker: `http://localhost:8080/swagger`

## Ключевые настройки

- `ConnectionStrings__Redis`
- `GrpcEndpoints__IdentityProfileService`
- `Jwt__Issuer`
- `Jwt__Audience`
- `ASPNETCORE_ENVIRONMENT`

Путь к публичному ключу внутри контейнера: `/app/Configs/public.pem`.

## Диагностика

- Ошибка старта про ключ: проверьте mount `Gateway/Configs -> /app/Configs`.
- Ошибка запросов профиля: проверьте доступность `identity-profile` в compose-сети.
- Ошибка валидации токена: проверьте соответствие `iss/aud` значениям `Jwt__Issuer/Jwt__Audience`.

