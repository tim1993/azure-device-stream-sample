// See https://aka.ms/new-console-template for more information
using IoTEmergency.Remote.DeviceModule;
using Microsoft.Azure.Devices.Client;

string deviceConnectionString = File.ReadAllText(".key");

Console.WriteLine("Starting raw DeviceStream");

var deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString);
await deviceClient.OpenAsync();
Console.WriteLine("Connected.");
var streamHandler = new DeviceStreamHandler(deviceClient, "127.0.0.1", 22);
await streamHandler.WaitForConnection(CancellationToken.None);
Console.WriteLine("Got request.");