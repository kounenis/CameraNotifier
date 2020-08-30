using CameraNotifier.Services.CameraFeed;
using CameraNotifier.Services.ImageClassifier;
using CameraNotifier.Services.SlackNotifier;
using CameraNotifier.Services.WatchService;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tensorflow;

namespace CameraNotifier
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
            services.AddRazorPages();

            services.Configure<ImageClassifierOptions>(Configuration.GetSection(ImageClassifierOptions.SettingsGroupName));
            services.Configure<WatchServiceOptions>(Configuration.GetSection(WatchServiceOptions.SettingsGroupName));
            services.Configure<SlackNotifierOptions>(Configuration.GetSection(SlackNotifierOptions.SettingsGroupName));
            services.Configure<CameraFeedOptions>(Configuration.GetSection(CameraFeedOptions.SettingsGroupName));

            services.AddSingleton<ICameraFeedService, CameraFeedService>();
            services.AddSingleton<IImageClassifier, ImageClassifier>();
            services.AddSingleton<ISlackNotifier, SlackNotifier>();
            services.AddSingleton<IWatchService, WatchService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
            });

            ((IWatchService)app.ApplicationServices.GetService(typeof(IWatchService))).Start();
        }
    }
}
