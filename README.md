# GetFast

Учебный веб-API городской курьерской службы. Назначение, роли и сценарии — в [паспорте проекта](PROJECT.md), ссылки на требования, угрозы и решения — там же.

## Текущее состояние

Реализована минимальная техническая основа для EK1:

- ASP.NET Core Web API на .NET 10;
- общий `GetFastDbContext` и подключение к PostgreSQL через EF Core;
- `GET /health`: `200` и `{"status":"healthy"}` при доступной базе, `503` и `{"status":"unhealthy"}` при отказе подключения;
- Swagger UI и OpenAPI в окружении `Development`;
- контейнерный запуск и интеграционные тесты с настоящим PostgreSQL через Testcontainers.

Реализована часть D-02: Identity и роли `Sender`, `Courier`, `Dispatcher`, регистрация отправителя, вход с JWT на 30 минут, политики ролей и запрет анонимного доступа по умолчанию. `GET /courier/tasks` и `GET /dispatch/shipments` пока возвращают пустые массивы для проверки полномочий. Группа `/shipments` подготовлена для методов M2.

Модель отправлений и D-01/D-03 ещё планируются. Проверки ролей не подтверждают проверку владельца отправления, назначенного курьера или полный критерий SR-03.

## Запуск через Docker Compose

Нужен Docker Desktop с Linux-контейнерами и Docker Compose. Отдельный SDK для этого способа запуска не нужен: сборка выполняется в Docker.

В корне репозитория создайте локальный `.env`, если его ещё нет:

```powershell
if (-not (Test-Path .env)) { Copy-Item .env.example .env }
```

Замените все `CHANGE_ME` в `.env` собственными случайными значениями. `JWT_SIGNING_KEY` должен содержать не менее 32 байт UTF-8; удобно использовать 64 шестнадцатеричных символа, полученных из 32 случайных байт. Для пароля PostgreSQL используйте буквы и цифры. Пароли сотрудников должны удовлетворять стандартным правилам Identity: от 6 символов, заглавная и строчная буквы, цифра и специальный символ. Не добавляйте `.env` в Git. Адреса `@example.invalid` — синтетические локальные учётные записи.

```powershell
docker compose up --build
```

Compose ждёт готовности PostgreSQL, затем запускает API. В Development API применяет миграции Identity и создаёт курьера и диспетчера из `.env`. Повторный запуск сохраняет учётные записи; конфликт email с другой ролью или несовпадение пароля останавливает запуск, не повышает права и не сбрасывает пароль.

По умолчанию API доступен на `http://localhost:8080`, PostgreSQL — на `localhost:5432`; оба порта опубликованы только на локальном интерфейсе. Если порт занят, измените `API_PORT` или `POSTGRES_PORT` в `.env`.

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

## macOS и Linux: контейнерный запуск

Откройте Docker Desktop. В корне репозитория:

```bash
[ -f .env ] || cp .env.example .env
```

Замените `CHANGE_ME` в локальном `.env` случайным паролем из букв и цифр. Если порты заняты, задайте, например, `API_PORT=18080` и `POSTGRES_PORT=55432`. Затем:

```bash
docker compose up --build -d
curl --fail --show-error http://localhost:18080/health
```

