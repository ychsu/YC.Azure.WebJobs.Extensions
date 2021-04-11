using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Description;

namespace YC.Azure.WebJobs.Extensions.NotificationHub
{
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    [Binding]
    public class NotificationAttribute : Attribute, IConnectionProvider
    {
        public string Connection { get; set; }

        public string HubName { get; set; }
    }
}