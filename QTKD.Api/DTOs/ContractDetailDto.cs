namespace QTKD.Api.DTOs;

public class ContractDetailDto
{
    public int Id { get; set; }
    public string ContractNo { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal ContractValue { get; set; }
    public string? ServiceTerms { get; set; }
    public string? AttachmentUrl { get; set; }
    public string Status { get; set; } = string.Empty;
}