namespace BSL.Models
{

    /// <summary>
    /// Структура метаданных объекта для расчета математической модели.
    /// </summary>
    public class CacheEntry
    {
        public object Data { get; set; }

        // \lambda_i - наблюдаемая частота запросов
        public double RequestRateLambda { get; set; }

        // t_i - стоимость извлечения (мс)
        public double FetchDurationMs { get; set; }

        // w_i - размер в байтах (аппроксимация)
        public long SizeBytes { get; set; }

        public int Version { get; set; }

        // Вычисление удельной полезности по условиям ККТ: \rho_i = (\lambda_i * t_i) / w_i
        public double CalculateRho() => (RequestRateLambda * FetchDurationMs) / SizeBytes;
    }
}