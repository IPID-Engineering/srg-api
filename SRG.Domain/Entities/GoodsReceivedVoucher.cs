using SRG.Domain.Enums;

namespace SRG.Domain.Entities;

public class GoodsReceivedVoucher
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Number { get; set; }
    public Guid WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }
    public Guid CreatedById { get; set; }
    public User? CreatedBy { get; set; }
    public string? SupplierName { get; set; }
    public DateOnly DeliveryDate { get; set; }
    public GoodsReceivedVoucherStatus Status { get; set; } = GoodsReceivedVoucherStatus.Draft;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<GoodsReceivedVoucherItem> Items { get; set; } = [];
}
