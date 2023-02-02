using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.DependencyInjection;

namespace YC.Azure.WebJobs.Extensions.WorkdayTimers
{
    public static class WorkdayTimersWebJobsBuilderExtensions
    {
        public static IWebJobsBuilder AddWorkdaysTimers<TWorkdayFilter>(this IWebJobsBuilder builder)
            where TWorkdayFilter : class, IWorkdayFilter
        {
            builder.AddExtension<WorkdayTimersExtensionConfigProvider>();

            builder.Services.AddTransient<IWorkdayFilter, TWorkdayFilter>();

            return builder;
        }

        public static IWebJobsBuilder AddWorkdaysTimers(this IWebJobsBuilder builder)
        {
            return AddWorkdaysTimers<WorkdayFilter>(builder);
        }
    }
}