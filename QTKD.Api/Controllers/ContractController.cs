using Microsoft.AspNetCore.Mvc;
using QTKD.Api.DTOs;
using QTKD.Api.Services;

namespace QTKD.Api.Controllers;

[ApiController]
[Route("api/contracts")]
public class ContractsController : ControllerBase
{
    private readonly IContractService m_ContractService;

    public ContractsController(IContractService ContractService)
    {
        m_ContractService = ContractService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ContractDetailDto>>> GetAllContracts()
    {
        var contracts = await m_ContractService.GetAllContractsAsync();
        return Ok(contracts);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ContractDetailDto>> GetContractById(int id)
    {
        var contract = await m_ContractService.GetContractByIdAsync(id);
        if (contract == null)
        {
            return NotFound(ServiceResult<ContractDetailDto>.Failure("CTR_NOT_FOUND", $"contract with id {id} not found"));
        }

        return Ok(contract);
    }

    [HttpPost]
    public async Task<ActionResult<ServiceResult<ContractDetailDto>>> CreateContract([FromBody] CreateContractRequest request)
    {
        var result = await m_ContractService.CreateContractAsync(request);
        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetContractById), new { id = result.Data!.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ServiceResult<ContractDetailDto>>> UpdateContract(int id, [FromBody] UpdateContractRequest request)
    {
        var result = await m_ContractService.UpdateContractAsync(id, request);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "CTR_NOT_FOUND")
            {
                return NotFound(result);
            }
            return BadRequest(result);
        }

        return Ok(result);
    }

    // US03: submit contract for approval
    [HttpPost("{id:int}/submit")]
    public async Task<ActionResult<ServiceResult<ContractDetailDto>>> SubmitContract(int id)
    {
        var result = await m_ContractService.SubmitContractAsync(id);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "CTR_NOT_FOUND")
            {
                return NotFound(result);
            }
            if (result.ErrorCode == "CTR_ALREADY_SUBMITTED")
            {
                return Conflict(result);
            }
            return BadRequest(result);
        }

        return Ok(result);
    }
}