using Microsoft.EntityFrameworkCore;
using QTKD.Api.Data;
using QTKD.Api.DTOs;
using QTKD.Api.Entities;

namespace QTKD.Api.Services;

public interface IPriceListService
{
    Task<IEnumerable<PriceListDto>> GetAllPriceListsAsync();
    Task<PriceListDto?> GetPriceListByIdAsync(int id);
    Task<ServiceResult<PriceListDto>> CreatePriceListAsync(CreatePriceListRequest request);
    Task<ServiceResult<IEnumerable<PriceItemDto>>> GetPriceItemsByPriceListIdAsync(int priceListId);
    Task<ServiceResult<PriceItemDto>> AddPriceItemAsync(int priceListId, CreatePriceItemRequest request);
}

public class PriceListService : IPriceListService
{
    private readonly QtkdDbContext m_context;

    public PriceListService(QtkdDbContext context)
    {
        m_context = context;
    }

    public async Task<IEnumerable<PriceListDto>> GetAllPriceListsAsync()
    {
        var priceLists = await m_context.PriceLists
            .AsNoTracking()
            .Include(p => p.PriceItems)
            .OrderBy(p => p.Id)
            .ToListAsync();

        return priceLists.Select(MapToDto);
    }

    public async Task<PriceListDto?> GetPriceListByIdAsync(int id)
    {
        var priceList = await m_context.PriceLists
            .AsNoTracking()
            .Include(p => p.PriceItems)
            .FirstOrDefaultAsync(p => p.Id == id);

        return priceList == null ? null : MapToDto(priceList);
    }

    public async Task<ServiceResult<PriceListDto>> CreatePriceListAsync(CreatePriceListRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return ServiceResult<PriceListDto>.Failure("PRC_INVALID_NAME", "price list name is required");
        }

        // PRC-02: effective date must not be later than expiry date
        if (request.ExpiryDate.HasValue && request.EffectiveDate > request.ExpiryDate.Value)
        {
            return ServiceResult<PriceListDto>.Failure(
                "PRC-02",
                "PRC-02 violation: effective date must not be later than expiry date");
        }

        var priceList = new PriceList
        {
            Name = request.Name.Trim(),
            EffectiveDate = request.EffectiveDate,
            ExpiryDate = request.ExpiryDate,
            Status = "draft"
        };

        m_context.PriceLists.Add(priceList);
        await m_context.SaveChangesAsync();

        return ServiceResult<PriceListDto>.Success(MapToDto(priceList), "price list created successfully");
    }

    public async Task<ServiceResult<IEnumerable<PriceItemDto>>> GetPriceItemsByPriceListIdAsync(int priceListId)
    {
        var exists = await m_context.PriceLists.AnyAsync(p => p.Id == priceListId);
        if (!exists)
        {
            return ServiceResult<IEnumerable<PriceItemDto>>.Failure("PRC_NOT_FOUND", $"price list with id {priceListId} not found");
        }

        var items = await m_context.PriceItems
            .AsNoTracking()
            .Where(pi => pi.PriceListId == priceListId)
            .OrderBy(pi => pi.Id)
            .Select(pi => MapItemToDto(pi))
            .ToListAsync();

        return ServiceResult<IEnumerable<PriceItemDto>>.Success(items, "price items retrieved successfully");
    }

    public async Task<ServiceResult<PriceItemDto>> AddPriceItemAsync(int priceListId, CreatePriceItemRequest request)
    {
        // validate that the referenced price list exists
        var priceList = await m_context.PriceLists
            .Include(p => p.PriceItems)
            .FirstOrDefaultAsync(p => p.Id == priceListId);

        if (priceList == null)
        {
            return ServiceResult<PriceItemDto>.Failure("PRC_NOT_FOUND", $"price list with id {priceListId} does not exist");
        }

        if (string.IsNullOrWhiteSpace(request.ServiceCode) || string.IsNullOrWhiteSpace(request.ServiceName))
        {
            return ServiceResult<PriceItemDto>.Failure("PRI_INVALID_SERVICE", "service code and service name are required");
        }

        // validate that the unit price is greater than zero
        if (request.UnitPrice <= 0)
        {
            return ServiceResult<PriceItemDto>.Failure("PRI_INVALID_PRICE", "unit price must be greater than 0");
        }

        var NormalizedCode = request.ServiceCode.Trim().ToUpperInvariant();
        if (priceList.PriceItems.Any(pi => pi.ServiceCode == NormalizedCode))
        {
            return ServiceResult<PriceItemDto>.Failure("PRI_DUPLICATE_CODE", $"service code '{NormalizedCode}' already exists in this price list");
        }

        var item = new PriceItem
        {
            PriceListId = priceListId,
            ServiceCode = NormalizedCode,
            ServiceName = request.ServiceName.Trim(),
            UnitPrice = request.UnitPrice,
            Unit = string.IsNullOrWhiteSpace(request.Unit) ? "container" : request.Unit.Trim()
        };

        m_context.PriceItems.Add(item);
        await m_context.SaveChangesAsync();

        return ServiceResult<PriceItemDto>.Success(MapItemToDto(item), "price item added to price list successfully");
    }

    private static PriceListDto MapToDto(PriceList p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        EffectiveDate = p.EffectiveDate,
        ExpiryDate = p.ExpiryDate,
        Status = p.Status,
        Items = p.PriceItems.Select(MapItemToDto).ToList()
    };

    private static PriceItemDto MapItemToDto(PriceItem pi) => new()
    {
        Id = pi.Id,
        PriceListId = pi.PriceListId,
        ServiceCode = pi.ServiceCode,
        ServiceName = pi.ServiceName,
        UnitPrice = pi.UnitPrice,
        Unit = pi.Unit
    };
}