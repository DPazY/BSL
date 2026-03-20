using OpenTelemetry;
using OpenTelemetry.Metrics;

namespace BSL.Implementation.Metrics
{
    public class MetricPublisihingHelpers
    {
        public static MeterProvider GetMeterProvider(string meterName, string endpointUri)
        {
            return Sdk
                .CreateMeterProviderBuilder()
                .AddMeter(meterName)
                .AddPrometheusHttpListener(options => options.UriPrefixes = [endpointUri])
                .Build();
        }

    }
}
