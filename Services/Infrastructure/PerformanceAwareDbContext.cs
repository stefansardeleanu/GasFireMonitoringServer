// File: Services/Infrastructure/PerformanceAwareDbContext.cs
// PHASE 7.3 TASK 3: Performance-aware database context wrapper
// Wrapper for tracking database operations with performance monitoring

using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GasFireMonitoringServer.Services.Infrastructure
{
    /// <summary>
    /// Performance-aware DbContext wrapper for tracking database operations
    /// </summary>
    public class PerformanceAwareDbContext
    {
        private readonly DbContext _context;
        private readonly IPerformanceMonitoringService _performanceService;
        private readonly ILogger _logger;

        public PerformanceAwareDbContext(
            DbContext context,
            IPerformanceMonitoringService performanceService,
            ILogger logger)
        {
            _context = context;
            _performanceService = performanceService;
            _logger = logger;
        }

        /// <summary>
        /// Execute a query with performance tracking
        /// </summary>
        public async Task<T> ExecuteWithTracking<T>(
            Func<DbContext, Task<T>> operation,
            string operationName)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var result = await operation(_context);
                stopwatch.Stop();

                _performanceService.RecordDatabaseQuery(operationName, stopwatch.ElapsedMilliseconds, true);

                // Log slow queries
                if (stopwatch.ElapsedMilliseconds > 500)
                {
                    _logger.LogWarning("Slow database operation: {OperationName} took {ElapsedMs}ms",
                        operationName, stopwatch.ElapsedMilliseconds);
                }

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _performanceService.RecordDatabaseQuery(operationName, stopwatch.ElapsedMilliseconds, false);

                _logger.LogError(ex, "Database operation failed: {OperationName} after {ElapsedMs}ms",
                    operationName, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        /// <summary>
        /// Execute a non-query operation with performance tracking
        /// </summary>
        public async Task ExecuteWithTracking(
            Func<DbContext, Task> operation,
            string operationName)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                await operation(_context);
                stopwatch.Stop();

                _performanceService.RecordDatabaseQuery(operationName, stopwatch.ElapsedMilliseconds, true);

                // Log slow operations
                if (stopwatch.ElapsedMilliseconds > 500)
                {
                    _logger.LogWarning("Slow database operation: {OperationName} took {ElapsedMs}ms",
                        operationName, stopwatch.ElapsedMilliseconds);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _performanceService.RecordDatabaseQuery(operationName, stopwatch.ElapsedMilliseconds, false);

                _logger.LogError(ex, "Database operation failed: {OperationName} after {ElapsedMs}ms",
                    operationName, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }
    }
}