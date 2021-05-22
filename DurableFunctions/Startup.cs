using DurableFunctions;
using DurableFunctions.Entities;
using DurableFunctions.Entities.Interfaces;
using DurableFunctions.Monitoring;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;


[assembly: WebJobsStartup(typeof(Startup))]

public class Startup : IWebJobsStartup
{
    public void Configure(IWebJobsBuilder builder)
    {
        builder.Services.AddTransient<IWeatherService, OpenWeatherMapService>();
        builder.Services.AddTransient<IEntityFactory, EntityFactory>();

        builder.Services.BuildServiceProvider();
    }
}
