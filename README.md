# BSL (Book & Serials Library) — Highload Caching Optimization PoC

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet)
![C#](https://img.shields.io/badge/C%23-12.0-239120?style=flat-square&logo=c-sharp)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-336791?style=flat-square&logo=postgresql&logoColor=white)
![k6](https://img.shields.io/badge/k6-Load_Testing-7D64FF?style=flat-square&logo=k6&logoColor=white)
![License](https://img.shields.io/badge/License-MIT-blue.svg?style=flat-square)

## About
**BSL** is a domain-driven service suite designed for managing publications (Books, Newspapers, Patents). 

**Primary Purpose:** Beyond its business logic, this project serves as a practical implementation for the academic thesis: *"Optimization of the performance of high-load systems using mathematical models of resource management (on the example of the caching problem in .NET)"*. 

It demonstrates a novel approach to resolving caching bottlenecks in Highload systems by applying advanced mathematical models to predict traffic bursts and mathematically justify cache eviction strategies.

## Research & Mathematical Model
The core innovation of this project is the **Hybrid Cost-Aware Caching Algorithm**, which fundamentally shifts cache management from standard heuristic approaches (like LRU/LFU) to a mathematically rigorous predictive model.

### 1. IFS-Predictor (Proactive Prefetching)
To anticipate high-load spikes before they occur, the system employs **Iterated Function Systems (IFS)** and fractal analysis. 
* **Mechanism:** By analyzing the self-similar nature of network traffic and database access patterns, the IFS-Predictor identifies emerging load bursts.
* **Outcome:** The system proactively fetches the required publication data from PostgreSQL into the cache *before* the actual user request hits the server, drastically reducing latency during peak loads.

### 2. KKT-Eviction (Reactive Eviction)
When memory limits are reached, standard eviction algorithms often discard data that is computationally expensive to retrieve. BSL solves this using the **Continuous Knapsack Problem (Fractional Knapsack)** combined with **Karush-Kuhn-Tucker (KKT) conditions**.
* **Mechanism:** Every cached object is assigned a *Specific Utility Index*. This index is dynamically calculated based on:
  * Access frequency.
  * Object size in memory.
  * **Miss Penalty:** The computational and I/O cost required to fetch the object from the database if it is evicted.
* **Outcome:** The cache mathematically guarantees the eviction of the "cheapest" data (in terms of DB retrieval cost), optimizing the overall system throughput.

## Key Features
* **Advanced Telemetry & Metrics:** Continuous collection of method execution durations (e.g., `bsl.repository.method.duration`) feeds directly into the mathematical model to adjust the "Miss Penalty" in real-time.
* **Background Processing:** Implementation of robust background workers (`FileWatcher`, `FileProcessingQueue`) for asynchronous file handling without blocking the main execution thread.
* **SOLID Architecture:** Strict adherence to SOLID principles ensures that the complex mathematical logic is decoupled from the business domain.

## Architecture & Patterns
The project heavily leverages GoF design patterns to seamlessly integrate the complex caching algorithms into the standard data flow:

* **Decorator Pattern:** The `RepositoryDecorator` wraps the standard `PostgresRepository` and `FileRepository`. This allows for transparent injection of the KKT-Eviction cache and telemetry tracking without modifying the underlying data access logic.
* **Strategy Pattern:** Used for dynamic serialization contexts (`JsonSerializerStrategy`, `XmlSerializerStrategy`, `ProtobufSerializerStrategy`), allowing the system to switch serialization formats on the fly based on payload size and performance requirements.
* **Repository Pattern:** Abstracts the underlying data storage, enabling easy swapping between SQL databases, file systems, and in-memory test mocks.

```csharp
// Example: Transparent caching and metrics via Decorator
public class CachedRepositoryDecorator<T> : IRepository<T> 
{
    private readonly IRepository<T> _innerRepository;
    private readonly IMetricsContext _metrics;
    private readonly IKktCache _cache;

    public CachedRepositoryDecorator(IRepository<T> inner, IMetricsContext metrics, IKktCache cache)
    {
        _innerRepository = inner;
        _metrics = metrics;
        _cache = cache;
    }

    public async Task<T> GetByIdAsync(Guid id)
    {
        if (_cache.TryGet(id, out var cachedItem)) 
            return cachedItem;
        
        var timer = System.Diagnostics.Stopwatch.StartNew();
        var item = await _innerRepository.GetByIdAsync(id);
        timer.Stop();
        
        _metrics.Record("bsl.repository.method.duration", timer.ElapsedMilliseconds);
        
        // The duration is used to calculate the 'Miss Penalty' for the KKT algorithm
        _cache.Set(id, item, timer.ElapsedMilliseconds); 
        
        return item;
    }
}
```

## Tech Stack
* **Language/Framework:** C#, .NET 8
* **Database:** PostgreSQL
* **Architecture:** Domain-Driven Design (DDD) concepts, Dependency Injection
* **Testing:** xUnit, Moq
* **Load Testing:** k6 (JavaScript)

## Getting Started

### Prerequisites
* .NET SDK 8.0+
* PostgreSQL server running locally or via Docker.

### Installation
1. Clone the repository:
```bash
git clone [https://github.com/yourusername/bsl.git](https://github.com/yourusername/bsl.git)
```
2. Configure the database connection string in `BSL.App/AppConfig.json`.
3. Apply database migrations (if applicable) or run the setup script.
4. Run the application:
```bash
dotnet build
dotnet run --project BSL.App
```

## Testing & Load Simulation
The project includes a comprehensive suite of unit tests to validate the business logic and the mathematical models.

To prove the efficiency of the IFS-Predictor and KKT-Eviction algorithms under high-load scenarios, **k6** is utilized.

Run the load test using the provided script:
```bash
k6 run BSL.Test/loadtest.js
```
*The load tests simulate self-similar traffic bursts to trigger the predictive prefetching and monitor the cache hit/miss ratio under constrained memory.*
