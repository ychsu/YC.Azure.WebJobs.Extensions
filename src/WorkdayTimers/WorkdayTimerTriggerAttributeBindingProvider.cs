using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Timers;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Microsoft.Extensions.Logging;
using NCrontab;

namespace YC.Azure.WebJobs.Extensions.WorkdayTimers
{
    internal class WorkdayTimerTriggerAttributeBindingProvider : ITriggerBindingProvider
    {
        private readonly ILogger _logger;
        private readonly INameResolver _nameResolver;
        private readonly WorkdayTimersOptions _options;
        private readonly ScheduleMonitor _scheduleMonitor;
        private readonly IWorkdayFilter _workdayFilter;

        public WorkdayTimerTriggerAttributeBindingProvider(WorkdayTimersOptions options,
            IWorkdayFilter workdayFilter,
            INameResolver nameResolver,
            ILogger logger,
            ScheduleMonitor scheduleMonitor)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _workdayFilter = workdayFilter ?? throw new ArgumentNullException(nameof(workdayFilter));
            _nameResolver = nameResolver ?? throw new ArgumentNullException(nameof(nameResolver));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _scheduleMonitor = scheduleMonitor ?? throw new ArgumentNullException(nameof(scheduleMonitor));
        }

        public Task<ITriggerBinding> TryCreateAsync(TriggerBindingProviderContext context)
        {
            var parameter = context.Parameter;
            var attribute = parameter.GetCustomAttribute<WorkdayTimerTriggerAttribute>(false);
            if (attribute is null) return Task.FromResult<ITriggerBinding>(null);

            if (parameter.ParameterType != typeof(TimerInfo))
                throw new InvalidOperationException(
                    $"Can't bind WorkdayTimerTriggerAttribute to type '{parameter.ParameterType}'.");

            var schedule = CreateSchedule(attribute);

            if (schedule != null)
                schedule = new WorkdaySchedule(_workdayFilter, schedule);

            return Task.FromResult<ITriggerBinding>(
                new WorkdayTimerTriggerBinding(parameter,
                    attribute,
                    schedule,
                    _options,
                    _logger,
                    _scheduleMonitor));
        }

        private TimerSchedule CreateSchedule(WorkdayTimerTriggerAttribute attribute)
        {
            var resolvedExpression = _nameResolver.ResolveWholeString(attribute.ScheduleExpression);

            var schedule = default(TimerSchedule);
            if (TryParseCronSchedule(resolvedExpression, ref schedule, out var crontabSchedule))
            {
                if (attribute.UseMonitor && ShouldDisableScheduleMonitor(crontabSchedule, DateTime.Now))
                    attribute.UseMonitor = false;
            }
            else if (TryParseConstantSchedule(resolvedExpression, ref schedule, out var periodTimespan))
            {
                if (attribute.UseMonitor && periodTimespan.TotalMinutes < 1) attribute.UseMonitor = false;
            }

            return schedule;
        }

        private bool ShouldDisableScheduleMonitor(CrontabSchedule crontabSchedule, DateTime now)
        {
            // take the original expression minus the seconds portion
            var expression = crontabSchedule.ToString();
            var expressions = expression.Split(' ');

            // If any of the minute or higher fields contain non-wildcard expressions
            // the schedule can be longer than 1 minute. I.e. the only way for all occurrences
            // to be less than or equal to a minute is if all these fields are wild ("* * * * *").
            var hasNonSecondRestrictions = expressions.Skip(1).Any(p => p != "*");

            if (hasNonSecondRestrictions) return false;

            // If to here, we know we're dealing with a schedule of the form X * * * * *
            // so we just need to consider the seconds expression to determine if it occurs
            // more frequently than 1 minute.
            // E.g. an expression like */10 * * * * * occurs every 10 seconds, while an
            // expression like 0 * * * * * occurs exactly once per minute.
            var nextOccurrences = crontabSchedule
                .GetNextOccurrences(now, now + TimeSpan.FromMinutes(1)).ToArray();

            return nextOccurrences.Length > 1;
        }

        private bool TryParseConstantSchedule(string resolvedExpression,
            ref TimerSchedule schedule,
            out TimeSpan span)
        {
            if (TimeSpan.TryParse(resolvedExpression, out span) == false) return false;
            schedule = new ConstantSchedule(span);
            return true;
        }

        private bool TryParseCronSchedule(string cronExpression,
            ref TimerSchedule schedule,
            out CrontabSchedule crontabSchedule)
        {
            var options = new CrontabSchedule.ParseOptions
            {
                IncludingSeconds = cronExpression.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length != 5
            };

            crontabSchedule = CrontabSchedule.TryParse(cronExpression, options);
            if (crontabSchedule is null) return false;
            schedule = new CronSchedule(crontabSchedule);
            return true;
        }
    }
}