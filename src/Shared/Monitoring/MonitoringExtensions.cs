using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

namespace Shared.Monitoring
{
    public static class MonitoringExtensions
    {
        public static IServiceCollection AddServiceMonitoring(this IServiceCollection services, string serviceName, string serviceVersion = "1.0.0")
        {
            services.AddOpenTelemetry()
                .WithMetrics(builder =>
                {
                    builder
                        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName, serviceVersion))
                        .AddAspNetCoreInstrumentation()
                        .AddRuntimeInstrumentation()
                        .AddPrometheusExporter();
                });

            return services;
        }
    }
}
