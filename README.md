# BSL (Book & Serials Library)

[![CI/CD Pipeline](https://github.com/DPazY/BSL/actions/workflows/ci.yml/badge.svg)](https://github.com/DPazY/BSL/actions/workflows/ci.yml)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-336791?style=flat-square&logo=postgresql&logoColor=white)
![License](https://img.shields.io/badge/License-MIT-blue.svg?style=flat-square)

Учебный pet-проект: REST-сервис каталога изданий (книги, газеты, патенты) на ASP.NET Core Minimal APIs с in-memory кэширующим слоем поверх PostgreSQL/Dapper и фоновым конвейером импорта файлов.

Это не продакшн-система: кэш локален для одного экземпляра сервиса, аутентификация — простой API-ключ, конфигурация рассчитана на запуск в Docker Compose на localhost.

---

## Что реализовано

### 1. Кэширующий декоратор репозитория (`IfsCachedRepository`)

* Паттерн **Decorator** поверх `IRepository<T>` (Dapper + PostgreSQL): HTTP-слой ничего не знает о кэше.
* При промахе данные читаются из БД, размер объекта оценивается через `ObjectSizeApproximator`, и при превышении лимита памяти выполняется вытеснение.
* **Cost-aware вытеснение:** каждый элемент получает вес по экспоненциально затухающей интенсивности обращений (EWMA, период полураспада 60 с); очередь вытеснения — `PriorityQueue`, переоценка веса — ленивая (при депайке). Это эвристика «похожая на LFU/LRU-K», а не точное решение задачи оптимизации.
* Атомарные счётчики (`Interlocked`) для учёта занятой памяти и обновления весов; блокировка только на самой очереди вытеснения.
* Сквозные заголовки телеметрии у HTTP-ответов: `X-Data-Source` (`IFS-Cache` / `PostgreSQL`) и `X-Execution-Time-Ms`.
* Есть также базовый `LruCachedRepository` — он используется как контрольная точка в бенчмарке ниже.
* Ограничение моделирования: единый `SemaphoreSlim(1,1)` в `CachedRepository` сериализует обращения при промахе, но полной защиты от thundering herd (single-flight на ключ) сейчас нет.

### 2. Фоновый конвейер загрузки (`FileWatcher` + `BackgroundService`)

* `FileSystemWatcher` следит за смонтированной директорией внутри контейнера, плюс REST-эндпоинт `POST /api/v1/ingestion/upload` принимает файлы (`multipart/form-data`).
* Пакеты попадают в потокобезопасную очередь `FileProcessingQueue` и импортируются в PostgreSQL асинхронно, вне пула потоков веб-сервера.
* Сериализация через паттерн **Strategy**: `XmlSerializerStrategy`, `JsonSerializerStrategy`, `ProtobufSerializerStrategy` (CSV упоминается в summary эндпоинта, отдельной стратегии пока нет).
* Кастомные `SqlMapper.TypeHandler` для Dapper: `DateOnly`, `text[]`.
* `IfsBackgroundPrefetcher` — фоновый сервис прогрева кэша на `PeriodicTimer` с защитой от наложения тактов.

### 3. Панель управления (`bsl-ui`)

React + TypeScript + Vite + Tailwind, раздаётся через Nginx (reverse proxy на API):

* карточки изданий и источник каждого ответа (`X-Data-Source`);
* сравнительный тест кэшей через `POST /api/v1/cache/benchmark`;
* загрузка файлов в конвейер.

### Бенчмарк IFS vs LRU

`POST /api/v1/cache/benchmark` прогоняет синтетический трафик (распределение Ципфа) против `IfsCachedRepository` и `LruCachedRepository` и возвращает `p50`, `p95`, `TotalMs`, `SpeedupFactor`. Это in-process замер на одном экземпляре, а не распределённый нагрузочный тест; абсолютные цифры зависят от машины.

Нагрузочный сценарий k6 (`BSL.Test/loadtest.js`) поднимает до 200 виртуальных пользователей с самоподобным трафиком — для проверки поведения кэша и GC под нагрузкой.

---

## Архитектура

```text
+-----------------------------------------------------------------------+
|                        Docker Compose Network                         |
|                                                                       |
|  +-----------------------+         /api/*          +---------------+  |
|  |     bsl-ui (:3080)    | ----------------------> | bsl-app (:5000|  |
|  |   Control Plane UI    | <---------------------- |  .NET 10 API  |  |
|  |   Nginx Reverse Proxy |   X-Data-Source Header  +-------+-------+  |
|  +-----------------------+                                 |          |
|                                                            |          |
|                 +--------------------------+---------------+          |
|                 |                          |                           |
|                 v                          v                           |
|  +-----------------------------+  +----------------------+            |
|  |  IfsCachedRepository        |  |  FileWatcher Service |            |
|  |  - EWMA weights (decay)     |  |  - watch directory   |            |
|  |  - PriorityQueue eviction   |  |  - Concurrent Queue  |            |
|  |  - Interlocked counters     |  |  - XML/JSON/Proto    |            |
|  +--------------+--------------+  +-----------+----------+            |
|                 |                             |                        |
|                 +--------------+--------------+                        |
|                                | Dapper Async SQL                     |
|                                v                                      |
|                     +---------------------+                           |
|                     | bsl-postgres (:5432)|                           |
|                     +---------------------+                           |
+-----------------------------------------------------------------------+
```

---

## Быстрый старт (Docker Compose)

Нужен только установленный Docker.

```bash
git clone https://github.com/DPazY/BSL.git
cd BSL
docker compose up -d --build
```

После запуска:

* **UI:** http://localhost:3080
* **Swagger UI:** http://localhost:5000/swagger
* **Health check:** http://localhost:5000/api/health
* **PostgreSQL:** `localhost:5432` (БД `bsl_db`)

> Пароль БД в `docker-compose.yml`/`init.sql` — плейсхолдер для локальной разработки. Для любого окружения вне localhost вынесите его в переменные окружения (`.env`), а `ApiKeyMiddleware` требует заголовок `X-API-Key`.

---

## Основные REST API эндпоинты

| Метод | Маршрут | Описание |
| :--- | :--- | :--- |
| `GET` | `/api/v1/catalog/books` | Пагинированный поиск книг: фильтр по автору, префиксу издательства, сортировка по году |
| `GET` | `/api/v1/catalog/books/{name}` | Точечное получение книги через кэш; заголовки `X-Data-Source`, `X-Execution-Time-Ms` |
| `GET` | `/api/v1/catalog/newspapers` | Список периодических изданий |
| `GET` | `/api/v1/catalog/patents` | Список патентов |
| `POST` | `/api/v1/cache/warmup` | Прогрев кэша по топ-ключам текущей телеметрии |
| `POST` | `/api/v1/cache/benchmark` | Сравнение `IFS vs LRU` на синтетическом трафике Ципфа (`p50`, `p95`, `TotalMs`, `SpeedupFactor`) |
| `POST` | `/api/v1/ingestion/upload` | Загрузка файла (XML/JSON/Protobuf) в конвейер обработки |
| `GET` | `/api/health` | Проверка веб-сервиса и доступности пула соединений PostgreSQL |

---

## Структура решения

* **`BSL.App`** — ASP.NET Core Minimal APIs (.NET 10): DI-контейнер, `ProblemDetails` (RFC 7807), фоновые службы, OpenAPI/Swagger.
* **`BSL.Implementation`** — доменная логика: репозитории Dapper + PostgreSQL, `IfsCachedRepository`, `LruCachedRepository`, `MetricsRepository`, стратегии сериализации, очередь файлов, валидаторы.
* **`BSL.Models`** — доменные модели (`Book`, `Newspaper`, `Patent`, `Edition`), DTO, контракты (`IRepository<T>`).
* **`BSL.Security`** — `ApiKeyMiddleware` (заголовок `X-API-Key`).
* **`BSL.Test`** — модульные и интеграционные тесты (NUnit, Moq): алгоритмы вытеснения, валидаторы, парсеры, работа с БД + `loadtest.js` для k6.
* **`bsl-ui`** — панель управления (React, TypeScript, Vite, Tailwind CSS, Nginx).

---

## Тесты и нагрузка

```bash
dotnet test --configuration Release
```

```bash
k6 run BSL.Test/loadtest.js
```

---

## Известные ограничения (что можно улучшить)

* Кэш in-memory и локален для экземпляра — при горизонтальном масштабировании нужны шардинг или внешний кэш (Redis); нет инвалидации при записи через конвейер импорта.
* Нет single-flight защиты от thundering herd на уровне ключа.
* `ObjectSizeApproximator` оценивает размер объекта приблизительно — точный учёт памяти в CLR невозможен без нативных хуков.
* CSV-парсер заявлен в описании эндпоинта, но стратегия не реализована.
* API-ключ хранится в конфигурации проекта; ротации и хранения в secrets-менеджере нет.

---

## Контакты

* **Автор:** Данил Пазюрко
* **GitHub:** [https://github.com/DPazY](https://github.com/DPazY)
* **Telegram:** [@dpazyurko](https://t.me/dpazyurko)
