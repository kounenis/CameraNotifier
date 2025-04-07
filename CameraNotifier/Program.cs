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
            Console.WriteLine("Creating host builder");
            var configurationBuilder = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json",
                    optional: false);

            Console.WriteLine("Custom app settings: " + options.CustomAppSettings);
            if (!string.IsNullOrEmpty(options.CustomAppSettings) && File.Exists(options.CustomAppSettings))
            {
                Console.WriteLine("File exists, adding JSON");
                configurationBuilder.AddJsonFile(options.CustomAppSettings);
            }

            Console.WriteLine("Building configuration");
            var configuration = configurationBuilder.Build();


            Console.WriteLine("Setting up logger");
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .WriteTo.File(configuration.GetValue<string>("Logging:LogFile"))
                .CreateLogger();

            Log.Logger.Information("Starting application");

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
