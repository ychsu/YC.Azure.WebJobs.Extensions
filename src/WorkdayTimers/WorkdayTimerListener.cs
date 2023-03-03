using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Timers;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Extensions.Logging;
using Timer = System.Timers.Timer;

namespace YC.Azure.WebJobs.Extensions.WorkdayTimers
{
    [Singleton(Mode = SingletonMode.Listener)]
    internal class WorkdayTimerListener : IListener
    {
        public const string UnscheduledInvocationReasonKey = "UnscheduledInvocationReason";
        public const string OriginalScheduleKey = "OriginalSchedule";
        private static readonly TimeSpan MaxTimerInterval = TimeSpan.FromDays(24);
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly ITriggeredFunctionExecutor _executor;
        private readonly string _functionLogName;
        private readonly SemaphoreSlim _invocationLock = new SemaphoreSlim(1, 1);
        private readonly ILogger _logger;
        private readonly WorkdayTimersOptions _options;
        private readonly TimerSchedule _schedule;
        private readonly string _timerLookupName;

        private bool _disposed;
        private TimeSpan _remainingInterval;
        private Timer _timer;

        public WorkdayTimerListener(WorkdayTimerTriggerAttribute attribute,
            TimerSchedule schedule,
            string timerName,
            WorkdayTimersOptions options,
            ITriggeredFunctionExecutor executor,
            ILogger logger,
            ScheduleMonitor scheduleMonitor,
            string functionLogName)
        {
            _timerLookupName = timerName;
            _options = options;
            _executor = executor;
            _logger = logger;
            _cancellationTokenSource = new CancellationTokenSource();
            _schedule = schedule;
            ScheduleMonitor = attribute.UseMonitor ? scheduleMonitor : null;
            _functionLogName = functionLogName;
        }

        internal ScheduleStatus ScheduleStatus { get; set; }

        internal ScheduleMonitor ScheduleMonitor { get; set; }

        /// <summary>
        ///     When set, we have a startup invocation that needs to happen immediately.
        /// </summary>
        internal StartupInvocationContext StartupInvocation { get; set; }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            if (_timer is {Enabled: true})
                throw new InvalidOperationException("The listener has already been started.");

            // if schedule monitoring is enabled, record (or initialize)
            // the current schedule status
            var isPastDue = false;

            // we use DateTime.Now rather than DateTime.UtcNow to allow the local machine to set the time zone. In Azure this will be
            // UTC by default, but can be configured to use any time zone if it makes scheduling easier.
            var now = DateTime.Now;

            if (ScheduleMonitor != null)
            {
                // check to see if we've missed an occurrence since we last started.
                // If we have, invoke it immediately.
                ScheduleStatus = await ScheduleMonitor.GetStatusAsync(_timerLookupName);
                var pastDueDuration =
                    await ScheduleMonitor.CheckPastDueAsync(_timerLookupName, now, _schedule, ScheduleStatus);
                isPastDue = pastDueDuration != TimeSpan.Zero;
            }

            if (ScheduleStatus == null)
                // no schedule status has been stored yet, so initialize
                ScheduleStatus = new ScheduleStatus
                {
                    Last = default,
                    Next = _schedule.GetNextOccurrence(now)
                };

            // 如果是立刻執行或上一次沒執行到, 就立刻執行
            if (isPastDue)
            {
                StartupInvocation = new StartupInvocationContext
                {
                    IsPastDue = true,
                    OriginalSchedule = ScheduleStatus.Next
                };
                StartTimer(StartupInvocation.Interval);
            }
            else
            {
                // start the regular schedule
                StartTimer(DateTime.Now);
            }

            _logger.LogDebug($"Timer listener started ({_functionLogName})");
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (_timer == null)
                throw new InvalidOperationException(
                    "The listener has not yet been started or has already been stopped.");

            _cancellationTokenSource.Cancel();

            _timer.Dispose();
            _timer = null;

