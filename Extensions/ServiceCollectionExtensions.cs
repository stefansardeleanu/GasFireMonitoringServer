// File: Extensions/ServiceCollectionExtensions.cs
// PHASE 7.3 TASK 3: Service collection extensions for performance monitoring
// Extension methods for easy integration with dependency injection

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using GasFireMonitoringServer.Services.Infrastructure;

namespace GasFireMonitoringServer.Extensions
{
    /// <summary>
    /// Extension methods for service collection to add performance monitoring
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add comprehensive performance monitoring to the service collection
        /// </summary>
        public static IServiceCollection AddPerformanceMonitoring(this IServiceCollection services)
        {
            // Register performance monitoring service
            services.AddSingleton<IPerformanceMonitoringService, PerformanceMonitoringService>();

            // Register as hosted service for background monitoring
            services.AddHostedService<PerformanceMonitoringService>(serviceProvider =>
                serviceProvider.GetRequiredService<IPerformanceMonitoringService>() as PerformanceMonitoringService);

            // Register enhanced MQTT service wrapper
            services.AddSingleton<EnhancedMqttService>();

            return services;
        }

        /// <summary>
        /// Add performance-aware database context wrapper
        /// </summary>
        public static IServiceCollection AddPerformanceAwareDbContext<T>(this IServiceCollection services)
            where T : DbContext
        {
            services.AddScoped<PerformanceAwareDbContext>(serviceProvider =>
            {
                var context = serviceProvider.GetRequiredService<T>();
                var performanceService = serviceProvider.GetRequiredService<IPerformanceMonitoringService>();
                var logger = serviceProvider.GetRequiredService<ILogger<PerformanceAwareDbContext>>();

                return new PerformanceAwareDbContext(context, performanceService, logger);
            });

            return services;
        }
    }
}