В этом примере ожидается `{"status":"healthy"}`, Swagger — `http://localhost:18080/swagger`. При `API_PORT=8080` используйте порт 8080. Для остановки: `docker compose down`. Для тестов нужен .NET 10 SDK, команды из раздела «Проверки» одинаковы для PowerShell, bash и zsh.

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
$env:Jwt__SigningKey = $getFastEnv.JWT_SIGNING_KEY
$env:Seed__CourierEmail = $getFastEnv.SEED_COURIER_EMAIL
$env:Seed__CourierPassword = $getFastEnv.SEED_COURIER_PASSWORD
$env:Seed__DispatcherEmail = $getFastEnv.SEED_DISPATCHER_EMAIL
$env:Seed__DispatcherPassword = $getFastEnv.SEED_DISPATCHER_PASSWORD
dotnet run --project src/GetFast.Api --urls "http://localhost:$($getFastEnv.API_PORT)"
```

Если API из Compose уже запущен, остановите его командой `docker compose stop api`, чтобы освободить порт. Сам `dotnet run` не загружает `.env` автоматически. Без `ConnectionStrings__DefaultConnection` приложение останавливается с сообщением об отсутствующей конфигурации.

Для последующих миграций в репозитории закреплён локальный инструмент `dotnet-ef`. Проверка после `dotnet tool restore`:

```powershell
dotnet ef --version
```

В Production начальные сотрудники не создаются, а миграции применяются отдельно до запуска API. В терминале с уже заданной `ConnectionStrings__DefaultConnection`:

```powershell
dotnet ef database update --project src/GetFast.Api
```

Без `Jwt__SigningKey` или при длине менее 32 байт приложение не запускается. В Development также обязательны все четыре `Seed__*`. Команды EF Core используют строку подключения и не требуют JWT или начальных сотрудников.

## Регистрация, вход и роли

```powershell
$registration = @{ email = 'sender@example.invalid'; password = '<случайный локальный пароль>' } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri http://localhost:8080/auth/register -ContentType 'application/json' -Body $registration
$login = Invoke-RestMethod -Method Post -Uri http://localhost:8080/auth/login -ContentType 'application/json' -Body $registration
$headers = @{ Authorization = "Bearer $($login.accessToken)" }
Invoke-WebRequest -Uri http://localhost:8080/dispatch/shipments -Headers $headers
```

Регистрация назначает только `Sender`; JSON с `role` или другим неизвестным полем возвращает `400`. Последний запрос отправителя возвращает `403`. Для проверки разрешённого действия войдите с `SEED_DISPATCHER_EMAIL` и `SEED_DISPATCHER_PASSWORD`: его токен даёт `200` и `[]`. Курьер с собственным токеном получает `200` на `/courier/tasks`, но `403` на `/dispatch/shipments`. Без токена эти методы дают `401`.

В Swagger нажмите **Authorize** и вставьте `accessToken`; префикс Bearer добавляется интерфейсом. JWT содержит `sub` с GUID пользователя и `role` из базы. Сервер проверяет подпись HS256, issuer, audience и срок; дополнительного допуска после истечения нет. Роль, изменённая в базе, остаётся в уже выданном токене до его истечения. Проверка принадлежности конкретного отправления относится к D-01.

Анонимно доступны только явно отмеченные `/auth/register`, `/auth/login`, `/health` и Swagger в Development. Группы методов M2/M3 добавляются через `RoleEndpointGroups` в `MapRoleEndpointGroups`; новые `/track/*` нужно явно помечать анонимными. Тест перечня методов проверяет пометки и реальные требования ролей. FallbackPolicy требует входа, если политику забыли.

## Проверки

Нужны .NET 10 SDK и запущенный Docker Desktop. Поднимать Compose и создавать `.env` для тестов не требуется: Testcontainers сам создаёт изолированные базы со случайными паролями и удаляет тестовые контейнеры после прогона.

```powershell
dotnet build GetFast.sln --configuration Release --warnaserror
dotnet test GetFast.sln --configuration Release
```

Проверяются каркас, миграции Identity, регистрация с подстановкой роли и некорректными полями, вход и срок JWT, ключи конфигурации, начальные сотрудники, все пары роль–метод, неверные токены и перечень конечных точек. Swagger проверяется в Development и Production, включая OpenAPI Bearer. Если Docker недоступен, интеграционные тесты завершаются ошибкой, а не пропускаются.

Полный критерий SR-03 требует будущих методов назначений и действий курьера из D-01; текущие пустые ответы проверяют границы ролей. Фактические проверки защитных механизмов будут добавляться вместе с реализацией связанных SR и D. Свидетельства должны относиться к конкретной версии продукта.

## Подготовка EK1

- [Презентация EK1](docs/GetFast-EK1.pptx): шесть слайдов с заметками выступающих.
- [Выступление и вопросы](docs/ek1-defense.md): по 90 секунд для M1, M2 и M3, затем вопросы.
- [Фактические проверки каркаса](docs/ek1-verification.md): точная область проверок и ограничения.

Полная реализация D-01–D-03 к EK1 не требуется по [регламенту курса](https://github.com/hse-rbpo-bachelor-2026/course/blob/main/assessment/EK1.md). В версию для EK1 входят каркас и вход с ролями (часть D-02); D-01 и D-03 остаются планом.