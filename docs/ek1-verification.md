# Проверка запускаемой основы EK1

Результаты ниже относятся к указанным версиям исходного кода. Исторический прогон не подтверждает автоматически работу более позднего main.

## Проверка основы main от 05.10.2026

Исходная версия: `e6e9ee5a1117667892308fccfbc01659aec49b68`, уже включающая PR №2 с частью D-02. Окружение: Windows, .NET SDK 10.0.401. Изменения текущего PR затрагивают документы и презентацию; исходный код, тесты и конфигурация запуска совпадают с этой версией.

| Фактический запуск | Результат |
| --- | --- |
| `dotnet build GetFast.sln -c Release -warnaserror` | Успех, 0 ошибок, 0 предупреждений |
| `dotnet test GetFast.sln -c Release --no-build --filter "FullyQualifiedName~ConfigurationTests"` с TRX-отчётом | 6 пройдено, 0 не пройдено, 0 пропущено; эти проверки не требуют работающей базы |
| `dotnet test GetFast.sln -c Release --no-build --list-tests` | Обнаружены 52 тестовых случая; это перечень, а не результат их выполнения |
| `dotnet tool restore` | Локальный `dotnet-ef` 10.0.12 восстановлен |
| `dotnet ef migrations has-pending-model-changes --project src/GetFast.Api --configuration Release --no-build` | Модель совпадает с последней миграцией; для команды задана синтетическая `ConnectionStrings__DefaultConnection`, подключения к базе не требуется |
| `docker info` | Docker Engine недоступен: отсутствует канал `dockerDesktopLinuxEngine`; Docker Desktop завершает запуск с ошибкой служебного сокета `dockerInference` |

Полный прогон тестов с PostgreSQL и контейнерное демо в этой проверке не выполнены из-за сбоя локального Docker Desktop. Проверка модели не подтверждает применение миграции к базе, а шесть тестов конфигурации — работу входа и ролей. После восстановления Docker нужно выполнить полный прогон и демо по [README](../README.md) на версии, выбранной для сдачи.

## Исторический протокол от 04.10.2026

### Проверенная область

Проверена версия каркаса `af43c8c0701808edf897b8919842f9b3e0892249`, до включения PR №2. Код, зависимости и контейнерная конфигурация этого исторического прогона относятся к указанному SHA. Параметры локального `.env` не входят в Git.

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

### Что результат не подтверждает

Сценарии доставки, предметные миграции, вход/роли и D-01–D-03 отсутствовали в проверенном каркасе `af43c8c`. Нельзя выдавать эти семь тестов за выполнение SR-01–SR-10. Вход и роли из PR №2 не входят в этот исторический прогон; в текущем main они уже реализованы. Проверки D-03 в design-decisions.md остаются планом.

### Повторение команд

Подготовить `.env` по README, запустить Docker Desktop. Исторический прогон использовал `API_PORT=18080` и `POSTGRES_PORT=55432` в `.env`. При повторении на другой версии результаты нужно записать с её SHA, отдельно от приведённых выше:

```bash
docker compose -p getfast-georgii-ek1 up --build -d
curl --fail --show-error http://localhost:18080/health
dotnet build GetFast.sln --configuration Release --warnaserror
dotnet test GetFast.sln --configuration Release --no-build
```

Для другого компьютера нужен .NET 10 SDK либо контейнерный запуск без отдельного SDK. Локальные пути, пароли и файлы результатов не входят в репозиторий.
