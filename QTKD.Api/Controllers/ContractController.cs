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

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ContractDetailDto>> GetContractById(int id)
    {
        var contract = await m_ContractService.GetContractByIdAsync(id);
        if (contract == null)
            return NotFound(new { message = $"Contract with Id {id} was not found" });
        return Ok(contract);
    }
}