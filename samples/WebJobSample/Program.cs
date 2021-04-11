﻿using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Internal;
using YC.Azure.WebJobs.Extensions.EntityFrameworkCore;
using YC.Azure.WebJobs.Extensions.NotificationHub;

namespace WebJobSample
{
    class Program
    {
        private static async Task Main()
        {
            var builder = new HostBuilder();
            builder
                .ConfigureWebJobs(b =>
                {
                    b.AddAzureStorageCoreServices();
                    b.AddTimers();
                    b.AddNotificationHub();
                    b.AddEntityFrameworkCore();
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddTransient<ISystemClock, SystemClock>();
                })
                .ConfigureLogging((context, b) => { });
            var host = builder.Build();
            using (host)
            {
                await host.RunAsync();
            }
        }
    }
}
