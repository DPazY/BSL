using BSL.Models;
using BSL.Models.Interface;
using Microsoft.Extensions.Options;

namespace BSL.Implementation.Service
{
    public class IfsBackgroundPrefetcher : BackgroundService
    {
        private readonly ITelemetryAggregator _telemetryAggregator;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IOptions<AppSettings> _options;
        private readonly ILogger<IfsBackgroundPrefetcher> _logger; 

        public IfsBackgroundPrefetcher(
            ITelemetryAggregator telemetryAggregator,
            IServiceScopeFactory scopeFactory,
            IOptions<AppSettings> options,
            ILogger<IfsBackgroundPrefetcher> logger) 
        {
            _telemetryAggregator = telemetryAggregator;
            _scopeFactory = scopeFactory;
            _options = options;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Запущен фоновый демон префетчинга IFS.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();

                    var predictor = scope.ServiceProvider.GetRequiredService<IIfsPredictor>();
                    var repository = scope.ServiceProvider.GetRequiredService<IRepository>();

                    var activeKeys = _telemetryAggregator.CurrentKeys;

                    foreach (var key in activeKeys)
                    {
                        stoppingToken.ThrowIfCancellationRequested();

                        var history = _telemetryAggregator.GetTimeSeriesAndReset(key);
                        double predictedLambda = predictor.PredictNextLambda(history);
                        double threshold = _options.Value.PrefetchThreshold;

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
                                        _logger.LogWarning("Неизвестный тип сущности для кэширования: {EntityType}", entityType);
                                        break;
                                }
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Работа фонового демона IFS была отменена.");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Произошла ошибка при выполнении цикла префетчинга IFS.");
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }

            _logger.LogInformation("Фоновый демон префетчинга IFS завершил работу.");
        }
    }
}