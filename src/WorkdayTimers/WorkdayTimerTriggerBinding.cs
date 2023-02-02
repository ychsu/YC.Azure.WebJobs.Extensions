using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Timers;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Microsoft.Extensions.Logging;

namespace YC.Azure.WebJobs.Extensions.WorkdayTimers
{
    public class WorkdayTimerTriggerBinding : ITriggerBinding
    {
        private readonly ParameterInfo _parameter;
        private readonly WorkdayTimerTriggerAttribute _attribute;
        private readonly TimerSchedule _schedule;
        private readonly WorkdayTimersOptions _options;
        private readonly ILogger _logger;
        private readonly ScheduleMonitor _scheduleMonitor;
        private readonly string _timerName;
        private readonly IReadOnlyDictionary<string, Type> _bindingContract;

        public WorkdayTimerTriggerBinding(ParameterInfo parameter,
            WorkdayTimerTriggerAttribute attribute,
            TimerSchedule schedule,
            WorkdayTimersOptions options,
            ILogger logger,
            ScheduleMonitor scheduleMonitor)
        {
            _parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
            _attribute = attribute ?? throw new ArgumentNullException(nameof(attribute));
            _schedule = schedule ?? throw new ArgumentNullException(nameof(schedule));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger;
            _scheduleMonitor = scheduleMonitor ?? throw new ArgumentNullException(nameof(scheduleMonitor));
            _bindingContract = CreateBindingDataContract();

            var methodInfo = (MethodInfo) parameter.Member;
            _timerName = $"{methodInfo.DeclaringType.FullName}.{methodInfo.Name}";
        }

        public async Task<ITriggerData> BindAsync(object value, ValueBindingContext context)
        {
            var timerInfo = value as TimerInfo;
            if (timerInfo == null)
            {
                ScheduleStatus status = null;
                if (_attribute.UseMonitor && _scheduleMonitor != null)
                {
                    status = await _scheduleMonitor.GetStatusAsync(_timerName);
                }

                timerInfo = new TimerInfo(_schedule, status);
            }

            IValueProvider valueProvider = new ValueProvider(timerInfo);
            IReadOnlyDictionary<string, object> bindingData = CreateBindingData();

            return new TriggerData(valueProvider, bindingData);
        }

        public Task<IListener> CreateListenerAsync(ListenerFactoryContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return Task.FromResult<IListener>(new WorkdayTimerListener(
                _attribute, 
                _schedule, 
                _timerName, 
                _options, 
                context.Executor, 
                _logger, 
                _scheduleMonitor, 
                context.Descriptor?.LogName));
        }

        public ParameterDescriptor ToParameterDescriptor()
        {
            return new ParameterDescriptor()
            {
                Name = _parameter.Name,
                DisplayHints = new ParameterDisplayHints()
                {
                    Description = $"Timer executed on schedule ({_schedule})"
                }
            };
        }

        private IReadOnlyDictionary<string, Type> CreateBindingDataContract()
        {
            var contract = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
            {
                {"WorkdayTimerTrigger", typeof(DateTime)}
            };

            return contract;
        }

        private IReadOnlyDictionary<string, object> CreateBindingData()
        {
            var bindingData = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                {"WorkdayTimerTrigger", DateTime.Now}
            };

            return bindingData;
        }

        public Type TriggerValueType => typeof(TimerInfo);


        public IReadOnlyDictionary<string, Type> BindingDataContract => _bindingContract;


        private class ValueProvider : IValueProvider
        {
            private readonly object _value;

            public ValueProvider(object value)
            {
                _value = value;
            }

            public Type Type => typeof(TimerInfo);

            public Task<object> GetValueAsync()
            {
                return Task.FromResult(_value);
            }

            public string ToInvokeString()
            {
                return DateTime.Now.ToString("o");
            }
        }
    }
}