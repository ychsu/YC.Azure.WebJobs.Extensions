using System;
using YC.Azure.WebJobs.Extensions.WorkdayTimers;

namespace WebJobSample;

public class MyWorkdayFilter : WorkdayFilter
{
    public override bool IsWorkday(DateTime date)
    {
        var result = base.IsWorkday(date);
        return result && date.DayOfWeek != DayOfWeek.Friday;
    }
}