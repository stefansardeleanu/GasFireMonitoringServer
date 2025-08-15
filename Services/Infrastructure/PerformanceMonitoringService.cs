// File: Services/Infrastructure/PerformanceMonitoringService.cs
// PHASE 7.3 TASK 3: Comprehensive Performance Monitoring Implementation
// Real-time system health monitoring for gas and fire monitoring system

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using GasFireMonitoringServer.Data;
using GasFireMonitoringServer.Hubs;
using GasFireMonitoringServer.Services.Infrastructure.Interfaces;

namespace GasFireMonitoringServer.Services.Infrastructure
{
    /// <summary>
    /// Comprehensive performance monitoring service for industrial gas and fire monitoring system
    /// Tracks system health, MQTT connectivity, database performance, and real-time metrics
    /// </summary>
    public class PerformanceMonitoringService : BackgroundService, IPerformanceMonitoringService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PerformanceMonitoringService> _logger;
        private readonly IHubContext<MonitoringHub> _hubContext;

        // Performance metrics storage
        private readonly ConcurrentDictionary<string, PerformanceMetric> _metrics = new();
        private readonly ConcurrentQueue<DatabaseQueryMetric> _recentQueries = new();
        private readonly ConcurrentQueue<ApiRequestMetric> _recentApiRequests = new();
        private readonly ConcurrentQueue<MqttMessageMetric> _recentMqttMessages = new();

        // System health tracking
        private SystemHealthStatus _currentHealth = new();
        private DateTime _lastHealthCheck = DateTime.MinValue;
        private DateTime _lastMetricsReport = DateTime.MinValue;

        // Performance counters
        private long _totalApiRequests = 0;
        private long _totalDatabaseQueries = 0;
        private long _totalMqttMessages = 0;
        private long _totalSignalRBroadcasts = 0;

        // Configuration
        private readonly TimeSpan _healthCheckInterval = TimeSpan.FromMinutes(1);
        private readonly TimeSpan _metricsReportInterval = TimeSpan.FromMinutes(5);
        private readonly int _maxRecentMetrics = 1000;

        public PerformanceMonitoringService(
            IServiceProvider serviceProvider,
            ILogger<PerformanceMonitoringService> logger,
            IHubContext<MonitoringHub> hubContext)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _hubContext = hubContext;

