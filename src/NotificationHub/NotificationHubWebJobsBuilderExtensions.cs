using Microsoft.Azure.WebJobs;

namespace YC.Azure.WebJobs.Extensions.NotificationHub
{
    public static class NotificationHubWebJobsBuilderExtensions
    {
        public static IWebJobsBuilder AddNotificationHub(this IWebJobsBuilder builder)
        {
            builder.AddExtension<NotificationHubExtensionConfigProvider>();

            return builder;
        }
    }
}