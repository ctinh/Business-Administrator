namespace QTKD.Api.DTOs;

public class PriceItemDto 
{
    public int Id { get; set; }
    public int PriceListId { get; set; }
    public string ServiceCode { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public string Unit { get; set; } = string.Empty;
}

public class PriceListDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly EffectiveDate { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<PriceItemDto> Items { get; set; } = new();
}

public class CreatePriceListRequest
{
    public string Name { get; set; } = string.Empty;
    public DateOnly EffectiveDate { get; set; }
    public DateOnly? ExpiryDate { get; set; }
}

public class CreatePriceItemRequest
{
    public string ServiceCode { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public string Unit { get; set; } = string.Empty;
}