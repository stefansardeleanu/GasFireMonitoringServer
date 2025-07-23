// File: Hubs/MonitoringHub.cs
// SignalR hub for real-time communication with clients

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace GasFireMonitoringServer.Hubs
{
    // Hub is like a PLC communication interface - clients can connect and receive data
    public class MonitoringHub : Hub
    {
        private readonly ILogger<MonitoringHub> _logger;

        // Store which sites each client is monitoring
        private static readonly Dictionary<string, List<int>> _clientSubscriptions = new();

        public MonitoringHub(ILogger<MonitoringHub> logger)
        {
            _logger = logger;
        }

        // Called when a client connects
        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation($"Client connected: {Context.ConnectionId}");
            await base.OnConnectedAsync();
        }

        // Called when a client disconnects
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation($"Client disconnected: {Context.ConnectionId}");

            // Remove client subscriptions
            _clientSubscriptions.Remove(Context.ConnectionId);

            await base.OnDisconnectedAsync(exception);
        }

        // Client calls this to subscribe to specific sites
        public async Task SubscribeToSites(List<int> siteIds)
        {
            var connectionId = Context.ConnectionId;

            // Store subscription
            _clientSubscriptions[connectionId] = siteIds;

            // Add client to groups for each site
            foreach (var siteId in siteIds)
            {
                await Groups.AddToGroupAsync(connectionId, $"site_{siteId}");
            }

            _logger.LogInformation($"Client {connectionId} subscribed to sites: {string.Join(", ", siteIds)}");

            // Notify client of successful subscription
            await Clients.Caller.SendAsync("SubscriptionUpdated", siteIds);
        }

        // Client calls this to unsubscribe from sites
        public async Task UnsubscribeFromSites(List<int> siteIds)
        {
            var connectionId = Context.ConnectionId;

            // Remove from groups
            foreach (var siteId in siteIds)
            {
                await Groups.RemoveFromGroupAsync(connectionId, $"site_{siteId}");
            }

            // Update subscription list
            if (_clientSubscriptions.ContainsKey(connectionId))
            {
                _clientSubscriptions[connectionId].RemoveAll(id => siteIds.Contains(id));
            }

            _logger.LogInformation($"Client {connectionId} unsubscribed from sites: {string.Join(", ", siteIds)}");
        }

        // Server-side method to send sensor updates to subscribed clients
        public static async Task SendSensorUpdate(IHubContext<MonitoringHub> hubContext, int siteId, object sensorData)
        {
            // Send to all clients subscribed to this site
            await hubContext.Clients.Group($"site_{siteId}").SendAsync("SensorUpdate", sensorData);
        }

        // Server-side method to send alarm notifications
        public static async Task SendAlarmNotification(IHubContext<MonitoringHub> hubContext, int siteId, object alarmData)
        {
            // Send to all clients subscribed to this site
            await hubContext.Clients.Group($"site_{siteId}").SendAsync("NewAlarm", alarmData);
        }
    }
}