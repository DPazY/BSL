# BSL (Book & Serials Library) — Highload Caching Optimization Engine

[![CI/CD Pipeline](https://github.com/DPazY/BSL/actions/workflows/ci.yml/badge.svg)](https://github.com/DPazY/BSL/actions/workflows/ci.yml)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet)
![C#](https://img.shields.io/badge/C%23-13.0-239120?style=flat-square&logo=c-sharp)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-336791?style=flat-square&logo=postgresql&logoColor=white)
![k6](https://img.shields.io/badge/k6-Load_Testing-7D64FF?style=flat-square&logo=k6&logoColor=white)
![License](https://img.shields.io/badge/License-MIT-blue.svg?style=flat-square)

## About
**BSL** is a high-performance, domain-driven backend service designed for processing publications (Books, Newspapers, Patents).

**Primary Purpose:** Beyond basic catalog operations, this project serves as an engineering and research implementation for the academic thesis: *"Optimization of the performance of high-load systems using mathematical models of resource management (on the example of the caching problem in .NET)"*.

It resolves the **Thundering Herd** problem and database pool exhaustion under non-stationary, self-similar network traffic (Heavy-Tailed distributions) by shifting cache management from classical heuristics (LRU/LFU) to a predictive mathematical model.

---

## Benchmark Results (k6 Load Testing)

Comparison between classical **Reactive LRU** and the developed **Proactive Caching Engine (BSL)** under synthetic self-similar traffic and constrained memory ($W = 100$ items):

| Metric | Reactive LRU | Proactive Caching (BSL) | Impact |
| :--- | :---: | :---: | :---: |
| **Peak Hit Ratio** | 3–5% *(cache thrashing)* | **73–78%** | **~14x improvement** |
| **Latency p95 (Hits)** | ~11,800 ms *(DB queue congestion)* | **< 40 ms** | **~295x lower latency** |
| **Latency p95 (Misses)** | 10,000+ ms *(timeouts)* | **500–1,000 ms** | **10–12x reduction** |
| **Throughput (RPS Hits)** | 13.3 req/s | **16.3 req/s** | **+22.5%** |

---

## Research & Mathematical Model

The core innovation of the project is the **Hybrid Cost-Aware Predictive Caching Algorithm**.

### 1. IFS-Predictor (Proactive Prefetching)
To anticipate high-load spikes before they occur, the system employs an approach inspired by **Iterated Function Systems (IFS)** and local affine transformations:
* **Mechanism:** By scanning recent access intensity history ($k = 20$), the predictor searches for the closest geometric analog in historical data using 1D affine mapping $W(d) = s \cdot d + o$ minimized via Mean Squared Error (MSE).
* **Burst Adaptation:** The contractivity condition $|s| < 1$ is relaxed to $|s| \le 1.5$ to allow trend extrapolation during emerging traffic surges.
* **Outcome:** Background worker (`PeriodicTimer`) proactively fetches publications from PostgreSQL into memory *before* customer queries arrive, eliminating cold misses.

### 2. Fractional Knapsack & Eviction Policy
When memory capacity is exceeded, traditional algorithms drop data based only on recency. BSL models eviction as a **Continuous Knapsack Problem**:
* **Mechanism:** Every cached object is assigned a dynamic utility index:
  $$\rho_i = \frac{\lambda_i \cdot t_i}{w_i}$$
  where $\lambda_i$ is access frequency, $t_i$ is DB extraction cost (penalty), and $w_i$ is physical memory footprint.
* **Lazy Re-evaluation:** To avoid $O(N)$ priority queue re-sorting overhead, candidates from the `PriorityQueue` (Min-Heap) are evaluated lazily on eviction. If an item's priority increased due to a newly predicted burst, eviction is canceled (`MaxSpins = 50` livelock protection), ensuring $O(\log N)$ eviction complexity.

---

## Concurrency & High-Performance Architecture

* **Lock-Free Hit Tracking:** Telemetry updates and exponential decay calculations are completely thread-safe and lock-free, implemented via CAS loops (`Interlocked.CompareExchange`) over `ConcurrentDictionary`.
* **Zero-Allocation Ready:** Critical mathematical paths minimize heap allocations to prevent Gen0 GC pauses under high concurrency.
* **Decorator Pipeline:** Architecture follows the Decorator pattern over Dapper repositories, keeping business logic clean and decoupled from caching mechanics.

mermaid
flowchart TD
Client[HTTP Client / k6 Traffic Generator] --> App[ASP.NET Core Minimal API]

    subgraph Data Access Layer (Decorator Chain)
        App --> MetricDec[MetricsDecorator (Telemetry Tracking)]
        MetricDec --> CacheRepo[PACachedRepository (Lock-Free In-Memory)]
        CacheRepo -->|Cache Hit| MemoryCache[(ConcurrentDictionary)]
        CacheRepo -->|Cache Miss| PgRepo[PostgresRepository (Dapper)]
    end
    
    subgraph Background Services
        Prefetcher[IfsBackgroundPrefetcher] -.->|Predictive Prefetch| PgRepo
        Prefetcher -.->|Pre-warm Cache| MemoryCache
    end
    
    PgRepo --> Database[(PostgreSQL Database)]

---

## Architecture & Design Patterns

* **Decorator Pattern:** `RepositoryDecorator` wraps the persistence repositories (`PostgresRepository`, `FileRepository`), enabling transparent cache injection and execution time telemetry (`bsl.repository.method.duration`) without touching domain logic.
* **Strategy Pattern:** Interchangeable serialization engines (`JsonSerializerStrategy`, `XmlSerializerStrategy`, `ProtobufSerializerStrategy`) switchable depending on payload constraints.
* **Repository Pattern:** Complete abstraction over persistence layers for seamless swapping between PostgreSQL, file storage, and mock test providers.

---

## Tech Stack
* **Language & Runtime:** C# 13, .NET 10
* **Database & Access:** PostgreSQL, Dapper (Micro-ORM)
* **Testing & Quality:** xUnit, Moq, GitHub Actions CI
* **Observability:** OpenTelemetry, AppMetrics
* **Load Simulation:** k6 (JavaScript)

---

## Getting Started

### Prerequisites
* .NET SDK 10.0+
* Docker & Docker Compose (or local PostgreSQL)

### Installation & Run

1. Clone the repository:
bash
git clone https://github.com/DPazY/BSL.git
cd BSL


2. Start database and service via Docker Compose:
bash
docker compose up --build -d

Swagger UI will be available at: `http://localhost:5000/swagger`

3. Or build and run locally:
bash
dotnet restore
dotnet build -c Release
dotnet test -c Release --verbosity normal
dotnet run --project BSL.App


---

## Load Simulation (k6)

To execute the self-similar fractal load scenario and observe the Thundering Herd suppression:

bash
k6 run BSL.Test/loadtest.js