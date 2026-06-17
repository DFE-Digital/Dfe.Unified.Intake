using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace Dfe.Unified.Intake
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
               .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
               .Enrich.FromLogContext()
               .WriteTo.Console(new RenderedCompactJsonFormatter())
               .CreateLogger();

            Log.Information("Starting web host");
            CreateHostBuilder(args).Build().Run();
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
               .UseSerilog()
               .ConfigureWebHostDefaults(webBuilder =>
               {
                   webBuilder.UseStartup<Startup>();
                   webBuilder.UseKestrel(options =>
                   {
                       options.AddServerHeader = false;
                   });
               });
        }

        //public static void Main(string[] args)
        //{
        //    var builder = WebApplication.CreateBuilder(args);

        //    // Add services to the container.
        //    builder.Services.AddRazorPages();

        //    var app = builder.Build();

        //    // Configure the HTTP request pipeline.
        //    if (!app.Environment.IsDevelopment())
        //    {
        //        app.UseExceptionHandler("/Error");
        //        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        //        app.UseHsts();
        //    }

        //    app.UseHttpsRedirection();

        //    app.UseRouting();

        //    app.UseAuthorization();

        //    app.MapStaticAssets();
        //    app.MapRazorPages()
        //       .WithStaticAssets();

        //    app.Run();
        //}
    }
}
