## YC.Azure.WebJobs.Extensions.NotificationHub

### Installation

Nuget

```
Install-Package YC.Azure.WebJobs.Extensions.NotificationHub
```

dotnet cli

```
dotnet add package YC.Azure.WebJobs.Extensions.NotificationHub
```

### Using the binding

#### Configure

```csharp
var builder = new HostBuilder();
builder
  .ConfigureWebJobs(b =>
  {
    b.AddNotificationHub();
  })
  .ConfigureLogging((context, b) => { });
var host = builder.Build();
using (host)
{
  await host.RunAsync();
}
```

appsettings.json

```json
{
    "ConnectionStrings": {
        "NotificationHub": ""
    },
    "NotificationHubName": "",
}
```

#### Output binding samples with timer trigger

```csharp
public async Task Test([TimerTrigger("*/5 * * * * *")] TimerInfo info,
	[Notification] IAsyncCollector<NotificationMessagge> collector)
{
  await collector.AddAsync(new () {
    Platform = Platform.Apple,
    Payload = @"{ ""aps"": { ""alert"": ""test"", ""badge"": 90 } }"
  });
}

[return: Notification]
public NotificationMessagge Test2([TimerTrigger("*/8 * * * * *")] TimerInfo info)
{
  return new()
  {
    Platform = Platform.Apple,
    Payload = @"{ ""aps"": { ""alert"": ""test"", ""badge"": 90 } }"
  };
}
```