using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using SRG.Application.Common;
using SRG.Domain.Entities;

namespace SRG.Infrastructure.Persistence;

public class AppDbContext(
    DbContextOptions<AppDbContext> options,
    ICurrentUserContext currentUserContext) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Section> Sections => Set<Section>();
    public DbSet<Installation> Installations => Set<Installation>();
    public DbSet<Crew> Crews => Set<Crew>();
    public DbSet<CrewAccess> CrewAccessList => Set<CrewAccess>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Worker> Workers => Set<Worker>();
    public DbSet<SubcontractorWorker> SubcontractorWorkers => Set<SubcontractorWorker>();
    public DbSet<SubcontractorCrew> SubcontractorCrews => Set<SubcontractorCrew>();
    public DbSet<SubcontractorCrewPmAccess> SubcontractorCrewPmAccessList => Set<SubcontractorCrewPmAccess>();
    public DbSet<SubcontractorForemanHistory> SubcontractorForemanHistory => Set<SubcontractorForemanHistory>();
    public DbSet<ProjectSubcontractor> ProjectSubcontractors => Set<ProjectSubcontractor>();
    public DbSet<DailyReport> DailyReports => Set<DailyReport>();
    public DbSet<DailyReportComment> DailyReportComments => Set<DailyReportComment>();
    public DbSet<DailyReportStatusHistory> DailyReportStatusHistory => Set<DailyReportStatusHistory>();
    public DbSet<DailyReportChangeHistory> DailyReportChangeHistory => Set<DailyReportChangeHistory>();
    public DbSet<DailyReportWorkOrder> DailyReportWorkOrders => Set<DailyReportWorkOrder>();
    public DbSet<WorkHour> WorkHours => Set<WorkHour>();
    public DbSet<WorkEntry> DailyReportWorkEntries => Set<WorkEntry>();
    public DbSet<MaterialUsage> MaterialUsages => Set<MaterialUsage>();
    public DbSet<WorkType> WorkTypes => Set<WorkType>();
    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();
    public DbSet<OrderedWork> OrderedWorks => Set<OrderedWork>();
    public DbSet<OrderedMaterial> OrderedMaterials => Set<OrderedMaterial>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Material> Materials => Set<Material>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<WarehouseStock> WarehouseStocks => Set<WarehouseStock>();
    public DbSet<Issue> Issues => Set<Issue>();
    public DbSet<IssueItem> IssueItems => Set<IssueItem>();
    public DbSet<Return> Returns => Set<Return>();
    public DbSet<ReturnItem> ReturnItems => Set<ReturnItem>();
    public DbSet<GoodsReceivedVoucher> GoodsReceivedVouchers => Set<GoodsReceivedVoucher>();
    public DbSet<GoodsReceivedVoucherItem> GoodsReceivedVoucherItems => Set<GoodsReceivedVoucherItem>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<MaterialRequest> MaterialRequests => Set<MaterialRequest>();
    public DbSet<InewiRecord> InewiRecords => Set<InewiRecord>();
    public DbSet<InewiIntegrationSettings> InewiIntegrationSettings => Set<InewiIntegrationSettings>();
    public DbSet<RateGroup> RateGroups => Set<RateGroup>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    public override int SaveChanges()
    {
        AddAutomaticAuditLogs();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        AddAutomaticAuditLogs();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void AddAutomaticAuditLogs()
    {
        ChangeTracker.DetectChanges();

        var logs = ChangeTracker.Entries()
            .Where(entry => entry.Entity is not AuditLog
                && entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Select(CreateAuditLog)
            .Where(log => log is not null)
            .Select(log => log!)
            .ToList();

        if (logs.Count > 0)
        {
            AuditLogs.AddRange(logs);
        }
    }

    private AuditLog? CreateAuditLog(EntityEntry entry)
    {
        var entityId = GetEntityId(entry);

        if (entityId is null)
        {
            return null;
        }

        object? changes = entry.State switch
        {
            EntityState.Added => new
            {
                state = "Added",
                values = CurrentValues(entry),
            },
            EntityState.Deleted => new
            {
                state = "Deleted",
                values = OriginalValues(entry),
            },
            EntityState.Modified => new
            {
                state = "Modified",
                fields = ModifiedValues(entry),
            },
            _ => null,
        };

        if (changes is null)
        {
            return null;
        }

        return new AuditLog
        {
            UserId = currentUserContext.UserId ?? Guid.Empty,
            Action = $"ENTITY_{entry.State.ToString().ToUpperInvariant()}",
            EntityName = entry.Metadata.ClrType.Name,
            EntityId = entityId.Value,
            Changes = JsonSerializer.Serialize(changes),
            CreatedAt = DateTime.UtcNow,
        };
    }

    private static Guid? GetEntityId(EntityEntry entry)
    {
        var idProperty = entry.Properties.FirstOrDefault(property => property.Metadata.Name == "Id");
        return idProperty?.CurrentValue is Guid id ? id : null;
    }

    private static Dictionary<string, object?> CurrentValues(EntityEntry entry)
    {
        return entry.Properties
            .Where(property => !property.Metadata.IsShadowProperty())
            .ToDictionary(property => property.Metadata.Name, property => property.CurrentValue);
    }

    private static Dictionary<string, object?> OriginalValues(EntityEntry entry)
    {
        return entry.Properties
            .Where(property => !property.Metadata.IsShadowProperty())
            .ToDictionary(property => property.Metadata.Name, property => property.OriginalValue);
    }

    private static Dictionary<string, object?> ModifiedValues(EntityEntry entry)
    {
        return entry.Properties
            .Where(property => property.IsModified && !property.Metadata.IsShadowProperty())
            .ToDictionary(
                property => property.Metadata.Name,
                property => (object?)new { before = property.OriginalValue, after = property.CurrentValue });
    }
}
