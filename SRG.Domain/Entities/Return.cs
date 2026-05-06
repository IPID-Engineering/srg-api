using SRG.Domain.Enums;

namespace SRG.Domain.Entities;

public class Return
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid FromWarehouseId { get; set; }
    public Warehouse? FromWarehouse { get; set; }
    public Guid ToWarehouseId { get; set; }
    public Warehouse? ToWarehouse { get; set; }
    public Guid CreatedById { get; set; }
    public User? CreatedBy { get; set; }
    public ReturnStatus Status { get; set; } = ReturnStatus.Draft;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<ReturnItem> Items { get; set; } = [];
}
