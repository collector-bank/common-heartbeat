using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Collector.Common.Heartbeat;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Core;

namespace Serilog.HeartbeatExample
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<IHeartbeatMonitor, HeartbeatMonitor>();
            services.AddScoped<ILogger>(p =>
            {
                var loggerConfig = new LoggerConfiguration()
                    .WriteTo.Console(outputTemplate: "{Timestamp:HH:mm} [{Level}] [{CorrelationId}]: {Message}{NewLine}")
                    .WriteTo.Trace(outputTemplate: "{Timestamp:HH:mm} [{Level}] [{CorrelationId}]: {Message}{NewLine}")
                    .CreateLogger();
                return loggerConfig.ForContext("CorrelationId", Guid.NewGuid().ToString());
            });
            services.AddScoped<Microsoft.Extensions.Logging.ILogger<IHeartbeatMonitor>>(p =>
            {
                var loggerFactory = p.GetRequiredService<ILoggerFactory>();
                var serilogger = p.GetRequiredService<ILogger>();
                return loggerFactory.AddSerilog(serilogger).CreateLogger<IHeartbeatMonitor>();
            });
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseHeartbeat<IHeartbeatMonitor>(x => x.RunAsync(), options =>
            {
                options.ApiKey = "Secret"; // Default = string.Empty / None
                //options.ApiKeyHeaderKey = "SomeHeaderName"; // Default = "DiagnosticsAPIKey"
                //options.HeartbeatRoute = "/api/otherroute"; // Default = "/api/heartbeat"
            });

            app.Run(async (context) =>
            {
                await context.Response.WriteAsync("Hello World!");
            });
        }
    }
}