            // Initialize base metrics
            InitializeMetrics();
        }

        #region IPerformanceMonitoringService Implementation

        public void RecordApiRequest(string endpoint, string method, int statusCode, long elapsedMs)
        {
            Interlocked.Increment(ref _totalApiRequests);

            var metric = new ApiRequestMetric
            {
                Endpoint = endpoint,
                Method = method,
                StatusCode = statusCode,
                ElapsedMs = elapsedMs,
                Timestamp = DateTime.UtcNow
            };

            _recentApiRequests.Enqueue(metric);

            // Keep only recent metrics
            if (_recentApiRequests.Count > _maxRecentMetrics)
            {
                _recentApiRequests.TryDequeue(out _);
            }

            // Update performance metrics
            UpdateMetric("api_request_count", 1, MetricType.Counter);
            UpdateMetric($"api_{method.ToLower()}_requests", 1, MetricType.Counter);
            UpdateMetric("api_response_time", elapsedMs, MetricType.Gauge);

            // Track slow requests
            if (elapsedMs > 1000)
            {
                UpdateMetric("api_slow_requests", 1, MetricType.Counter);
                _logger.LogWarning("Slow API request: {Method} {Endpoint} took {ElapsedMs}ms",
                    method, endpoint, elapsedMs);
            }

            // Track error rates
            if (statusCode >= 400)
            {
                UpdateMetric("api_error_count", 1, MetricType.Counter);
            }
        }

        public void RecordDatabaseQuery(string query, long elapsedMs, bool isSuccessful = true)
        {
            Interlocked.Increment(ref _totalDatabaseQueries);

            var metric = new DatabaseQueryMetric
            {
                Query = query,
                ElapsedMs = elapsedMs,
                IsSuccessful = isSuccessful,
                Timestamp = DateTime.UtcNow
            };

            _recentQueries.Enqueue(metric);

            // Keep only recent metrics
            if (_recentQueries.Count > _maxRecentMetrics)
            {
                _recentQueries.TryDequeue(out _);
            }

            // Update performance metrics
            UpdateMetric("db_query_count", 1, MetricType.Counter);
            UpdateMetric("db_query_time", elapsedMs, MetricType.Gauge);

            if (isSuccessful)
            {
                UpdateMetric("db_successful_queries", 1, MetricType.Counter);
            }
            else
            {
                UpdateMetric("db_failed_queries", 1, MetricType.Counter);
                _logger.LogError("Database query failed: {Query}", query.Substring(0, Math.Min(100, query.Length)));
            }

            // Track slow queries
            if (elapsedMs > 500)
            {
                UpdateMetric("db_slow_queries", 1, MetricType.Counter);
                _logger.LogWarning("Slow database query: {Query} took {ElapsedMs}ms",
                    query.Substring(0, Math.Min(100, query.Length)), elapsedMs);
            }
        }

        public void RecordMqttMessage(string topic, bool isSuccessful = true, string? errorMessage = null)
        {
            Interlocked.Increment(ref _totalMqttMessages);

            var metric = new MqttMessageMetric
            {
                Topic = topic,
                IsSuccessful = isSuccessful,
                ErrorMessage = errorMessage,
                Timestamp = DateTime.UtcNow
            };

            _recentMqttMessages.Enqueue(metric);

            // Keep only recent metrics
            if (_recentMqttMessages.Count > _maxRecentMetrics)
            {
                _recentMqttMessages.TryDequeue(out _);
            }

            // Update performance metrics
            UpdateMetric("mqtt_message_count", 1, MetricType.Counter);

            if (isSuccessful)
            {
                UpdateMetric("mqtt_successful_messages", 1, MetricType.Counter);
            }
            else
            {
                UpdateMetric("mqtt_failed_messages", 1, MetricType.Counter);
                _logger.LogError("MQTT message processing failed for topic {Topic}: {Error}", topic, errorMessage);
            }

            // Track message rate
            UpdateMetric("mqtt_message_rate", 1, MetricType.Rate);
        }

        public void RecordSignalRBroadcast(string messageType, int clientCount)
        {
            Interlocked.Increment(ref _totalSignalRBroadcasts);

            UpdateMetric("signalr_broadcast_count", 1, MetricType.Counter);
            UpdateMetric($"signalr_{messageType}_broadcasts", 1, MetricType.Counter);
            UpdateMetric("signalr_client_count", clientCount, MetricType.Gauge);
        }

        public async Task<SystemHealthStatus> GetSystemHealthAsync()
        {
            return await Task.FromResult(_currentHealth);
        }

        public async Task<Dictionary<string, object>> GetPerformanceMetricsAsync()
        {
            var metrics = new Dictionary<string, object>();

            // Add current metrics
            foreach (var metric in _metrics.Values)
            {
                metrics[metric.Name] = new
                {
                    value = metric.Value,
                    type = metric.Type.ToString(),
                    lastUpdated = metric.LastUpdated
                };
            }

            // Add aggregate statistics
            metrics["api_requests_last_hour"] = await GetApiRequestsLastHour();
            metrics["db_queries_last_hour"] = await GetDatabaseQueriesLastHour();
            metrics["mqtt_messages_last_hour"] = await GetMqttMessagesLastHour();
            metrics["average_response_time"] = await GetAverageResponseTime();
            metrics["error_rate"] = await GetErrorRate();

            return metrics;
        }

        #endregion

        #region Background Service Implementation

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Performance monitoring service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var now = DateTime.UtcNow;

                    // Perform health checks
                    if (now - _lastHealthCheck >= _healthCheckInterval)
                    {
                        await PerformHealthCheckAsync();
                        _lastHealthCheck = now;
                    }

                    // Report metrics
                    if (now - _lastMetricsReport >= _metricsReportInterval)
                    {
                        await ReportMetricsAsync();
                        _lastMetricsReport = now;
                    }

                    // Update system metrics
                    await UpdateSystemMetricsAsync();

                    // Wait before next iteration
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in performance monitoring service");
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }

            _logger.LogInformation("Performance monitoring service stopped");
        }

        #endregion

        #region Private Methods

        private void InitializeMetrics()
        {
            // Initialize counters
            UpdateMetric("api_request_count", 0, MetricType.Counter);
            UpdateMetric("db_query_count", 0, MetricType.Counter);
            UpdateMetric("mqtt_message_count", 0, MetricType.Counter);
            UpdateMetric("signalr_broadcast_count", 0, MetricType.Counter);

            // Initialize gauges
            UpdateMetric("system_memory_mb", 0, MetricType.Gauge);
            UpdateMetric("active_connections", 0, MetricType.Gauge);
            UpdateMetric("cpu_usage_percent", 0, MetricType.Gauge);
        }

        private void UpdateMetric(string name, double value, MetricType type)
        {
            _metrics.AddOrUpdate(name,
                new PerformanceMetric { Name = name, Value = value, Type = type, LastUpdated = DateTime.UtcNow },
                (key, existing) =>
                {
                    switch (type)
                    {
                        case MetricType.Counter:
                            existing.Value += value;
                            break;
                        case MetricType.Gauge:
                            existing.Value = value;
                            break;
                        case MetricType.Rate:
                            // Simple rate calculation - messages per minute
                            var timeDiff = DateTime.UtcNow - existing.LastUpdated;
                            if (timeDiff.TotalMinutes > 0)
                            {
                                existing.Value = value / timeDiff.TotalMinutes;
                            }
                            break;
                    }
                    existing.LastUpdated = DateTime.UtcNow;
                    return existing;
                });
        }

        private async Task PerformHealthCheckAsync()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var mqttService = scope.ServiceProvider.GetService<IMqttService>();

                var health = new SystemHealthStatus
                {
                    Timestamp = DateTime.UtcNow,
                    OverallStatus = "healthy"
                };

                // Database health check
                try
                {
                    var stopwatch = Stopwatch.StartNew();
                    var canConnect = await dbContext.Database.CanConnectAsync();
                    stopwatch.Stop();

                    health.DatabaseHealth = new DatabaseHealth
                    {
                        IsConnected = canConnect,
                        ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                        Status = canConnect ? "healthy" : "unhealthy"
                    };

                    if (canConnect)
                    {
                        var sensorCount = await dbContext.Sensors.CountAsync();
                        var recentAlarms = await dbContext.Alarms
                            .Where(a => a.Timestamp >= DateTime.UtcNow.AddHours(-1))
                            .CountAsync();

                        health.DatabaseHealth.SensorCount = sensorCount;
                        health.DatabaseHealth.RecentAlarmCount = recentAlarms;
                    }
                }
                catch (Exception ex)
                {
                    health.DatabaseHealth = new DatabaseHealth
                    {
                        IsConnected = false,
                        Status = "unhealthy",
                        ErrorMessage = ex.Message
                    };
                    health.OverallStatus = "degraded";
                }

                // MQTT health check
                health.MqttHealth = new MqttHealth
                {
                    IsConnected = mqttService?.IsConnected ?? false,
                    Status = mqttService?.IsConnected == true ? "healthy" : "unhealthy",
                    MessageRate = GetMqttMessageRate()
                };

                if (health.MqttHealth.IsConnected == false)
                {
                    health.OverallStatus = "degraded";
                }

                // System resource health
                var process = Process.GetCurrentProcess();
                health.SystemHealth = new SystemHealth
                {
                    MemoryUsageMB = process.WorkingSet64 / 1024 / 1024,
                    CpuUsagePercent = await GetCpuUsageAsync(),
                    ActiveConnections = GetActiveConnectionCount(),
                    UptimeMinutes = (DateTime.UtcNow - process.StartTime).TotalMinutes
                };

                // Update current health status
                _currentHealth = health;

                // Log health status
                _logger.LogInformation("System health check: {OverallStatus} - DB: {DbStatus}, MQTT: {MqttStatus}, Memory: {MemoryMB}MB",
                    health.OverallStatus, health.DatabaseHealth.Status, health.MqttHealth.Status, health.SystemHealth.MemoryUsageMB);

                // Broadcast health update
                await _hubContext.Clients.Group("system_monitoring").SendAsync("SystemHealthUpdate", health);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing health check");
            }
        }

        private async Task ReportMetricsAsync()
        {
            try
            {
                var metrics = await GetPerformanceMetricsAsync();

                _logger.LogInformation("Performance Metrics Report - " +
                    "API Requests: {ApiRequests}, DB Queries: {DbQueries}, MQTT Messages: {MqttMessages}, " +
                    "SignalR Broadcasts: {SignalRBroadcasts}, Avg Response Time: {AvgResponseTime}ms",
                    _totalApiRequests, _totalDatabaseQueries, _totalMqttMessages, _totalSignalRBroadcasts,
                    await GetAverageResponseTime());

                // Broadcast metrics update
                await _hubContext.Clients.Group("system_monitoring").SendAsync("PerformanceMetricsUpdate", metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reporting metrics");
            }
        }

        private async Task UpdateSystemMetricsAsync()
        {
            try
            {
                var process = Process.GetCurrentProcess();

                UpdateMetric("system_memory_mb", process.WorkingSet64 / 1024 / 1024, MetricType.Gauge);
                UpdateMetric("active_connections", GetActiveConnectionCount(), MetricType.Gauge);
                UpdateMetric("cpu_usage_percent", await GetCpuUsageAsync(), MetricType.Gauge);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error updating system metrics");
            }
        }

        private async Task<double> GetCpuUsageAsync()
        {
            // Simple CPU usage calculation
            try
            {
                var process = Process.GetCurrentProcess();
                var startTime = DateTime.UtcNow;
                var startCpuUsage = process.TotalProcessorTime;

                await Task.Delay(100);

                var endTime = DateTime.UtcNow;
                var endCpuUsage = process.TotalProcessorTime;

                var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
                var totalMsPassed = (endTime - startTime).TotalMilliseconds;
                var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);

                return cpuUsageTotal * 100;
            }
            catch
            {
                return 0;
            }
        }

        private int GetActiveConnectionCount()
        {
            // This would need integration with SignalR hub to get actual connection count
            // For now, return a placeholder
            return 0;
        }

        private double GetMqttMessageRate()
        {
            var recentMessages = _recentMqttMessages
                .Where(m => m.Timestamp >= DateTime.UtcNow.AddMinutes(-5))
                .Count();

            return recentMessages / 5.0; // Messages per minute over last 5 minutes
        }

        private async Task<int> GetApiRequestsLastHour()
        {
            return _recentApiRequests
                .Where(r => r.Timestamp >= DateTime.UtcNow.AddHours(-1))
                .Count();
        }

        private async Task<int> GetDatabaseQueriesLastHour()
        {
            return _recentQueries
                .Where(q => q.Timestamp >= DateTime.UtcNow.AddHours(-1))
                .Count();
        }

        private async Task<int> GetMqttMessagesLastHour()
        {
            return _recentMqttMessages
                .Where(m => m.Timestamp >= DateTime.UtcNow.AddHours(-1))
                .Count();
        }

        private async Task<double> GetAverageResponseTime()
        {
            var recentRequests = _recentApiRequests
                .Where(r => r.Timestamp >= DateTime.UtcNow.AddMinutes(-5))
                .ToList();

            return recentRequests.Any() ? recentRequests.Average(r => r.ElapsedMs) : 0;
        }

        private async Task<double> GetErrorRate()
        {
            var recentRequests = _recentApiRequests
                .Where(r => r.Timestamp >= DateTime.UtcNow.AddMinutes(-5))
                .ToList();

            if (!recentRequests.Any()) return 0;

            var errorCount = recentRequests.Count(r => r.StatusCode >= 400);
            return (double)errorCount / recentRequests.Count * 100;
        }

        #endregion
    }

    #region Supporting Classes and Interfaces

    public interface IPerformanceMonitoringService
    {
        void RecordApiRequest(string endpoint, string method, int statusCode, long elapsedMs);
        void RecordDatabaseQuery(string query, long elapsedMs, bool isSuccessful = true);
        void RecordMqttMessage(string topic, bool isSuccessful = true, string? errorMessage = null);
        void RecordSignalRBroadcast(string messageType, int clientCount);
        Task<SystemHealthStatus> GetSystemHealthAsync();
        Task<Dictionary<string, object>> GetPerformanceMetricsAsync();
    }

    public class PerformanceMetric
    {
        public string Name { get; set; } = string.Empty;
        public double Value { get; set; }
        public MetricType Type { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class ApiRequestMetric
    {
        public string Endpoint { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public int StatusCode { get; set; }
        public long ElapsedMs { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class DatabaseQueryMetric
    {
        public string Query { get; set; } = string.Empty;
        public long ElapsedMs { get; set; }
        public bool IsSuccessful { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class MqttMessageMetric
    {
        public string Topic { get; set; } = string.Empty;
        public bool IsSuccessful { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class SystemHealthStatus
    {
        public DateTime Timestamp { get; set; }
        public string OverallStatus { get; set; } = "unknown";
        public DatabaseHealth DatabaseHealth { get; set; } = new();
        public MqttHealth MqttHealth { get; set; } = new();
        public SystemHealth SystemHealth { get; set; } = new();
    }

    public class DatabaseHealth
    {
        public bool IsConnected { get; set; }
        public long ResponseTimeMs { get; set; }
        public string Status { get; set; } = "unknown";
        public string? ErrorMessage { get; set; }
        public int SensorCount { get; set; }
        public int RecentAlarmCount { get; set; }
    }

    public class MqttHealth
    {
        public bool IsConnected { get; set; }
        public string Status { get; set; } = "unknown";
        public double MessageRate { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class SystemHealth
    {
        public long MemoryUsageMB { get; set; }
        public double CpuUsagePercent { get; set; }
        public int ActiveConnections { get; set; }
        public double UptimeMinutes { get; set; }
    }

    public enum MetricType
    {
        Counter,
        Gauge,
        Rate
    }

    #endregion
}