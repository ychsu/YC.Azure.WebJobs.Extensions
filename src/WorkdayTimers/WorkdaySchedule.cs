using System;
using Microsoft.Azure.WebJobs.Extensions.Timers;

namespace YC.Azure.WebJobs.Extensions.WorkdayTimers
{
    public class WorkdaySchedule : TimerSchedule
    {
        private readonly IWorkdayFilter _workdayFilter;
        private readonly TimerSchedule _innerSchedule;

        public WorkdaySchedule(IWorkdayFilter workdayFilter, TimerSchedule innerSchedule)
        {
            _workdayFilter = workdayFilter ?? throw new ArgumentNullException(nameof(workdayFilter));
            _innerSchedule = innerSchedule ?? throw new ArgumentNullException(nameof(innerSchedule));
        }

        public override DateTime GetNextOccurrence(DateTime now)
        {
            var next = now;

            while (!_workdayFilter.IsWorkday(next = _innerSchedule.GetNextOccurrence(next)))
            {
            }

            return next;
        }

        public override bool AdjustForDST => _innerSchedule.AdjustForDST;
    }
}