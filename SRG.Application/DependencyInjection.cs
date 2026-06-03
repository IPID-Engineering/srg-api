using Microsoft.Extensions.DependencyInjection;
using SRG.Application.Analytics;
using SRG.Application.Auth;
using SRG.Application.Construction;
using SRG.Application.DailyReports;
using SRG.Application.Email;
using SRG.Application.Inewi;
using SRG.Application.MaterialRequests;
using SRG.Application.RateGroups;
using SRG.Application.Warehouses;
using SRG.Application.WorkOrders;

namespace SRG.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IMicrosoftAuthService, MicrosoftAuthService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IForemanAuthService, ForemanAuthService>();
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<ISectionService, SectionService>();
        services.AddScoped<ICrewService, CrewService>();
        services.AddScoped<ICrewAccessService, CrewAccessService>();
        services.AddScoped<ITeamService, TeamService>();
        services.AddScoped<IWorkerService, WorkerService>();
        services.AddScoped<IForemanWorkerService, ForemanWorkerService>();
        services.AddScoped<ISubcontractorWorkerService, SubcontractorWorkerService>();
        services.AddScoped<ISubcontractorCrewService, SubcontractorCrewService>();
        services.AddScoped<IProjectSubcontractorService, ProjectSubcontractorService>();
        services.AddScoped<IDailyReportService, DailyReportService>();
        services.AddScoped<IForemanDailyReportService, ForemanDailyReportService>();
        services.AddScoped<IMaterialService, MaterialService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IWarehouseService, WarehouseService>();
        services.AddScoped<IIssueService, IssueService>();
        services.AddScoped<IReturnService, ReturnService>();
        services.AddScoped<IGoodsReceivedVoucherService, GoodsReceivedVoucherService>();
        services.AddScoped<IWorkTypeService, WorkTypeService>();
        services.AddScoped<IWorkOrderService, WorkOrderService>();
        services.AddScoped<ICrewStatsService, CrewStatsService>();
        services.AddScoped<IMaterialRequestService, MaterialRequestService>();
        services.AddScoped<IInewiService, InewiService>();
        services.AddScoped<IInewiIntegrationService, InewiIntegrationService>();
        services.AddScoped<IRateGroupService, RateGroupService>();

        return services;
    }
}
