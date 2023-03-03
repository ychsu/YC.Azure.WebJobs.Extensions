using System;
using Microsoft.Azure.WebJobs.Description;

namespace YC.Azure.WebJobs.Extensions.WorkdayTimers
{
    [AttributeUsage(AttributeTargets.Parameter)]
    [Binding]
    public class WorkdayTimerTriggerAttribute : Attribute
    {
        public WorkdayTimerTriggerAttribute(string scheduleExpression)
        {
            ScheduleExpression = scheduleExpression;
            UseMonitor = true;
        }

        /// <summary>
        ///     Gets the schedule expression.
        /// </summary>
        public string ScheduleExpression { get; }

        /// <summary>
        ///     Gets or sets a value indicating whether the schedule should be monitored.
        ///     Schedule monitoring persists schedule occurrences to aid in ensuring the
        ///     schedule is maintained correctly even when roles restart.
        ///     If not set explicitly, this will default to true for schedules that have a recurrence
        ///     interval greater than 1 minute (i.e., for schedules that occur more than once
        ///     per minute, persistence will be disabled).
        /// </summary>
        public bool UseMonitor { get; set; }
    }
}