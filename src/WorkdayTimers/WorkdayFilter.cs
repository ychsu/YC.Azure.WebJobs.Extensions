using System;
using System.Linq;

namespace YC.Azure.WebJobs.Extensions.WorkdayTimers
{
    public class WorkdayFilter : IWorkdayFilter
    {
        public virtual bool IsWorkday(DateTime date)
        {
            return !new[] {DayOfWeek.Saturday, DayOfWeek.Sunday}.Contains(date.DayOfWeek);
        }
    }
}