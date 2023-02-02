using System;

namespace YC.Azure.WebJobs.Extensions.WorkdayTimers
{
    public interface IWorkdayFilter
    {
        bool IsWorkday(DateTime date);
    }
}