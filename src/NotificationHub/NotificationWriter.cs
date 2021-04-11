using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.NotificationHubs;
using Microsoft.Azure.WebJobs;

namespace YC.Azure.WebJobs.Extensions.NotificationHub
{
    internal class NotificationWriter : IAsyncCollector<NotificationMessage>
    {
        private readonly INotificationHubClient _hubClient;
        private readonly ConcurrentQueue<NotificationMessage> _queue;

        public NotificationWriter(INotificationHubClient hubClient)
        {
            _hubClient = hubClient ?? throw new ArgumentNullException(nameof(hubClient));
            _queue = new ConcurrentQueue<NotificationMessage>();
        }

        public Task AddAsync(NotificationMessage item, CancellationToken cancellationToken = new CancellationToken())
        {
            _queue.Enqueue(item);
            return Task.CompletedTask;
        }

        public async Task FlushAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            while (_queue.TryDequeue(out var message))
            {
                Notification msg = message.Platform switch
                {
                    Platform.Fcm => new FcmNotification(message.Payload),
                    Platform.Apple => new AppleNotification(message.Payload),
                    _ => throw new ArgumentOutOfRangeException()
                };
                try
                {
                    await _hubClient.SendNotificationAsync(msg, message.TagExpression, cancellationToken);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }
    }
}