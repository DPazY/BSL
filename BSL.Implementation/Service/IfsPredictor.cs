using BSL.Models.Interface;

namespace BSL.Implementation
{
    /// <summary>
    /// Математическое ядро прогнозирования на основе модифицированного экспоненциального сглаживания (EMA) 
    /// с учетом фрактальной волатильности (IFS).
    /// </summary>
    public class IfsPredictor : IIfsPredictor
    {
        // Коэффициент сглаживания \alpha (от 0 до 1). 
        // 0.3 означает, что мы отдаем 30% веса новым данным и 70% - исторической памяти.
        private const double Alpha = 0.3;

        // Весовой коэффициент фрактального всплеска \gamma.
        // Определяет агрессивность префетчинга при резких скачках (эффект толпы).
        private const double Gamma = 0.5;

        public double PredictNextLambda(double[] historicalLambdas)
        {
            if (historicalLambdas == null || historicalLambdas.Length == 0)
                return 0.0;

            if (historicalLambdas.Length == 1)
                return historicalLambdas[0];

            double ema = historicalLambdas[0];
            double maxJump = 0.0;

            for (int i = 1; i < historicalLambdas.Length; i++)
            {
                // 1.  Экспоненциальное скользящее среднее (Базовый тренд)
                ema = Alpha * historicalLambdas[i] + (1 - Alpha) * ema;

                // 2. Локальный фрактальный всплеск (Градиент)
                double currentJump = Math.Abs(historicalLambdas[i] - historicalLambdas[i - 1]);
                if (currentJump > maxJump)
                {
                    maxJump = currentJump;
                }
            }

            // 3.  Итоговый прогноз: Тренд + Фрактальная поправка
            double predictedLambda = ema + Gamma * maxJump;

            return predictedLambda;
        }
    }
}