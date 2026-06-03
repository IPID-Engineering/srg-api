using SRG.Domain.Enums;

namespace SRG.Domain.Entities;

public class Issue
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Number { get; set; }
    public Guid WorkOrderId { get; set; }
    public WorkOrder? WorkOrder { get; set; }
    public Guid FromWarehouseId { get; set; }
    public Warehouse? FromWarehouse { get; set; }
    public Guid ToWarehouseId { get; set; }
    public Warehouse? ToWarehouse { get; set; }
    public Guid CreatedById { get; set; }
    public User? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ConfirmedAt { get; set; }
    public IssueStatus Status { get; set; } = IssueStatus.Draft;
    
    /// <summary>
    /// Pracownik brygady odbierający materiały (opcjonalne).
    /// </summary>
    public Guid? ReceivedByWorkerId { get; set; }
    public Worker? ReceivedByWorker { get; set; }
    
    /// <summary>
    /// Pracownik podwykonawcy odbierający materiały (opcjonalne).
    /// </summary>
    public Guid? ReceivedBySubcontractorWorkerId { get; set; }
    public SubcontractorWorker? ReceivedBySubcontractorWorker { get; set; }
    
    /// <summary>
    /// Imię i nazwisko osoby odbierającej (zapisywane przy potwierdzeniu).
    /// </summary>
    public string? ReceivedByName { get; set; }
    
    /// <summary>
    /// Unikalny kod weryfikacyjny generowany przy potwierdzeniu wydania.
    /// Używany do weryfikacji autentyczności dokumentu przez QR kod.
    /// </summary>
    public string? VerificationCode { get; set; }
    
    public ICollection<IssueItem> Items { get; set; } = [];
}
