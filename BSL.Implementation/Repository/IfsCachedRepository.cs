using BSL.Implementation.Metrics;
using BSL.Models.Interface;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace BSL.Implementation.Repository
{
    /// <summary>
    /// Интеллектуальный репозиторий с поддержкой IFS-предиктора и Cost-Aware вытеснения.
    /// Реализует паттерн ленивой переоценки (Lazy Re-evaluation) для O(1) чтения в Highload.
    /// </summary>
    public class IfsCachedRepository : RepositoryDecorator
    {
        private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();

        private readonly PriorityQueue<string, double> _evictionQueue = new();

        private readonly object _heapLock = new();

        private readonly ITelemetryAggregator _telemetryAggregator;

        private readonly long _maxMemoryBytes;
        private long _currentMemoryBytes = 0;

        public IfsCachedRepository(
            IRepository innerRepository,
            ITelemetryAggregator telemetryAggregator,
            long maxMemoryBytes)
            : base(innerRepository)
        {
            _telemetryAggregator = telemetryAggregator;
            _maxMemoryBytes = maxMemoryBytes;
        }

        public override async Task<T> GetByName<T>(string name) where T : class
        {
            string cacheKey = $"{typeof(T).Name}:{name}";

            _telemetryAggregator.RecordHit(cacheKey);

            if (_cache.TryGetValue(cacheKey, out var entry))
            {
                entry.RecordHit();

                if (MetricsContext.Current.Value != null)
                {
                    MetricsContext.Current.Value.IsHit = true;
                }
                return entry.Data as T;
            }

            var sw = Stopwatch.StartNew();

            var dataFromFile = await base.GetByName<T>(name);
            sw.Stop();

            if (dataFromFile != null)
            {
                var newEntry = new CacheEntry
                {
                    Data = dataFromFile,
                    FetchDurationMs = sw.Elapsed.TotalMilliseconds,
                    SizeBytes = ObjectSizeApproximator.EstimateSizeBytes(dataFromFile)
                };

                newEntry.RecordHit();

                EnsureMemoryCapacity(newEntry.SizeBytes);

                if (_cache.TryAdd(cacheKey, newEntry))
                {
                    Interlocked.Add(ref _currentMemoryBytes, newEntry.SizeBytes);

                    lock (_heapLock)
                    {
                        _evictionQueue.Enqueue(cacheKey, newEntry.CalculateRho());
                    }
                }
            }

            return dataFromFile;
        }

        /// <summary>
        /// Реактивное вытеснение (ККТ-Eviction) с ленивой переоценкой.
        /// </summary>
        private void EnsureMemoryCapacity(long requiredBytes)
        {
            if (Interlocked.Read(ref _currentMemoryBytes) + requiredBytes <= _maxMemoryBytes)
                return;

            lock (_heapLock)
            {
                int loopBreaker = 0;
                const int MaxSpins = 50;

                while (Interlocked.Read(ref _currentMemoryBytes) + requiredBytes > _maxMemoryBytes)
                {
                    if (!_evictionQueue.TryDequeue(out var candidateKey, out var oldRho))
                    {
                        break;
                    }

                    if (!_cache.TryGetValue(candidateKey, out var entry))
                        continue;

                    double actualRho = entry.CalculateRho();

                    if (actualRho > oldRho + 0.0001 && loopBreaker < MaxSpins)
                    {
                        _evictionQueue.Enqueue(candidateKey, actualRho);
                        loopBreaker++;
                    }
                    else
                    {
                        if (_cache.TryRemove(candidateKey, out var evictedEntry))
                        {
                            Interlocked.Add(ref _currentMemoryBytes, -evictedEntry.SizeBytes);
                            loopBreaker = 0;
                        }
                    }
                }
            }
        }
    }
}