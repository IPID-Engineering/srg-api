namespace SRG.Domain.Entities;

public class IssueItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid IssueId { get; set; }
    public Issue? Issue { get; set; }
    public Guid MaterialId { get; set; }
    public Material? Material { get; set; }
    public decimal Quantity { get; set; }
}
