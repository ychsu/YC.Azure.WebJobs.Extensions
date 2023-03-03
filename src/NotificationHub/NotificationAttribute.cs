using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Description;

namespace YC.Azure.WebJobs.Extensions.NotificationHub
{
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    [Binding]
    public class NotificationAttribute : Attribute, IConnectionProvider
    {
        public NotificationAttribute()
        {
        }

        public NotificationAttribute(bool isSendWhenAdd)
        {
            IsSendWhenAdd = isSendWhenAdd;
        }

        public string HubName { get; set; }

        /// <summary>
        ///     get or set is send notification to azure notification hub when add
        /// </summary>
        public bool? IsSendWhenAdd { get; set; }

        public string Connection { get; set; }
    }
}