# GetFast

Учебный веб-API городской курьерской службы. Назначение, роли и сценарии — в [паспорте проекта](PROJECT.md), ссылки на требования, угрозы и решения — там же.

## Текущее состояние

Реализована минимальная техническая основа для EK1:

- ASP.NET Core Web API на .NET 10;
- общий `GetFastDbContext` и подключение к PostgreSQL через EF Core;
- `GET /health`: `200` и `{"status":"healthy"}` при доступной базе, `503` и `{"status":"unhealthy"}` при отказе подключения;
- Swagger UI и OpenAPI в окружении `Development`;
- контейнерный запуск и интеграционные тесты с настоящим PostgreSQL через Testcontainers.

Учётные записи, роли, отправления, миграции предметной модели и механизмы D-01–D-03 ещё не реализованы. Контекст базы пока не содержит предметных сущностей. Проверки каркаса не подтверждают выполнение требований безопасности SR-01–SR-10.

## Запуск через Docker Compose

Нужен Docker Desktop с Linux-контейнерами и Docker Compose. Отдельный SDK для этого способа запуска не нужен: сборка выполняется в Docker.

В корне репозитория создайте локальный `.env`, если его ещё нет:

```powershell
if (-not (Test-Path .env)) { Copy-Item .env.example .env }
```

Замените `CHANGE_ME` в `POSTGRES_PASSWORD` случайным локальным паролем. Для строки подключения удобно использовать пароль из букв и цифр. Не добавляйте `.env` в Git. Имена базы и пользователя в примере относятся только к учебному локальному окружению.

```powershell
docker compose up --build
```

Compose ждёт готовности PostgreSQL, затем запускает API. По умолчанию API доступен на `http://localhost:8080`, PostgreSQL — на `localhost:5432`; оба порта опубликованы только на локальном интерфейсе. Если порт занят, измените `API_PORT` или `POSTGRES_PORT` в `.env`.

В другом терминале:

```powershell
Invoke-RestMethod http://localhost:8080/health
```

Ожидается `status: healthy` и HTTP `200`. В `Development` Swagger UI открывается по адресу [http://localhost:8080/swagger](http://localhost:8080/swagger). В `Production` Swagger отсутствует.

Остановка:

```powershell
docker compose down
```

Данные PostgreSQL сохраняются в Docker volume.

## Запуск API через .NET SDK

Нужен .NET 10 SDK. Файл `global.json` допускает стабильные версии SDK 10.0 с актуальным feature band.

Сначала подготовьте `.env` как выше и запустите только базу:

```powershell
docker compose up -d postgres
dotnet restore GetFast.sln
dotnet tool restore
```

Следующие команды PowerShell читают значения из локального `.env` и передают строку подключения процессу API:

```powershell
$getFastEnv = ConvertFrom-StringData -StringData ((Get-Content .env | Where-Object { $_ -notmatch '^\s*(#|$)' }) -join "`n")
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=$($getFastEnv.POSTGRES_PORT);Database=$($getFastEnv.POSTGRES_DB);Username=$($getFastEnv.POSTGRES_USER);Password=$($getFastEnv.POSTGRES_PASSWORD)"
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --project src/GetFast.Api --urls "http://localhost:$($getFastEnv.API_PORT)"
```

Если API из Compose уже запущен, остановите его командой `docker compose stop api`, чтобы освободить порт. Сам `dotnet run` не загружает `.env` автоматически. Без `ConnectionStrings__DefaultConnection` приложение останавливается с сообщением об отсутствующей конфигурации.

Для последующих миграций в репозитории закреплён локальный инструмент `dotnet-ef`. Проверка после `dotnet tool restore`:

```powershell
dotnet ef --version
```

## Проверки

Нужны .NET 10 SDK и запущенный Docker Desktop. Поднимать Compose и создавать `.env` для тестов не требуется: Testcontainers сам создаёт изолированные базы со случайными паролями и удаляет тестовые контейнеры после прогона.

```powershell
dotnet build GetFast.sln --configuration Release --warnaserror
dotnet test GetFast.sln --configuration Release
```

Проверяются доступная и недоступная база, отсутствие секретов в отрицательном ответе `/health`, Swagger в `Development` и `Production`, отказ запуска без строки подключения. Если Docker недоступен, интеграционные тесты завершаются ошибкой, а не пропускаются.

Фактические проверки защитных механизмов будут добавляться вместе с реализацией связанных SR и D. Свидетельства должны относиться к конкретной версии продукта.
