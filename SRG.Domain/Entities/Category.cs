namespace SRG.Domain.Entities;

public class Category
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public string? FamilyCode { get; set; }
    public string? SubFamilyCode { get; set; }
    public Guid? ParentCategoryId { get; set; }
    public Category? ParentCategory { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Category> SubCategories { get; set; } = [];
    public ICollection<Material> Materials { get; set; } = [];
}
