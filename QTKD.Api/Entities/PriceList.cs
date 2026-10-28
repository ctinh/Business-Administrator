namespace QTKD.Api.Entities;

public class PriceList
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DateOnly EffectiveDate { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public string Status { get; set; } = "draft"; // draft, effective, expired

    // navigation property
    public ICollection<PriceItem> PriceItems { get; set; } = new List<PriceItem>();
}