using System.Diagnostics;

namespace BSL.Implementation
{
    public class CacheEntry
    {
        public object Data { get; set; }

        public double FetchDurationMs { get; set; }
        public long SizeBytes { get; set; }

        private double _lambda = 0.0;

        // Временная метка последнего обращения
        private long _lastUpdateTimestamp = Stopwatch.GetTimestamp();

        // Период полураспада в секундах. Подбирается эмпирически под профиль нагрузки.
        // Вес хита уменьшится вдвое через n секунд простоя.
        private static readonly double HalfLifeSeconds = 60.0;

        /// <summary>
        /// Фиксирует обращение к объекту и обновляет значение интенсивности (EWMA).
        /// </summary>
        public void RecordHit()
        {
            long now = Stopwatch.GetTimestamp();
            long last = Interlocked.Exchange(ref _lastUpdateTimestamp, now);

            double elapsedSeconds = (now - last) / (double)Stopwatch.Frequency;

            // Расчет коэффициента экспоненциального затухания: e^(-ln(2) * t / T)
            double decayFactor = Math.Exp(-Math.Log(2) * elapsedSeconds / HalfLifeSeconds);

            // Атомарное обновление lambda с использованием lock-free паттерна CAS (Compare-And-Swap)
            double initialValue, computedValue;
            do
            {
                initialValue = _lambda;
                // Старое значение затухает + добавляется вес нового хита (1.0)
                computedValue = (initialValue * decayFactor) + 1.0;
            }
            while (Interlocked.CompareExchange(ref _lambda, computedValue, initialValue) != initialValue);
        }

        /// <summary>
        /// Вычисляет индекс удельной полезности \rho_i.
        /// Соответствует формуле из модели: \rho_i = (\lambda_i * t_i) / w_i
        /// </summary>
        public double CalculateRho()
        {
            // Перед расчетом приоритета применяем затухание для времени, прошедшего с последнего хита.
            // Это гарантирует, что давно не используемые объекты "подешевеют" прямо в момент проверки вытеснения.
            long now = Stopwatch.GetTimestamp();
            long last = Interlocked.Read(ref _lastUpdateTimestamp);
            double elapsedSeconds = (now - last) / (double)Stopwatch.Frequency;

            double decayFactor = Math.Exp(-Math.Log(2) * elapsedSeconds / HalfLifeSeconds);
            double currentLambda = _lambda * decayFactor;

            // Формула ККТ
            return (currentLambda * FetchDurationMs) / Math.Max(1, SizeBytes);
        }
    }
}