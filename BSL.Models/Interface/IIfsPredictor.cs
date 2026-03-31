namespace BSL.Models.Interface
{
    /// <summary>
    /// Математическое ядро для расчета итерируемых систем функций (IFS) и прогнозирования фрактальных временных рядов.
    /// </summary>
    public interface IIfsPredictor
    {
        /// <summary>
        /// Вычисляет прогнозируемую интенсивность обращений на следующий квант времени.
        /// Реализует решение обратной задачи фрактальной геометрии (коллажирование).
        /// </summary>
        /// <param name="historicalLambdas">Массив исторических частот обращений на интервалах \Delta\tau.</param>
        /// <returns>Прогнозируемое значение \lambda_{i, future}.</returns>
        double PredictNextLambda(double[] historicalLambdas);
    }
}