namespace QTKD.Api.DTOs;

public class CustomerDto
{
    public int Id { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? TaxCode { get; set; }
    public string? Address { get; set; }
    public string? ContactEmail { get; set; }
    public string Status { get; set; } = string.Empty;
}