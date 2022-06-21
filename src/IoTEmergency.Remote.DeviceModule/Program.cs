namespace IoTEmergency.Remote.DeviceModule
{
    using System;
    using System.Collections;
    using System.Runtime.Loader;
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
            PrintEnvironment();
            var protocolsettings = new AmqpTransportSettings(TransportType.Amqp_Tcp_Only);
            ITransportSettings[] settings = { protocolsettings };

            // Open a connection to the Edge runtime
            ModuleClient ioTHubModuleClient = await ModuleClient.CreateFromEnvironmentAsync(settings);

            await ioTHubModuleClient.OpenAsync();
            var deviceStreamHander = new DeviceStreamHandler(ioTHubModuleClient, "localhost", 22);
            await deviceStreamHander.WaitForConnection(CancellationToken.None);
            Console.WriteLine("IoT Hub module client initialized.");
        }

        static void PrintEnvironment()
        {
            var envVars = Environment.GetEnvironmentVariables();
            foreach (DictionaryEntry env in envVars)
            {
                Console.WriteLine($"{env.Key}: {env.Value}");
            }
        }
    }
}
