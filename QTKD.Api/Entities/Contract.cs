namespace QTKD.Api.Entities;

public class Contract
{
    public int Id { get; set; }
    public string ContractNo { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal ContractValue { get; set; }
    public string? ServiceTerms { get; set; }
    public string? AttachmentUrl { get; set; }
    public string Status { get; set; } = "draft"; // draft, pending_approval, approved, rejected, revision_requested, active

    // navigation property
    public Customer? Customer { get; set; }
}