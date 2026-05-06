namespace SRG.Domain.Entities;

public class ReturnItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ReturnId { get; set; }
    public Return? Return { get; set; }
    public Guid MaterialId { get; set; }
    public Material? Material { get; set; }
    public decimal Quantity { get; set; }
}
