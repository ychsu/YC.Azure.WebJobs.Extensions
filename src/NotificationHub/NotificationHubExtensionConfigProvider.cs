using System;
using Microsoft.Azure.NotificationHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Extensions.Configuration;

namespace YC.Azure.WebJobs.Extensions.NotificationHub
{
    internal class NotificationHubExtensionConfigProvider : IExtensionConfigProvider
    {
        private readonly IConfiguration _configuration;

        public NotificationHubExtensionConfigProvider(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public void Initialize(ExtensionConfigContext context)
        {
            var binding = context.AddBindingRule<NotificationAttribute>();

            binding.BindToCollector(BuildFromAttribute);
        }

        private IAsyncCollector<NotificationMessage> BuildFromAttribute(NotificationAttribute attr)
        {
            var connectionString = _configuration.GetWebJobsConnectionString(attr.Connection ?? "NotificationHub");
            var hubName = attr.HubName ?? _configuration.GetValue<string>("NotificationHubName");
            var client = NotificationHubClient.CreateClientFromConnectionString(
                connectionString, hubName);
            return new NotificationWriter(client);
        }
    }
}