using GovUk.Frontend.AspNetCore;
using System.Diagnostics.CodeAnalysis;

namespace Dfe.Unified.Intake
{
    [ExcludeFromCodeCoverage]
    public class Startup
    {
        private IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            // App insights is configured via the connection string in appsettings.json, so we only add it if the connection string is present.
            string appInsightsConnectionString = Configuration["ApplicationInsights:ConnectionString"];
            if (!string.IsNullOrWhiteSpace(appInsightsConnectionString))
            {
                services.AddApplicationInsightsTelemetry();
            }

            services.AddSession();

            services.AddHttpContextAccessor();

            services.AddGovUkFrontend();

            services.AddRazorPages();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Errors");
            }

            app.UseStatusCodePagesWithReExecute("/Errors", "?statusCode={0}");

            app.UseHttpsRedirection();

            app.UseStaticFiles();
            app.UseGovUkFrontend();
            app.UseRouting();

            app.UseSession();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapStaticAssets();
                endpoints.MapRazorPages().WithStaticAssets();
            });
        }
    }
}
