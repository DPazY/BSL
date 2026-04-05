using BSL.Models;
using BSL.Models.Interface;
using Microsoft.Extensions.Options;

namespace BSL.App.Service
{
    /// <summary>
    /// Проактивный демон (Prefetcher), реализующий стратегию упреждающего кэширования (Cache Warming).
    /// Использует IFS-предиктор для анализа аттракторов трафика в реальном времени.
    /// </summary>
    public class IfsBackgroundPrefetcher : BackgroundService
    {
        private readonly ITelemetryAggregator _telemetryAggregator;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly AppSettings _options;
        private readonly ILogger<IfsBackgroundPrefetcher> _logger;

        // Интервал дискретизации для математической модели (t = 2 сек)
        private static readonly TimeSpan SamplingInterval = TimeSpan.FromSeconds(2);

        public IfsBackgroundPrefetcher(
            ITelemetryAggregator telemetryAggregator,
            IServiceScopeFactory scopeFactory,
            AppSettings options,
            ILogger<IfsBackgroundPrefetcher> logger)
        {
            _telemetryAggregator = telemetryAggregator;
            _scopeFactory = scopeFactory;
            _options = options;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Запущен фоновый демон префетчинга IFS. Интервал дискретизации: {Interval}s", SamplingInterval.TotalSeconds);

            // Используем PeriodicTimer для точного соблюдения тактов без накопления задержек
            using var timer = new PeriodicTimer(SamplingInterval);

            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    await ProcessPrefetchCycleAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Работа фонового демона IFS была штатно остановлена.");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Критическая ошибка в цикле префетчинга IFS.");
            }
        }

        private async Task ProcessPrefetchCycleAsync(CancellationToken stoppingToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();

                var predictor = scope.ServiceProvider.GetRequiredService<IIfsPredictor>();
                var repository = scope.ServiceProvider.GetRequiredService<IRepository>();

                var activeKeys = _telemetryAggregator.CurrentKeys;
                double threshold = _options.PrefetchThreshold;

                foreach (var key in activeKeys)
                {
                    stoppingToken.ThrowIfCancellationRequested();

                    var history = _telemetryAggregator.GetTimeSeriesAndReset(key);

                    double predictedLambda = predictor.PredictNextLambda(history);

                    if (predictedLambda > threshold)
                    {
                        var parts = key.Split(':');
                        if (parts.Length == 2)
                        {
                            string entityType = parts[0];
                            string entityName = parts[1];

                            switch (entityType)
                            {
                                case "Book":
                                    repository.GetByName<Book>(entityName);
                                    break;
                                case "Patent":
                                    repository.GetByName<Patent>(entityName);
                                    break;
                                case "Newspaper":
                                    repository.GetByName<Newspaper>(entityName);
                                    break;
                                default:
                                    _logger.LogWarning("Неизвестный тип сущности для префетчинга: {EntityType}", entityType);
                                    break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Ошибка при обработке такта префетчера.");
            }
        }
    }
}