using BSL.Models.Interface;
using System;

namespace BSL.Implementation.Service
{
    /// <summary>
    /// Математическое ядро прогнозирования на основе итерируемых систем функций (IFS).
    /// Реализует решение обратной задачи фрактальной геометрии (коллажирование)
    /// для поиска самоподобных участков во временном ряде нагрузки.
    /// </summary>
    public class IfsPredictor : IIfsPredictor
    {
        // Размер окна (Range block), по которому ищется паттерн.
        // Определяет чувствительность к краткосрочным всплескам.
        private const int WindowSize = 5;

        public double PredictNextLambda(double[] historicalLambdas)
        {
            if (historicalLambdas == null || historicalLambdas.Length <= WindowSize + 1)
            {
                return historicalLambdas?.LastOrDefault() ?? 0.0;
            }

            int n = historicalLambdas.Length;

            Span<double> range = historicalLambdas.AsSpan(n - WindowSize, WindowSize);

            double bestError = double.MaxValue;
            double bestS = 0;
            double bestO = 0;
            int bestDomainIndex = -1;

            // Итерация по историческим данным для поиска наиболее похожего Domain-блока
            for (int i = 0; i <= n - WindowSize - 1; i++)
            {
                Span<double> domain = historicalLambdas.AsSpan(i, WindowSize);

                CalculateAffineCoefficients(domain, range, out double s, out double o);

                // Оценка ошибки аппроксимации (Евклидово расстояние)
                double error = CalculateMse(domain, range, s, o);

                if (error < bestError)
                {
                    bestError = error;
                    bestS = s;
                    bestO = o;
                    bestDomainIndex = i;
                }
            }

            if (bestDomainIndex == -1)
                return historicalLambdas[^1]; 

            double nextHistoricalValue = historicalLambdas[bestDomainIndex + WindowSize];
            double predictedLambda = bestS * nextHistoricalValue + bestO;

            return Math.Max(0.0, predictedLambda);
        }

        /// <summary>
        /// Вычисление коэффициентов сжатия (s) и сдвига (o) методом наименьших квадратов.
        /// </summary>
        private void CalculateAffineCoefficients(Span<double> domain, Span<double> range, out double s, out double o)
        {
            double sumD = 0, sumR = 0, sumDR = 0, sumD2 = 0;
            int k = domain.Length;

            for (int i = 0; i < k; i++)
            {
                double d = domain[i];
                double r = range[i];

                sumD += d;
                sumR += r;
                sumDR += d * r;
                sumD2 += d * d;
            }

            double denominator = k * sumD2 - sumD * sumD;

            if (Math.Abs(denominator) < 1e-9)
            {
                s = 0;
                o = sumR / k;
            }
            else
            {
                s = (k * sumDR - sumD * sumR) / denominator;

                // Условие сжимающего отображения (по теореме Хатчинсона |s| < 1)
                s = Math.Clamp(s, -1.5, 1.5);

                o = (sumR - s * sumD) / k;
            }
        }

        /// <summary>
        /// Вычисление среднеквадратичной ошибки (MSE) при наложении аффинного преобразования.
        /// </summary>
        private double CalculateMse(Span<double> domain, Span<double> range, double s, double o)
        {
            double error = 0;
            for (int i = 0; i < domain.Length; i++)
            {
                double diff = (s * domain[i] + o) - range[i];
                error += diff * diff;
            }
            return error / domain.Length;
        }
    }
}