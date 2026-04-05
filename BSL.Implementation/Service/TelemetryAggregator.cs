using BSL.Implementation.Collection;
using BSL.Models.Interface;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace BSL.Implementation
{
    /// <summary>
    /// Агрегатор телеметрии для сбора исторических данных (интенсивности запросов \lambda).
    /// Использует неблокирующую очередь (Channels) для минимизации задержек на пути чтения (Read Path).
    /// </summary>
    public class TelemetryAggregator : ITelemetryAggregator, IDisposable
    {
        private readonly ConcurrentDictionary<string, ObjectHistory> _telemetryHistory = new();

        private readonly Channel<string> _hitChannel;
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _processingTask;

        private const int IfsWindowSize = 10;

        public TelemetryAggregator()
        {
            var options = new BoundedChannelOptions(100_000)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true, 
                SingleWriter = false 
            };

            _hitChannel = Channel.CreateBounded<string>(options);

            _processingTask = Task.Run(ProcessQueueAsync);
        }

        /// <summary>
        /// Асинхронно регистрирует обращение к объекту (Fire-and-Forget).
        /// Время выполнения: O(1), без выделения памяти (allocation-free).
        /// </summary>
        public void RecordHit(string key)
        {
            // TryWrite моментально возвращает управление. Поток чтения не ждет агрегации.
            _hitChannel.Writer.TryWrite(key);
        }

        /// <summary>
        /// Фоновый воркер, который выгребает события из канала и обновляет математическую историю.
        /// </summary>
        private async Task ProcessQueueAsync()
        {
            try
            {
                await foreach (var key in _hitChannel.Reader.ReadAllAsync(_cts.Token))
                {
                    var history = _telemetryHistory.GetOrAdd(key, _ => new ObjectHistory(IfsWindowSize));

                    history.RecordHit();
                }
            }
            catch (OperationCanceledException)
            {
                
            }
        }

        public string[] CurrentKeys => _telemetryHistory.Keys.ToArray();

        public double[] GetTimeSeriesAndReset(string key)
        {
            if (_telemetryHistory.TryGetValue(key, out var history))
            {
                return history.GetTimeSeriesAndReset();
            }

            return new double[IfsWindowSize];
        }

        public void Dispose()
        {
            _cts.Cancel();
            _processingTask.Wait(TimeSpan.FromSeconds(2));
            _cts.Dispose();
        }
    }
}