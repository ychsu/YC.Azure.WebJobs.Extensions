using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Timers;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace YC.Azure.WebJobs.Extensions.WorkdayTimers
{
    internal class WorkdayTimersExtensionConfigProvider : IExtensionConfigProvider
    {
        private readonly IOptions<WorkdayTimersOptions> _options;
        private readonly IWorkdayFilter _workdayFilter;
        private readonly ILoggerFactory _loggerFactory;
        private readonly INameResolver _nameResolver;
        private readonly ScheduleMonitor _scheduleMonitor;

        public WorkdayTimersExtensionConfigProvider(IOptions<WorkdayTimersOptions> options,
            IWorkdayFilter workdayFilter,
            ILoggerFactory loggerFactory,
            INameResolver nameResolver,
            ScheduleMonitor scheduleMonitor)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _workdayFilter = workdayFilter ?? throw new ArgumentNullException(nameof(workdayFilter));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _nameResolver = nameResolver ?? throw new ArgumentNullException(nameof(nameResolver));
            _scheduleMonitor = scheduleMonitor ?? throw new ArgumentNullException(nameof(scheduleMonitor));
        }

        public void Initialize(ExtensionConfigContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            var trigger = new WorkdayTimerTriggerAttributeBindingProvider(_options.Value,
                _workdayFilter,
                _nameResolver,
                _loggerFactory.CreateLogger(nameof(WorkdayTimers)),
                _scheduleMonitor);

            context.AddBindingRule<WorkdayTimerTriggerAttribute>()
                .BindToTrigger(trigger);
        }
    }
}