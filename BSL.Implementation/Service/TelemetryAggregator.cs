using BSL.Implementation.Collection;
using BSL.Models.Interface;
using System.Collections.Concurrent;

namespace BSL.Implementation
{
    public class TelemetryAggregator : ITelemetryAggregator
    {
        private readonly ConcurrentDictionary<string, ObjectHistory> _telemetryHistory = new();

        private const int IfsWindowSize = 10;

        public void RecordHit(string key)
        {
            var history = _telemetryHistory.GetOrAdd(key, _ => new ObjectHistory(IfsWindowSize));

            history.RecordHit();
        }

        public string[] CurrentKeys 
        {
            get
                {
                    return _telemetryHistory.Keys.ToArray();
                }
        }

        public double[] GetTimeSeriesAndReset(string key)
        {
            if (_telemetryHistory.TryGetValue(key, out var history))
            {
                return history.GetTimeSeriesAndReset();
            }

            return new double[IfsWindowSize];
        }
    }
}