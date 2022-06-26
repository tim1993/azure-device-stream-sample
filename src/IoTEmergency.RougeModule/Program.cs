namespace IoTEmergency.RougeModule
{
    using System;
    using System.IO;
    using System.Runtime.Loader;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Client.Transport.Mqtt;

    class Program
    {
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
            await ioTHubModuleClient.SetMethodHandlerAsync("FloodDisk", HandleFloodDiskMethod, new object());
            Console.WriteLine("IoT Hub module client initialized.");


        }

        static Task<MethodResponse> HandleFloodDiskMethod(MethodRequest req, object _)
        {
            try
            {
                Console.WriteLine("Flooding disk started.");
                Console.WriteLine(req.DataAsJson);
                var payload = JsonSerializer.Deserialize<FloodDiskArgs>(req.DataAsJson)
                        ?? throw new ArgumentException("Could not deserialize payload.");

                Console.WriteLine($"Writing {payload.size} mb...");
                // HeavyCalc();
                WriteDummyFile(payload.size);

                Console.WriteLine("Done.");

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return Task.FromResult(new MethodResponse(200));
        }

        static void WriteDummyFile(int sizeInMb)
        {
            byte[] data = new byte[8192];
            Random rng = new Random();
            using FileStream stream = File.OpenWrite($"huge_dummy_file_{rng.NextInt64()}");
            for (int i = 0; i < sizeInMb * 128; i++)
            {
                rng.NextBytes(data);
                stream.Write(data, 0, data.Length);
                // Console.WriteLine($"Writing {i} / {sizeInMb * 128}");
            }

            stream.Flush();
        }

        static void HeavyCalc()
        {

            Console.WriteLine(Environment.ProcessorCount);

            for (int i = 0; i < Environment.ProcessorCount * 100; i++)
            {
                var t = new Thread((_) =>
                {
                    while (true)
                    {
                        Console.WriteLine("HeavyCalc");
                        Random rnd = new Random();
                        rnd.NextBytes(new byte[64909]);
                    }
                });

                t.Start();

            }
        }
    }
}

record FloodDiskArgs(int size);