// File: Services/Interfaces/IMqttService.cs
// An interface defines the "contract" - what methods must exist

namespace GasFireMonitoringServer.Services.Interfaces
{
    public interface IMqttService
    {
        //Method signatures - no implementation here
        //Task means the metot runs asynchroounously (doesn't lbock)

        Task ConnectAsync();    //Connect to MQTT broker
        Task DisconnectAsync(); //Disconnect from broker
        bool IsConnected { get; } //Property to check connection status

        //Events - like PLC interrupts
        //These fire when something happens
       
        event EventHandler<string>MessageReceived; // fires when MQTT message arrives
        event EventHandler<bool> ConnectionChanged; //fires when conn status changes

    }
}
