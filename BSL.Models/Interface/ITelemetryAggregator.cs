namespace BSL.Models.Interface
{
    /// <summary>
    /// Потокобезопасный агрегатор телеметрии для формирования исторических "окон" метрик.
    /// </summary>
    public interface ITelemetryAggregator
    {
        /// <summary>
        /// Инкрементирует счетчик обращений к объекту в текущем кванте времени \Delta\tau.
        /// </summary>
        void RecordHit(string key);

        /// <summary>
        /// Возвращает исторический временной ряд для ключа и выполняет сдвиг временного окна.
        /// </summary>
        double[] GetTimeSeriesAndReset(string key);

        /// <summary>
        /// Возвращает список всех ключей, по которым собирается статистика.
        /// </summary>
        string[] CurrentKeys { get; }
    }
}