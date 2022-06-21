using System.Text;
using System.Text.Json;
using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.Configuration;

IConfiguration config = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .AddUserSecrets<Program>()
    .Build();

var iothubConnectionString = config["Device:ConnectionString"] ?? throw new InvalidOperationException("");

var deviceClient = DeviceClient.CreateFromConnectionString(iothubConnectionString);
await deviceClient.OpenAsync();
await deviceClient.SetMethodDefaultHandlerAsync(async (req, ctx) =>
{
    Console.WriteLine($"Method call: {req.Name}");
    return new MethodResponse(200);
}, null);
while (true)
{
    await Task.Delay(TimeSpan.FromSeconds(10));
    Console.WriteLine("Sending data");
    await deviceClient.SendEventAsync(GetDemoMessage());
}

Message GetDemoMessage() => new Message(Encoding.UTF8.GetBytes(GenerateDemoPayload()));
string GenerateDemoPayload() => JsonSerializer.Serialize(new { Content = Random.Shared.NextInt64() });