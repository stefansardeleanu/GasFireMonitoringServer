// File: Configuration / MqttSettings.cs
// This class holds MQTT connection settings

namespace GasFireMonitoringServer.Configuration
{
    public class MqttSettings
    {
        //Connection details
        public string BrokerAddress { get; set; }
        public int Port { get; set; }
        public string Username  { get; set; }
        public string Password { get; set; }

        //Client identification
        public string ClientId { get; set; }

        //Topics to subscribe
        public string TopicPattern { get; set; } //eg /PLCNEXT/+/+


    }
}
