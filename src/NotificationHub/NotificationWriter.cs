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
        private readonly bool _isSendWhenAdd;
        private readonly ConcurrentQueue<NotificationMessage> _queue;

        public NotificationWriter(INotificationHubClient hubClient, bool isSendWhenAdd)
        {
            _hubClient = hubClient ?? throw new ArgumentNullException(nameof(hubClient));
            _isSendWhenAdd = isSendWhenAdd;
            _queue = new ConcurrentQueue<NotificationMessage>();
        }

        public async Task AddAsync(NotificationMessage item,
            CancellationToken cancellationToken = new CancellationToken())
        {
            if (_isSendWhenAdd)
                await SendNotificationAsync(item, cancellationToken);
            else
                _queue.Enqueue(item);
        }

        public async Task FlushAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            while (_queue.TryDequeue(out var message)) await SendNotificationAsync(message, cancellationToken);
        }

        private async Task SendNotificationAsync(NotificationMessage message, CancellationToken cancellationToken)
        {
            try
            {
                await _hubClient.SendNotificationAsync(message.Notification, message.TagExpression, cancellationToken);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}