            // wait for any outstanding invocation to complete
            await _invocationLock.WaitAsync();
            _invocationLock.Release();

            _logger.LogDebug($"Timer listener stopped ({_functionLogName})");
        }

        public void Cancel()
        {
            ThrowIfDisposed();
            _cancellationTokenSource.Cancel();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _cancellationTokenSource.Cancel();

                if (_timer != null)
                {
                    _timer.Dispose();
                    _timer = null;
                }

                _invocationLock.Dispose();

                _disposed = true;
            }
        }

        private void StartTimer(DateTime now)
        {
            var nextInterval = GetNextTimerInterval(ScheduleStatus.Next, now, _schedule.AdjustForDST);
            StartTimer(nextInterval);
        }

        /// <summary>
        ///     Calculate the next timer interval based on the current (Local) time.
        /// </summary>
        /// <remarks>
        ///     We calculate based on the current time because we don't know how long
        ///     the previous function invocation took. Example: if you have an hourly timer
        ///     invoked at 12:00 and the invocation takes 1 minute, we want to calculate
        ///     the interval for the next timer using 12:01 rather than at 12:00. Otherwise,
        ///     you'd start a 1-hour timer at 12:01 when we really want it to be a 59-minute timer.
        /// </remarks>
        /// <param name="next">The next schedule occurrence in Local time.</param>
        /// <param name="now">The current Local time.</param>
        /// <returns>The next timer interval.</returns>
        internal static TimeSpan GetNextTimerInterval(DateTime next, DateTime now, bool adjustForDST)
        {
            TimeSpan nextInterval;

            if (adjustForDST)
            {
                // For calculations, we use DateTimeOffsets and TimeZoneInfo to ensure we honor time zone
                // changes (e.g. Daylight Savings Time)
                var nowOffset = new DateTimeOffset(now, TimeZoneInfo.Local.GetUtcOffset(now));
                var nextOffset = new DateTimeOffset(next, TimeZoneInfo.Local.GetUtcOffset(next));
                nextInterval = nextOffset - nowOffset;
            }
            else
            {
                nextInterval = next - now;
            }

            // If the interval happens to be negative (due to slow storage, for example), adjust the
            // interval back up 1 Tick (Zero is invalid for a timer) for an immediate invocation.
            if (nextInterval <= TimeSpan.Zero) nextInterval = TimeSpan.FromTicks(1);

            return nextInterval;
        }

        private void StartTimer(TimeSpan interval)
        {
            // Restart the timer with the next schedule occurrence, but only 
            // if Cancel, Stop, and Dispose have not been called.
            if (_cancellationTokenSource.IsCancellationRequested) return;

            _timer = new Timer
            {
                AutoReset = false
            };
            _timer.Elapsed += OnTimer;

            if (interval > MaxTimerInterval)
            {
                // if the interval exceeds the maximum interval supported by Timer,
                // store the remainder and use the max
                _remainingInterval = interval - MaxTimerInterval;
                interval = MaxTimerInterval;
            }
            else
            {
                // clear out any remaining interval
                _remainingInterval = TimeSpan.Zero;
            }

            _timer.Interval = interval.TotalMilliseconds;
            _timer.Start();
        }

        private async void OnTimer(object sender, ElapsedEventArgs e)
        {
            await HandleTimerEvent();
        }

        internal async Task HandleTimerEvent()
        {
            var timerStarted = false;

            try
            {
                if (_remainingInterval != TimeSpan.Zero)
                {
                    // if we're in the middle of a long interval that exceeds
                    // Timer's max interval, continue the remaining interval w/o
                    // invoking the function
                    StartTimer(_remainingInterval);
                    timerStarted = true;
                    return;
                }

                // first check to see if we're dealing with an immediate startup invocation
                if (StartupInvocation != null)
                {
                    var startupInvocation = StartupInvocation;
                    StartupInvocation = null;

                    if (startupInvocation.IsPastDue)
                        // invocation is past due
                        await InvokeJobFunction(DateTime.Now,
                            true,
                            startupInvocation.OriginalSchedule);
                }
                else
                {
                    // this is a normal scheduled invocation
                    await InvokeJobFunction(DateTime.Now);
                }
            }
            catch (Exception ex)
            {
                // ensure background exceptions don't stop the execution schedule
                _logger.LogError(ex, "Error occurred during scheduled invocation for '{functionName}'.",
                    _functionLogName);
            }
            finally
            {
                if (!timerStarted) StartTimer(DateTime.Now);
            }
        }

        /// <summary>
        ///     Invokes the job function.
        /// </summary>
        /// <param name="invocationTime">The time of the invocation, likely DateTime.Now.</param>
        /// <param name="isPastDue">True if the invocation is because the invocation is due to a past due timer.</param>
        /// <param name="originalSchedule"></param>
        internal async Task InvokeJobFunction(DateTime invocationTime, bool isPastDue = false,
            DateTime? originalSchedule = null)
        {
            try
            {
                await _invocationLock.WaitAsync();

                // if Cancel, Stop, or Dispose have been called, skip the invocation
                // since we're stopping the listener
                if (_cancellationTokenSource.IsCancellationRequested) return;

                var token = _cancellationTokenSource.Token;

                var timerInfoStatus = ScheduleMonitor is null ? null : ScheduleStatus;

                var timerInfo = new TimerInfo(_schedule, timerInfoStatus, isPastDue);

                // Build up trigger details that will be logged if the timer is running at a different time 
                // than originally scheduled.
                var details = new Dictionary<string, string>();
                if (isPastDue) details[UnscheduledInvocationReasonKey] = "IsPastDue";

                if (originalSchedule.HasValue) details[OriginalScheduleKey] = originalSchedule.Value.ToString("o");

                var input = new TriggeredFunctionData
                {
                    TriggerValue = timerInfo,
                    TriggerDetails = details
                };

                try
                {
                    await _executor.TryExecuteAsync(input, token);
                }
                catch
                {
                    // We don't want any function errors to stop the execution
                    // schedule. Invocation errors are already logged.
                }

                // If the trigger fired before it was officially scheduled (likely under 1 second due to clock skew),
                // adjust the invocation time forward for the purposes of calculating the next occurrence.
                // Without this, it's possible to set the 'Next' value to the same time twice in a row, 
                // which results in duplicate triggers if the site restarts.
                var adjustedInvocationTime = invocationTime;
                if (!isPastDue && ScheduleStatus?.Next > invocationTime) adjustedInvocationTime = ScheduleStatus.Next;

                // Create the Last value with the adjustedInvocationTime; otherwise, the listener will
                // consider this a schedule change when the host next starts.
                ScheduleStatus = new ScheduleStatus
                {
                    Last = adjustedInvocationTime,
                    Next = _schedule.GetNextOccurrence(adjustedInvocationTime),
                    LastUpdated = adjustedInvocationTime
                };

                if (ScheduleMonitor != null)
                {
                    await ScheduleMonitor.UpdateStatusAsync(_timerLookupName, ScheduleStatus);
                    _logger.LogDebug(
                        $"Function '{_functionLogName}' updated status: Last='{ScheduleStatus.Last:o}', Next='{ScheduleStatus.Next:o}', LastUpdated='{ScheduleStatus.LastUpdated:o}'");
                }
            }
            finally
            {
                _invocationLock.Release();
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(null);
        }

        internal class StartupInvocationContext
        {
            // for immediate startup invocations we use the smallest non-zero interval
            // possible (timer intervals must be non-zero)
            public const int IntervalMs = 1;

            public bool IsPastDue { get; set; }

            public DateTime OriginalSchedule { get; set; }

            public TimeSpan Interval => TimeSpan.FromMilliseconds(IntervalMs);
        }
    }
}