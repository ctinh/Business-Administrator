namespace QTKD.Api.Entities;

public class PriceItem
{
    public int Id { get; set; }
    public int PriceListId { get; set; }
    public string ServiceCode { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public string Unit { get; set; } = string.Empty; // container, ton, day, ...

    // navigation property
    public PriceList? PriceList { get; set; }
}