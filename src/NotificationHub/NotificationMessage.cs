using Microsoft.Azure.NotificationHubs;

namespace YC.Azure.WebJobs.Extensions.NotificationHub
{
    public class NotificationMessage
    {
        public Notification Notification { get; set; }
        public string TagExpression { get; set; }
    }
}