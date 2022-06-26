using Microsoft.Azure.Devices;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;

namespace IoTEmergency.Web.Data
{
    public class DeviceStreamProxyService
    {
        private readonly ServiceClient deviceClient;
        private Task? proxyTask;
        private CancellationTokenSource cts = new CancellationTokenSource();

        public bool IsRunning => proxyTask is not null;
        public DeviceStreamProxyService(ServiceClient deviceClient)
        {
            this.deviceClient = deviceClient;
        }

        private static async Task HandleIncomingDataAsync(NetworkStream localStream, ClientWebSocket remoteStream, CancellationToken cancellationToken)
        {
            byte[] receiveBuffer = new byte[10240];

            while (localStream.CanRead)
            {
                var receiveResult = await remoteStream.ReceiveAsync(receiveBuffer, cancellationToken).ConfigureAwait(false);

                await localStream.WriteAsync(receiveBuffer, 0, receiveResult.Count).ConfigureAwait(false);
            }
        }

        private static async Task HandleOutgoingDataAsync(NetworkStream localStream, ClientWebSocket remoteStream, CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[10240];

            while (remoteStream.State == WebSocketState.Open)
            {
                int receiveCount = await localStream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);

                await remoteStream.SendAsync(new ArraySegment<byte>(buffer, 0, receiveCount), WebSocketMessageType.Binary, true, cancellationToken).ConfigureAwait(false);
            }
        }

        private static async Task HandleIncomingConnectionsAndCreateStreams(string deviceId, ServiceClient serviceClient, NetworkStream tcpClient)
        {
            DeviceStreamRequest deviceStreamRequest = new DeviceStreamRequest(
                streamName: "TestStream"
            );


            DeviceStreamResponse result = await serviceClient.CreateStreamAsync(deviceId, deviceStreamRequest, CancellationToken.None).ConfigureAwait(false);

            Console.WriteLine($"Stream response received: Name={deviceStreamRequest.StreamName} IsAccepted={result.IsAccepted}");

            if (result.IsAccepted)
            {
                try
                {
                    var cancellationTokenSource = new CancellationTokenSource();
                    using (var remoteStream = await GetStreamingClientAsync(result.Uri, result.AuthorizationToken, cancellationTokenSource.Token).ConfigureAwait(false))
                    {
                        Console.WriteLine("Starting streaming");

                        await Task.WhenAny(
                            HandleIncomingDataAsync(tcpClient, remoteStream, cancellationTokenSource.Token),
                            HandleOutgoingDataAsync(tcpClient, remoteStream, cancellationTokenSource.Token)).ConfigureAwait(false);
                    }

                    Console.WriteLine("Done streaming");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Got an exception: {0}", ex);
                }
            }
            tcpClient.Close();
        }

        private static async Task<ClientWebSocket> GetStreamingClientAsync(Uri url, string authorizationToken, CancellationToken cancellationToken)
        {
            ClientWebSocket wsClient = new ClientWebSocket();
            wsClient.Options.SetRequestHeader("Authorization", "Bearer " + authorizationToken);

            await wsClient.ConnectAsync(url, cancellationToken).ConfigureAwait(false);

            return wsClient;
        }

        public async Task OpenLocalProxy(int localPort, string deviceId)
        {
            if (proxyTask is not null)
            {
                cts.Cancel();
                await proxyTask.WaitAsync(TimeSpan.FromSeconds(10));
                proxyTask.Dispose();

                cts = new CancellationTokenSource();
            }
            proxyTask = Task.Run(async () =>
            {
                var tcpListener = new TcpListener(IPAddress.Any, localPort);
                tcpListener.Start();

                while (!cts.IsCancellationRequested)
                {
                    using var tcpClient = await tcpListener.AcceptSocketAsync().ConfigureAwait(false);
                    using var networkStream = new NetworkStream(tcpClient);
                    await HandleIncomingConnectionsAndCreateStreams(deviceId, deviceClient, networkStream);
                }

                tcpListener.Stop();
            });

        }
    }
}
