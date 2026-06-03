using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SRG.Application.Audit;
using SRG.Application.Analytics;
using SRG.Application.Export;
using SRG.Application.Inewi;
using SRG.Application.Persistence;
using SRG.Infrastructure.Audit;
using SRG.Infrastructure.Analytics;
using SRG.Infrastructure.DailyReports;
using SRG.Infrastructure.Export;
using SRG.Infrastructure.Inewi;
using SRG.Infrastructure.Persistence;

namespace SRG.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IConstructionRepository, ConstructionRepository>();
        services.AddScoped<IDailyReportRepository, DailyReportRepository>();
        services.AddScoped<IWarehouseRepository, WarehouseRepository>();
        services.AddScoped<IWorkOrderRepository, WorkOrderRepository>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        services.AddScoped<IExportService, ExportService>();
        services.AddScoped<IInewiRepository, InewiRepository>();
        services.AddScoped<IRateGroupRepository, RateGroupRepository>();
        services.AddHostedService<DailyReportAutoCreationService>();
        
        // Inewi API client
        services.AddHttpClient<IInewiApiClient, InewiApiClient>();

        return services;
    }
}
