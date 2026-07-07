using GovUk.Frontend.AspNetCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
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

            services.AddHttpClient();

            services.AddHttpContextAccessor();

            services.AddGovUkFrontend();

            // Sign users in with their DfE account (Azure AD) so the application can identify who is
            // making a request. The signed-in user's details are then available on their identity.
            services.AddMicrosoftIdentityWebAppAuthentication(Configuration);

            services.Configure<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme,
                options =>
                {
                    options.Cookie.Name = ".UnifiedIntake.Login";
                    options.Cookie.HttpOnly = true;
                    options.Cookie.IsEssential = true;
                });

            services.AddControllersWithViews()
                .AddMicrosoftIdentityUI();

            services.AddAuthorization(options =>
            {
                options.DefaultPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
            });

            services.AddRazorPages(options =>
            {
                options.Conventions.AuthorizeFolder("/");
                options.Conventions.AllowAnonymousToPage("/Error");
                options.Conventions.AllowAnonymousToPage("/Privacy");
            });
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

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapStaticAssets();
                endpoints.MapRazorPages().WithStaticAssets();
                endpoints.MapControllers();
            });
        }
    }
}
