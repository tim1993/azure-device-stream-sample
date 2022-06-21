using Microsoft.Azure.Devices.Client;
using System.Net.Sockets;
using System.Net.WebSockets;

namespace IoTEmergency.Remote.DeviceModule
{
    public class DeviceStreamHandler
    {
        private readonly DeviceClient _deviceClient;
        private readonly string _host;
        private readonly int _port;

        public DeviceStreamHandler(DeviceClient deviceClient, string host, int port)
        {
            _deviceClient = deviceClient;
            _host = host;
            _port = port;
        }

        private static async Task HandleIncomingDataAsync(NetworkStream localStream, ClientWebSocket remoteStream, CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[10240];

            while (remoteStream.State == WebSocketState.Open)
            {
                var receiveResult = await remoteStream.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);

                await localStream.WriteAsync(buffer, 0, receiveResult.Count).ConfigureAwait(false);
            }
        }

        private static async Task HandleOutgoingDataAsync(NetworkStream localStream, ClientWebSocket remoteStream, CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[10240];

            while (localStream.CanRead)
            {
                int receiveCount = await localStream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);

                await remoteStream.SendAsync(new ArraySegment<byte>(buffer, 0, receiveCount), WebSocketMessageType.Binary, true, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task WaitForConnection(CancellationToken cancellationTokenSource)
        {
            while (!cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    await WaitForConnectionInternal(cancellationTokenSource).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Got an exception: {0}", ex);
                }
                Console.WriteLine("Waiting again...");
            }
        }

        private async Task WaitForConnectionInternal(CancellationToken cancellationToken)
        {
            Console.WriteLine("Waiting for device stream request.");
            DeviceStreamRequest streamRequest = await _deviceClient.WaitForDeviceStreamRequestAsync(cancellationToken).ConfigureAwait(false);
            Console.WriteLine("Received device stream.");
            if (streamRequest != null)
            {
                await _deviceClient.AcceptDeviceStreamRequestAsync(streamRequest, cancellationToken).ConfigureAwait(false);

                using (ClientWebSocket webSocket = await GetStreamingClientAsync(streamRequest.Uri, streamRequest.AuthorizationToken, cancellationToken).ConfigureAwait(false))
                {
                    using (TcpClient tcpClient = new TcpClient())
                    {
                        await tcpClient.ConnectAsync(_host, _port).ConfigureAwait(false);

                        using (NetworkStream localStream = tcpClient.GetStream())
                        {
                            Console.WriteLine("Starting streaming");

                            await Task.WhenAny(
                                HandleIncomingDataAsync(localStream, webSocket, cancellationToken),
                                HandleOutgoingDataAsync(localStream, webSocket, cancellationToken)).ConfigureAwait(false);

                            localStream.Close();
                        }
                    }

                  //  await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, cancellationToken).ConfigureAwait(false);
                }

            }
        }

        public static async Task<ClientWebSocket> GetStreamingClientAsync(Uri url, string authorizationToken, CancellationToken cancellationToken)
        {
            ClientWebSocket wsClient = new ClientWebSocket();
            wsClient.Options.SetRequestHeader("Authorization", "Bearer " + authorizationToken);

            await wsClient.ConnectAsync(url, cancellationToken).ConfigureAwait(false);

            return wsClient;
        }
    }
}