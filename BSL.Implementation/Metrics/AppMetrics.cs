using System.Diagnostics.Metrics;

namespace BSL.Implementation.Metrics
{
    public class AppMetrics
    {
        public const string MeterName = "BSL.Metrics";

        private readonly Counter<int> _methodCallsCounter;
        private readonly Histogram<double> _methodDurationHistogram;

        public AppMetrics(IMeterFactory meterFactory)
        {
            var meter = meterFactory.Create(MeterName);

            _methodCallsCounter = meter.CreateCounter<int>(
                name: "bsl.repository.calls.count",
                description: "Общее количество вызовов методов репозитория");

            _methodDurationHistogram = meter.CreateHistogram<double>(
                name: "bsl.repository.method.duration",
                unit: "ms",
                description: "Время выполнения методов репозитория в миллисекундах");
        }

        public void RecordMethodExecution(string methodName, string targetType, double durationMs, bool isCacheHit = false)
        {
            var tags = new[]
            {
                new KeyValuePair<string, object?>("method.name", methodName),
                new KeyValuePair<string, object?>("target.type", targetType), 
                new KeyValuePair<string, object?>("cache.hit", isCacheHit.ToString().ToLower())
            };

            _methodCallsCounter.Add(1, tags);

            _methodDurationHistogram.Record(durationMs, tags);
        }
    }
}