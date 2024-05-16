using System;
using System.Threading.Tasks;
using Microsoft.Azure.NotificationHubs;
using Microsoft.Azure.WebJobs;
using YC.Azure.WebJobs.Extensions.NotificationHub;
using YC.Azure.WebJobs.Extensions.WorkdayTimers;

namespace WebJobSample;

public class Function
{
    [NoAutomaticTrigger]
    [return: Notification]
    public NotificationMessage SendNotification([TimerTrigger("*/15 * * * * *")] TimerInfo timerInfo)
    {
        return new NotificationMessage
        {
            Notification = new AppleNotification(@"{ ""aps"": { ""alert"": ""test"", ""badge"": 90 } }"),
            TagExpression = "user_3"
        };
    }

    /// <summary>
    ///     將會取ConnectionStrings中 TestConn連線字串, 及使用Hub為Test
    /// </summary>
    /// <param name="timerInfo"></param>
    /// <returns></returns>
    [NoAutomaticTrigger]
    [return: Notification(Connection = "TestConn", HubName = "Test")]
    public NotificationMessage SendNotification2([TimerTrigger("*/15 * * * * *")] TimerInfo timerInfo)
    {
        return new NotificationMessage
        {
            Notification = new AppleNotification(@"{ ""aps"": { ""alert"": ""test"", ""badge"": 90 } }"),
            TagExpression = "user_3"
        };
    }

    /// <summary>
    ///     當 結束時 collection flush, 一次性發送通知
    /// </summary>
    /// <param name="timerInfo"></param>
    /// <param name="messages"></param>
    /// <returns></returns>
    [NoAutomaticTrigger]
    public async Task SendNotification3([TimerTrigger("*/15 * * * * *")] TimerInfo timerInfo,
        [Notification] IAsyncCollector<NotificationMessage> messages)
    {
        await messages.AddAsync(new NotificationMessage
        {
            Notification = new AppleNotification(@"{ ""aps"": { ""alert"": ""test"" } }"),
            TagExpression = "zh"
        });
        await messages.AddAsync(new NotificationMessage
        {
            Notification = new AppleNotification(@"{ ""aps"": { ""alert"": ""test"" } }"),
            TagExpression = "en"
        });
    }

    /// <summary>
    ///     當 加到 collection 時, 立即發送
    /// </summary>
    /// <param name="timerInfo"></param>
    /// <param name="messages"></param>
    /// <returns></returns>
    [NoAutomaticTrigger]
    public async Task SendNotification4([TimerTrigger("*/15 * * * * *")] TimerInfo timerInfo,
        [Notification(true)] IAsyncCollector<NotificationMessage> messages)
    {
        await messages.AddAsync(new NotificationMessage
        {
            Notification = new AppleNotification(@"{ ""aps"": { ""alert"": ""test"" } }"),
            TagExpression = "zh"
        });
        await messages.AddAsync(new NotificationMessage
        {
            Notification = new AppleNotification(@"{ ""aps"": { ""alert"": ""test"" } }"),
            TagExpression = "en"
        });
    }

    public void WorkdayTriggered([WorkdayTimerTrigger("0 16 13 * * *")] TimerInfo timerInfo)
    {
        Console.WriteLine(timerInfo.FormatNextOccurrences(14));
    }
}