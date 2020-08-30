using System;
using System.IO;
using CommandLine;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace CameraNotifier
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Parser.Default.ParseArguments<ApplicationOptions>(args)
                .WithParsed(options =>
                {
                    CreateHostBuilder(args, options).Build().Run();
                });
        }

        public static IHostBuilder CreateHostBuilder(string[] args, ApplicationOptions options)
        {
            var configurationBuilder = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json",
                    optional: true);

            if (!string.IsNullOrEmpty(options.CustomAppSettings) && File.Exists(options.CustomAppSettings))
            {
                configurationBuilder.AddJsonFile(options.CustomAppSettings);
            }

            var configuration = configurationBuilder.Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .WriteTo.File(configuration.GetValue<string>("Logging:LogFile"))
                .CreateLogger();

            return Host.CreateDefaultBuilder(args)
                    .ConfigureWebHostDefaults(webBuilder =>
                    {
                        webBuilder
                            .UseStartup<Startup>()
                            .UseConfiguration(configuration);
                    });
        }
    }
}
