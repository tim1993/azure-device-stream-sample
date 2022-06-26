namespace IoTEmergency.ChuckisstanceModule
{
    using System;
    using System.IO;
    using System.Runtime.Loader;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Net.Http;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Client.Transport.Mqtt;
    using System.Text;

    class Program
    {
        const string ChuckNorrisApi = "https://api.chucknorris.io/jokes/random";
        static readonly HttpClient client = new HttpClient();
        static void Main(string[] args)
        {
            Init().Wait();

            // Wait until the app unloads or is cancelled
            var cts = new CancellationTokenSource();
            AssemblyLoadContext.Default.Unloading += (ctx) => cts.Cancel();
            Console.CancelKeyPress += (sender, cpe) => cts.Cancel();
            WhenCancelled(cts.Token).Wait();
        }

        /// <summary>
        /// Handles cleanup operations when app is cancelled or unloads
        /// </summary>
        public static Task WhenCancelled(CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            cancellationToken.Register(s => ((TaskCompletionSource<bool>?)s)?.SetResult(true), tcs);
            return tcs.Task;
        }

        /// <summary>
        /// Initializes the ModuleClient and sets up the callback to receive
        /// messages containing temperature information
        /// </summary>
        static async Task Init()
        {
            MqttTransportSettings mqttSetting = new MqttTransportSettings(TransportType.Mqtt_Tcp_Only);
            ITransportSettings[] settings = { mqttSetting };

            // Open a connection to the Edge runtime
            ModuleClient ioTHubModuleClient = await ModuleClient.CreateFromEnvironmentAsync(settings);
            await ioTHubModuleClient.OpenAsync();
            Console.WriteLine("IoT Hub module client initialized.");

            while (true)
            {
                var wisdome = await GetChucksWisdome();
                await SendChucksWisdome(wisdome, ioTHubModuleClient);
                await File.AppendAllTextAsync("wisdome.txt", $"\n{wisdome}");
                Console.WriteLine("Persisted wisdome.");
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }

        static async Task SendChucksWisdome(ChuckNorrisJoke wisdome, ModuleClient ioTHubModuleClient)
        {
            var message = new Message(Encoding.UTF8.GetBytes(wisdome.Value));

            await ioTHubModuleClient.SendEventAsync(message);
        }

        static async Task<ChuckNorrisJoke> GetChucksWisdome()
        {
            var response = await client.GetAsync("https://api.chucknorris.io/jokes/random");
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadAsStringAsync();
            var joke = await JsonSerializer.DeserializeAsync<ChuckNorrisJoke>(await response.Content.ReadAsStreamAsync(), new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
                                    ?? throw new InvalidOperationException();

            Console.WriteLine($"Parsed wisdome: {joke.Value}");

            return joke;
        }
    }
}

record ChuckNorrisJoke(string Value);