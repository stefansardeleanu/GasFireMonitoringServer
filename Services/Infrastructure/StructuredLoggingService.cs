// File: Services/Infrastructure/StructuredLoggingService.cs
using Serilog.Context;

namespace GasFireMonitoringServer.Services.Infrastructure
{
    /// <summary>
    /// Service for structured logging with performance tracking
    /// </summary>
    public interface IStructuredLoggingService
    {
        IDisposable BeginScope(string operationType, object? properties = null);
        void LogConfigurationOperation(string operation, int? siteId = null, bool success = true, long? elapsedMs = null);
        void LogLayoutOperation(string operation, int siteId, string? fileName = null, bool success = true, long? elapsedMs = null);
        void LogMqttOperation(string operation, string? topic = null, bool success = true, string? error = null);
        void LogDatabaseOperation(string operation, string entity, int? recordCount = null, long? elapsedMs = null);
    }

    public class StructuredLoggingService : IStructuredLoggingService
    {
        private readonly ILogger<StructuredLoggingService> _logger;

        public StructuredLoggingService(ILogger<StructuredLoggingService> logger)
        {
            _logger = logger;
        }

        public IDisposable BeginScope(string operationType, object? properties = null)
        {
            var scopeProps = new List<IDisposable>
            {
                LogContext.PushProperty("OperationType", operationType)
            };

            if (properties != null)
            {
                var props = properties.GetType().GetProperties();
                foreach (var prop in props)
                {
                    var value = prop.GetValue(properties);
                    if (value != null)
                    {
                        scopeProps.Add(LogContext.PushProperty(prop.Name, value));
                    }
                }
            }

            return new CompositeDisposable(scopeProps);
        }

        public void LogConfigurationOperation(string operation, int? siteId = null, bool success = true, long? elapsedMs = null)
        {
            using (LogContext.PushProperty("OperationType", "Configuration"))
            using (LogContext.PushProperty("Operation", operation))
            using (LogContext.PushProperty("SiteId", siteId))
            using (LogContext.PushProperty("Success", success))
            using (LogContext.PushProperty("ElapsedMs", elapsedMs))
            {
                if (success)
                {
                    if (elapsedMs > 500)
                    {
                        _logger.LogWarning("SLOW Configuration operation: {Operation} for site {SiteId} took {ElapsedMs}ms",
                            operation, siteId, elapsedMs);
                    }
                    else
                    {
                        _logger.LogInformation("Configuration operation: {Operation} for site {SiteId} completed in {ElapsedMs}ms",
                            operation, siteId, elapsedMs);
                    }
                }
                else
                {
                    _logger.LogError("Configuration operation FAILED: {Operation} for site {SiteId}",
                        operation, siteId);
                }
            }
        }

        public void LogLayoutOperation(string operation, int siteId, string? fileName = null, bool success = true, long? elapsedMs = null)
        {
            using (LogContext.PushProperty("OperationType", "Layout"))
            using (LogContext.PushProperty("Operation", operation))
            using (LogContext.PushProperty("SiteId", siteId))
            using (LogContext.PushProperty("FileName", fileName))
            using (LogContext.PushProperty("Success", success))
            using (LogContext.PushProperty("ElapsedMs", elapsedMs))
            {
                if (success)
                {
                    _logger.LogInformation("Layout operation: {Operation} for site {SiteId} file {FileName} completed in {ElapsedMs}ms",
                        operation, siteId, fileName, elapsedMs);
                }
                else
                {
                    _logger.LogError("Layout operation FAILED: {Operation} for site {SiteId} file {FileName}",
                        operation, siteId, fileName);
                }
            }
        }

        public void LogMqttOperation(string operation, string? topic = null, bool success = true, string? error = null)
        {
            using (LogContext.PushProperty("OperationType", "MQTT"))
            using (LogContext.PushProperty("Operation", operation))
            using (LogContext.PushProperty("Topic", topic))
            using (LogContext.PushProperty("Success", success))
            {
                if (success)
                {
                    _logger.LogDebug("MQTT operation: {Operation} for topic {Topic}", operation, topic);
                }
                else
                {
                    _logger.LogError("MQTT operation FAILED: {Operation} for topic {Topic} - {Error}",
                        operation, topic, error);
                }
            }
        }

        public void LogDatabaseOperation(string operation, string entity, int? recordCount = null, long? elapsedMs = null)
        {
            using (LogContext.PushProperty("OperationType", "Database"))
            using (LogContext.PushProperty("Operation", operation))
            using (LogContext.PushProperty("Entity", entity))
            using (LogContext.PushProperty("RecordCount", recordCount))
            using (LogContext.PushProperty("ElapsedMs", elapsedMs))
            {
                if (elapsedMs > 2000) // Database operations slower than 2 seconds
                {
                    _logger.LogWarning("SLOW Database operation: {Operation} on {Entity} ({RecordCount} records) took {ElapsedMs}ms",
                        operation, entity, recordCount, elapsedMs);
                }
                else
                {
                    _logger.LogDebug("Database operation: {Operation} on {Entity} ({RecordCount} records) completed in {ElapsedMs}ms",
                        operation, entity, recordCount, elapsedMs);
                }
            }
        }

        private class CompositeDisposable : IDisposable
        {
            private readonly List<IDisposable> _disposables;

            public CompositeDisposable(List<IDisposable> disposables)
            {
                _disposables = disposables;
            }

            public void Dispose()
            {
                foreach (var disposable in _disposables)
                {
                    disposable.Dispose();
                }
            }
        }
    }
}