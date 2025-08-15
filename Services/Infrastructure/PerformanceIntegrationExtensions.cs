// File: Services/Infrastructure/PerformanceIntegrationExtensions.cs
// PHASE 7.3 TASK 3: Clean integration extensions for performance monitoring
// Extension methods for integrating performance monitoring with existing services

using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GasFireMonitoringServer.Services.Infrastructure
{
    /// <summary>
    /// Extension methods for integrating performance monitoring with existing services
    /// </summary>
    public static class PerformanceIntegrationExtensions
    {
        /// <summary>
        /// Extension method to add performance tracking to database operations
        /// </summary>
        public static async Task<T> WithPerformanceTracking<T>(
            this Task<T> task,
            IPerformanceMonitoringService performanceService,
            string operationName)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var result = await task;
                stopwatch.Stop();

                performanceService.RecordDatabaseQuery(operationName, stopwatch.ElapsedMilliseconds, true);
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                performanceService.RecordDatabaseQuery(operationName, stopwatch.ElapsedMilliseconds, false);
                throw;
            }
        }

        /// <summary>
        /// Extension method to add performance tracking to async operations
        /// </summary>
        public static async Task WithPerformanceTracking(
            this Task task,
            IPerformanceMonitoringService performanceService,
            string operationName)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                await task;
                stopwatch.Stop();

                performanceService.RecordDatabaseQuery(operationName, stopwatch.ElapsedMilliseconds, true);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                performanceService.RecordDatabaseQuery(operationName, stopwatch.ElapsedMilliseconds, false);
                throw;
            }
        }
    }
}