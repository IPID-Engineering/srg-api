using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SRG.Domain.Entities;
using SRG.Domain.Enums;
using SRG.Infrastructure.Persistence;

namespace SRG.Infrastructure.DailyReports;

public class DailyReportAutoCreationService(
    IServiceScopeFactory scopeFactory,
    ILogger<DailyReportAutoCreationService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await CreateTomorrowReportsAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeUntilNextMidnight(), stoppingToken);
            await CreateTomorrowReportsAsync(stoppingToken);
        }
    }

    private async Task CreateTomorrowReportsAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

        var crews = await dbContext.Crews
            .AsNoTracking()
            .Select(crew => new { crew.Id, crew.ProjectId, crew.CreatedById })
            .ToListAsync(cancellationToken);

        foreach (var crew in crews)
        {
            var exists = await dbContext.DailyReports.AnyAsync(
                report => report.CrewId == crew.Id && report.Date == tomorrow,
                cancellationToken);

            if (exists)
            {
                continue;
            }

            var sectionId = await dbContext.Sections
                .AsNoTracking()
                .Where(section => section.ProjectId == crew.ProjectId)
                .OrderBy(section => section.Name)
                .Select(section => (Guid?)section.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (sectionId is null)
            {
                logger.LogWarning("Skipping automatic DailyReport creation for crew {CrewId}: project has no sections.", crew.Id);
                continue;
            }

            await dbContext.DailyReports.AddAsync(new DailyReport
            {
                Date = tomorrow,
                CrewId = crew.Id,
                ProjectId = crew.ProjectId,
                SectionId = sectionId.Value,
                CreatedById = crew.CreatedById,
                Status = DailyReportStatus.Draft,
                CreatedAt = DateTime.UtcNow,
            }, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static TimeSpan TimeUntilNextMidnight()
    {
        var now = DateTimeOffset.Now;
        return now.Date.AddDays(1) - now;
    }
}
