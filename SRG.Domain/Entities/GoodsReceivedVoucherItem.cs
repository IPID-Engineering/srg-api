namespace SRG.Domain.Entities;

public class GoodsReceivedVoucherItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GoodsReceivedVoucherId { get; set; }
    public GoodsReceivedVoucher? GoodsReceivedVoucher { get; set; }
    public Guid MaterialId { get; set; }
    public Material? Material { get; set; }
    public int? LineNumber { get; set; }
    public string? PartNumber { get; set; }
    public string? VendorPartNumber { get; set; }
    public decimal Quantity { get; set; }
    public required string Unit { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal? ExtendedPrice { get; set; }
    public string? Status { get; set; }
}
