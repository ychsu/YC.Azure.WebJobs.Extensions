using Microsoft.Azure.WebJobs;
using YC.Azure.WebJobs.Extensions.NotificationHub;

namespace WebJobSample
{
    public class Function
    {
        [NoAutomaticTrigger]
        [return: Notification]
        public NotificationMessage SendNotification([TimerTrigger("*/15 * * * * *")] TimerInfo timerInfo)
        {
            return new()
            {
                Platform = Platform.Apple,
                Payload = @"{ ""aps"": { ""alert"": ""test"", ""badge"": 90 } }",
                TagExpression = "user_3"
            };
        }
    }
}