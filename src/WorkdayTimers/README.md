## YC.Azure.WebJobs.Extensions.WorkdayTimers
### Installation
Nuget
```
Install-Package YC.Azure.WebJobs.Extensions.WorkdayTimers
```

dotnet cli
```
dotnet add package YC.Azure.WebJobs.Extensions.WorkdayTimers
```

### Using the binding

#### Configure
```csharp
var builder = new HostBuilder();
builder
  .ConfigureWebJobs(b =>
  {
    b.AddWorkdayTimers();
    // b.AddWorkdayTimers<CustomWorkdayFilter>();
  })
  .ConfigureLogging((context, b) => { });
var host = builder.Build();
using (host)
{
  await host.RunAsync();
}

internal CustomWorkdayFilter : WorkdayFilter
{
    public override bool IsWorkday(DateTime date)
    {
        return base.IsWorkday(date) && 
            date.DayOfWeek != DayOfWeek.Friday;
    }
}
```

#### samples
```csharp
public void Test([WorkdayTimerTrigger("0 0 12 * * *")] TimerInfo info)
{
    Console.WriteLine(timerInfo.FormatNextOccurrences(10));
}
```