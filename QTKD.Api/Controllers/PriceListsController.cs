using Microsoft.AspNetCore.Mvc;
using QTKD.Api.DTOs;
using QTKD.Api.Services;

namespace QTKD.Api.Controllers;

[ApiController]
[Route("api/pricelists")]
public class PriceListsController : ControllerBase
{
    private readonly IPriceListService m_PriceListService;

    public PriceListsController(IPriceListService PriceListService)
    {
        m_PriceListService = PriceListService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PriceListDto>>> GetPriceLists()
    {
        var priceLists = await m_PriceListService.GetAllPriceListsAsync();
        return Ok(priceLists);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PriceListDto>> GetPriceListById(int id)
    {
        var priceList = await m_PriceListService.GetPriceListByIdAsync(id);
        if (priceList == null)
        {
            return NotFound(ServiceResult<PriceListDto>.Failure("PRC_NOT_FOUND", $"price list with id {id} not found"));
        }

        return Ok(priceList);
    }

    [HttpPost]
    public async Task<ActionResult<ServiceResult<PriceListDto>>> CreatePriceList([FromBody] CreatePriceListRequest request)
    {
        var result = await m_PriceListService.CreatePriceListAsync(request);
        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetPriceListById), new { id = result.Data!.Id }, result);
    }

    [HttpGet("{id:int}/items")]
    public async Task<ActionResult<ServiceResult<IEnumerable<PriceItemDto>>>> GetPriceItems(int id)
    {
        var result = await m_PriceListService.GetPriceItemsByPriceListIdAsync(id);
        if (!result.IsSuccess)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    [HttpPost("{id:int}/items")]
    public async Task<ActionResult<ServiceResult<PriceItemDto>>> AddPriceItem(int id, [FromBody] CreatePriceItemRequest request)
    {
        var result = await m_PriceListService.AddPriceItemAsync(id, request);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "PRC_NOT_FOUND")
            {
                return NotFound(result);
            }
            return BadRequest(result);
        }

        return Ok(result);
    }
}