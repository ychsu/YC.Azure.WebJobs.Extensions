## YC.Azure.WebJobs.Extensions.EntityFrameworkCore

### Installation

Nuget

```
Install-Package YC.Azure.WebJobs.Extensions.EntityFrameworkCore
```

dotnet cli

```
dotnet add package YC.Azure.WebJobs.Extensions.EntityFrameworkCore
```

### Using the binding

#### Configure

```csharp
var builder = new HostBuilder();
builder
  .ConfigureWebJobs(b =>
  {
    b.AddEntityFrameworkCore();
  })
  .ConfigureServices((context, service) => 
  {
    service
      .AddDbContext<DemoContext>(opt =>
      {
        opt.UseSqlServer("Server=localhost;Database=Demo;Trusted_Connection=True;");
      });
  })
  .ConfigureLogging((context, b) => { });
var host = builder.Build();
using (host)
{
  await host.RunAsync();
}
```

#### Output binding samples with timer trigger

```csharp
public async Task Test([TimerTrigger("*/5 * * * * *")] TimerInfo info,
	[DbSet(typeof(DemoContext))] IAsyncCollector<Todo> collector)
{
  await collector.AddAsync(new Todo() { Title = "todo" });
}

[return: DbSet(typeof(BlogContext))]
public Draft Test2([TimerTrigger("*/8 * * * * *")] TimerInfo info)
{
  return new Draft();
}
```