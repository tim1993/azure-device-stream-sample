using Azure.Messaging.EventHubs.Consumer;
using System.Text;

namespace IoTEmergency.Web.Data
{
    public class IoTHubReceiverService : IHostedService
    {

        public delegate void JokesReceivedEventHandler(string joke);
        public event JokesReceivedEventHandler? JokesReceived;
        private readonly TimeSpan Delay = TimeSpan.FromSeconds(10);
        private EventHubConsumerClient _eventHubClient;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private ILogger _logger;
        private Task? _task;


        public IoTHubReceiverService(EventHubConsumerClient eventHubClient, ILogger<IoTHubReceiverService> logger)
        {
            _eventHubClient = eventHubClient;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting ReceiverService");
            _task = Task.Run(async () =>
             {
                 while (!_cts.IsCancellationRequested)
                 {
                     await foreach (var receivedEvent in _eventHubClient.ReadEventsAsync(_cts.Token))
                     {
                         var receivedJokes = DeserializeMessage(receivedEvent);
                         if (receivedJokes is not null)
                         {
                             _logger.LogInformation("Notifying subscribers");
                             if (JokesReceived is not null)
                             {
                                 JokesReceived!(receivedJokes!);
                             }

                         }
                     }
                 }
             });

            return Task.CompletedTask;
        }

        private string? DeserializeMessage(PartitionEvent message)
        {
            var decodedMessage = Encoding.UTF8.GetString(message.Data.EventBody.ToArray());

            return decodedMessage;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _cts.Cancel();
            if (_task is not null)
            {
                await _task;
            }
            _cts = new CancellationTokenSource();
        }

    }

    public record ChuckNorrisJoke(string Value);
}