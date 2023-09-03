using System.Net.Mime;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Newtonsoft.Json;
using WalletService.Data;
using WalletService.DataAccess;
using WalletService.RequestModels;
using WalletService.Services;
using WalletService.Validations;
using Formatting = Newtonsoft.Json.Formatting;

namespace WalletService;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IWalletService, WalletService.Services.WalletService>();
        services.AddScoped<IWalletRepository, WalletRepository>();
        services.AddTransient<IValidator<CreateWalletRequest>, CreateWalletRequestValidator>();
        services.AddTransient<IValidator<UpdateBalanceRequest>, UpdateWalletRequestValidator>();
        services.AddScoped<IDbContextTransactionProxy, DbContextTransactionProxy>();

        services.AddHealthChecks()
            .AddCheck("Ping", () => HealthCheckResult
                    .Healthy("Ping is OK."),
                tags: new[] { "ping" })
            .AddDbContextCheck<WalletDbContext>("Database");

        services.AddDbContext<WalletDbContext>(options =>
            options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));
        services.AddSwaggerGen();
        services.AddControllers();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "Wallet Service V1"); });
        }

        using (var serviceScope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
        {
            var context = serviceScope.ServiceProvider.GetService<WalletDbContext>();
            context.Database.Migrate();
        }

        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapHealthChecks("/health", new HealthCheckOptions
            {
                ResponseWriter = async (context, report) =>
                {
                    var result = JsonConvert.SerializeObject(
                        new
                        {
                            status = report.Status.ToString(),
                            checks = report.Entries.Select(entry => new
                            {
                                name = entry.Key,
                                status = entry.Value.Status.ToString(),
                                description = entry.Key == "Database" ? "Database is OK" : entry.Value.Description
                            })
                        },
                        Formatting.Indented
                    );
                    context.Response.ContentType = MediaTypeNames.Application.Json;
                    await context.Response.WriteAsync(result);
                }
            });
        });
    }
}