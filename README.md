# BSL (Book & Serials Library) — Highload Caching Optimization Engine

[![CI/CD Pipeline](https://github.com/DPazY/BSL/actions/workflows/ci.yml/badge.svg)](https://github.com/DPazY/BSL/actions/workflows/ci.yml)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet)
![C#](https://img.shields.io/badge/C%23-13.0-239120?style=flat-square&logo=c-sharp)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-336791?style=flat-square&logo=postgresql&logoColor=white)
![k6](https://img.shields.io/badge/k6-Load_Testing-7D64FF?style=flat-square&logo=k6&logoColor=white)
![License](https://img.shields.io/badge/License-MIT-blue.svg?style=flat-square)

## About
# BSL — Высокопроизводительный сервис каталога изданий с предиктивным IFS-кэшированием

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![PostgreSQL 16](https://img.shields.io/badge/PostgreSQL-16-4169E1?logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![Docker Compose](https://img.shields.io/badge/Docker-Compose-2496ED?logo=docker&logoColor=white)](https://docs.docker.com/compose/)
[![License: MIT](https://img.shields.io/badge/License-MIT-emerald.svg)](https://opensource.org/licenses/MIT)

**BSL (Book Storage Library)** — распределённый веб-сервис управления каталогом печатных и электронных изданий (книги, газеты, патенты), оснащённый **предиктивным кэшем на базе систем итерируемых функций (IFS)** с оптимизацией вытеснения по условиям Каруша — Куна — Таккера (ККТ) и реактивным конвейером потоковой загрузки данных.

Проект разработан на стыке теории динамических систем и системного программирования: система предотвращает лавинообразные промахи кэша (*Thundering Herd*) и минимизирует хвостовую задержку ($p_{95}$) при самоподобных нагрузках с тяжёлыми хвостами.

---

## Ключевые инженерные и архитектурные решения

### 1. Предиктивное кэширование (`IfsCachedRepository` + ККТ) и Lock-Free синхронизация
* **Математическая модель трафика:** вместо реактивного вытеснения по давности использования (LRU), система моделирует поток обращений как самоподобный процесс (распределения Ципфа и Парето) и вычисляет вероятностный аттрактор методом систем итерируемых функций (IFS).
* **Оптимизация вытеснения через ККТ:** при заполнении кэша задача отбора ключей для сохранения и вытеснения решается через лагранжиан и условия Каруша — Куна — Таккера (ККТ), что снижает сквозную хвостовую задержку $p_{95}$ в **12 раз** (до $40\text{ мс}$ при $200\text{ VU}$ в тестах `k6`).
* **Неблокирующий пересчёт весов (Lock-Free / CAS):** обновление весов популярности и экспоненциального затухания выполняется без блокировок потоков с помощью `Interlocked.CompareExchange`, исключая простой рабочего пула при пиковых нагрузках.
* **Прозрачное декорирование DAL:** кэширующий слой и сборщик метрик реализованы через паттерн **Decorator** поверх репозиториев Dapper (`IRepository<T>`) и снабжают HTTP-ответы сквозными заголовками телеметрии `X-Data-Source` (`IFS-Cache` / `PostgreSQL`) и `X-Execution-Time-Ms`.

### 2. Реактивный конвейер потоковой загрузки (`FileWatcher` + `BackgroundService`)
* **Асинхронная обработка пакетов:** фоновый сервис отслеживает директорию `/app/watch` с помощью `FileSystemWatcher`, а также принимает файлы через `multipart/form-data` REST-эндпоинт `/api/v1/ingestion/upload`.
* **Потокобезопасная очередь и PeriodicTimer:** поступившие пакеты ставятся в неблокирующую очередь `FileProcessingQueue` и асинхронно импортируются в PostgreSQL без блокировки пула потоков веб-сервера. Упреждающий прогрев кэша (`IfsBackgroundPrefetcher`) работает на базе асинхронного `PeriodicTimer` с защитой от наложения тактов (*overlapping*).
* **Взаимозаменяемая сериализация (Strategy):** нормализация и парсинг разнородных потоков данных (`Protobuf`, `JSON`, `XML`, `CSV`) изолированы через паттерн **Strategy** с последующей пакетной записью в СУБД и кастомными `SqlMapper.TypeHandler` в Dapper (`DateOnly`, массивы `text[]`).

### 3. Интерактивная панель управления (`BSL Studio Control Plane`)
* Веб-интерфейс для мониторинга и профилирования в реальном времени: позволяет инспектировать карточки изданий, отслеживать источник каждого ответа (`IFS-Cache` или `PostgreSQL`), запускать сравнительный стресс-тест **IFS vs LRU** на синтетическом трафике Ципфа с визуализацией перцентилей $p_{50}$ / $p_{95}$ и отправлять пакеты данных в конвейер `FileWatcher`.

---

## Архитектура системы

```text
+-----------------------------------------------------------------------+
|                        Docker Compose Network                         |
|                                                                       |
|  +-----------------------+         /api/*          +---------------+  |
|  |     bsl-ui (:3000)    | ----------------------> | bsl-app (:8080|  |
|  |   Control Plane UI    | <---------------------- |  .NET 10 API  |  |
|  |   Nginx Reverse Proxy |   X-Data-Source Header  +-------+-------+  |
|  +-----------------------+                                 |          |
|                                                            |          |
|         +--------------------------------------------------+          |
|         |                                                  |          |
|         v                                                  v          |
|  +-----------------------------+            +----------------------+  |
|  |  IfsCachedRepository<T>     |            |  FileWatcher Service |  |
|  |  - IFS Attractor Warmup     |            |  - /app/watch Volume |  |
|  |  - KKT Eviction Optimizer   |            |  - Concurrent Queue  |  |
|  |  - Lock-Free CAS Weights    |            |  - XML/CSV/JSON/Proto|  |
|  +--------------+--------------+            +-----------+----------+  |
|                 |                                       |             |
|                 +-------------------+-------------------+             |
|                                     | Dapper Async SQL                |
|                                     v                                 |
|                          +---------------------+                      |
|                          | bsl-postgres (:5432)|                      |
|                          |  PostgreSQL 16 DB   |                      |
|                          +---------------------+                      |
+-----------------------------------------------------------------------+
```

---

## Математическая модель IFS-кэша

Для каждого ключа $i \in \{1, \dots, N\}$ при поступлении обращения $r_k$ выполняется сжимающее аффинное отображение весового коэффициента популярности:

$$
w_i^{(k+1)} = \gamma \cdot w_i^{(k)} + (1 - \gamma) \cdot \mathbb{I}(r_k = i), \quad \gamma \in (0, 1)
$$

При необходимости вытеснения элементов из кэша ограниченной ёмкости $C$ решается задача максимизации суммарной полезности с ограничениями в форме условий Каруша — Куна — Таккера (ККТ):

$$
\max_{x \in [0, 1]^N} \sum_{i=1}^{N} u_i(w_i, s_i) x_i \quad \text{при условии} \quad \sum_{i=1}^{N} s_i x_i \le C
$$

где $u_i$ — оценка вероятности повторного обращения на неподвижной точке аттрактора IFS, а $s_i$ — стоимость хранения объекта в памяти.

---

## Быстрый старт (Docker Compose)

Для запуска всей инфраструктуры (PostgreSQL 16 с автоинициализацией схемы, сервис .NET 10 API и панель управления) требуется только установленный **Docker Desktop**.

1. Склонируйте репозиторий и перейдите в корневую директорию:

```bash
git clone https://github.com/DPazY/BSL.git
cd BSL
```

2. Соберите и запустите контейнеры одной командой:

```bash
docker compose up -d --build
```

3. Откройте сервисы в браузере:
   * **BSL Studio Control Plane:** [http://localhost:3080](http://localhost:3080)
   * **Swagger UI (OpenAPI документация):** [http://localhost:5000/swagger](http://localhost:5000/swagger)
   * **Health Check эндпоинт:** [http://localhost:5000/api/health](http://localhost:5000/api/health)
   * **PostgreSQL 16:** `localhost:5432` (БД: `bsl_db`, пользователь: `postgres`, пароль: `postgres_password`)

---

## Основные REST API эндпоинты

| Метод | Маршрут | Описание |
| :--- | :--- | :--- |
| `GET` | `/api/v1/catalog/books` | Пагинированный поиск книг с фильтрацией по автору, префиксу издательства и сортировкой по году (`Asc` / `Desc`) |
| `GET` | `/api/v1/catalog/books/{name}` | Точечное получение книги через `IfsCachedRepository` с заголовками телеметрии `X-Data-Source` и `X-Execution-Time-Ms` |
| `GET` | `/api/v1/catalog/newspapers` | Получение списка периодических изданий (газет) из каталога |
| `GET` | `/api/v1/catalog/patents` | Получение списка патентов из каталога |
| `POST` | `/api/v1/cache/warmup` | Упреждающий прогрев кэша путём расчёта неподвижной точки аттрактора IFS |
| `POST` | `/api/v1/cache/benchmark` | Сравнительный стресс-тест `IFS vs LRU` на синтетическом трафике Ципфа (возвращает $p_{50}$, $p_{95}$, `TotalMs` и `SpeedupFactor`) |
| `POST` | `/api/v1/ingestion/upload` | Потоковая загрузка `XML` / `CSV` пакетов напрямую в директорию реактивного конвейера `FileWatcher` |
| `GET` | `/api/health` | Проверка работоспособности веб-сервиса и доступности пула соединений PostgreSQL |

---

## Структура решения

* **`BSL.App`** — серверное приложение на ASP.NET Core Minimal APIs (.NET 10), конфигурация DI-контейнера, глобальная обработка исключений в стандарте RFC 7807 (`ProblemDetails`), фоновые службы `BackgroundService` и OpenAPI/Swagger.
* **`BSL.Implementation`** — доменная логика, асинхронные репозитории Dapper + PostgreSQL, кастомные `SqlMapper.TypeHandler`, реализация `IfsCachedRepository`, `LruCache`, `MetricsRepository`, стратегии сериализации и очереди обработки файлов.
* **`BSL.Models`** — доменные модели (`Book`, `Newspaper`, `Patent`, `Edition`), DTO и контракты уровня доступа к данным (`IRepository<T>`).
* **`BSL.Security`** — middleware аутентификации (`ApiKeyMiddleware`) и компоненты разграничения доступа.
* **`BSL.Test`** — 69 модульных и интеграционных тестов (NUnit, Moq), проверяющих корректность алгоритмов вытеснения, валидаторов, парсеров и работы с БД.
* **`bsl-ui`** — интерактивная панель управления и профилирования кэша (React, TypeScript, Vite, Tailwind CSS, Nginx).

---

## Запуск тестов и нагрузочного профилирования

Запуск модульных и интеграционных тестов .NET:

```bash
dotnet test --configuration Release
```

Запуск нагрузочного сценария `k6` (самоподобный трафик Парето/Ципфа до 200 виртуальных пользователей):

```bash
k6 run BSL.Test/loadtest.js
```

---

## Контакты

* **Автор:** Данил Пазюрко
* **GitHub:** [https://github.com/DPazY](https://github.com/DPazY)
* **Telegram:** [@dpazyurko](https://t.me/dpazyurko)
