# Проверка запускаемой основы EK1

Дата: 04.10.2026. Исполнитель автоматических проверок: Codex в локальном окружении M3. Это фактические запуски инструментов агентом, а не декларация проверки человеком. Личная проверка и объяснение участника фиксируются отдельно в AI_USAGE.

## Проверенная область

Исходный код каркаса из main на commit `bfbadec6e6d7588e0e58f7a24a2656b4ebfa9e3e`. В данной ветке меняются только документы. Проверенные код, зависимости и контейнерная конфигурация совпадают с указанной исходной версией. Параметры локального `.env` не входят в Git.

Окружение: macOS arm64, .NET SDK 10.0.401, Docker Desktop (сервер 28.5.2), PostgreSQL 17-alpine. Из-за занятых стандартных портов использованы API 18080 и PostgreSQL 55432, имя Compose-проекта `getfast-georgii-ek1`.

| Фактический запуск | Результат |
| --- | --- |
| `dotnet build GetFast.sln --configuration Release --warnaserror` | Успех, 0 ошибок, 0 предупреждений |
| `dotnet test GetFast.sln --configuration Release --no-build` с TRX-отчётом | 7 пройдено, 0 не пройдено, 0 пропущено; настоящий PostgreSQL через Testcontainers |
| `docker compose -p getfast-georgii-ek1 up --build -d` | Контейнеры API и PostgreSQL запущены, база healthy |
| `GET http://localhost:18080/health` | HTTP 200, `{"status":"healthy"}` |
| `GET http://localhost:18080/swagger/index.html` | HTTP 200 |
| `id` внутри контейнера API | UID 1654, пользователь app |

Семь тестов: доступная база; отклонённое подключение к базе и отсутствие секретов в ответе; отказ запуска без строки подключения; Swagger UI и OpenAPI в Development и их отсутствие в Production (четыре случая).

## Что результат не подтверждает

Сценарии доставки, предметные миграции, вход/роли и D-01–D-03 отсутствуют в проверенном каркасе. Нельзя выдавать эти семь тестов за выполнение SR-01–SR-10. Вход и роли подготовлены в другом PR, который не включён в проверенную версию. Проверки D-03 в design-decisions.md остаются планом.

## Повторение команд

Подготовить `.env` по README, запустить Docker Desktop. Для данного локального окружения:

```bash
docker compose -p getfast-georgii-ek1 up --build -d
curl --fail --show-error http://localhost:18080/health
dotnet build GetFast.sln --configuration Release --warnaserror
dotnet test GetFast.sln --configuration Release --no-build
```

Для другого компьютера нужен .NET 10 SDK либо контейнерный запуск без отдельного SDK. Локальные пути, пароли и файлы результатов не входят в репозиторий.
