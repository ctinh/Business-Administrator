using Microsoft.AspNetCore.Mvc;
using QTKD.Api.DTOs;
using QTKD.Api.Services;

namespace QTKD.Api.Controllers;

[ApiController]
[Route("api/customers")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService m_CustomerService;

    public CustomersController(ICustomerService CustomerService)
    {
        m_CustomerService = CustomerService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CustomerDto>>> GetCustomers()
    {
        var customers = await m_CustomerService.GetAllCustomersAsync();
        return Ok(customers);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CustomerDto>> GetCustomerById(int id)
    {
        var customer = await m_CustomerService.GetCustomerByIdAsync(id);
        if (customer == null)
        {
            return NotFound(ServiceResult<CustomerDto>.Failure("CUS_NOT_FOUND", $"customer with id {id} not found"));
        }

        return Ok(customer);
    }

    [HttpPost]
    public async Task<ActionResult<ServiceResult<CustomerDto>>> CreateCustomer([FromBody] CreateCustomerRequest request)
    {
        var result = await m_CustomerService.CreateCustomerAsync(request);
        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetCustomerById), new { id = result.Data!.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ServiceResult<CustomerDto>>> UpdateCustomer(int id, [FromBody] UpdateCustomerRequest request)
    {
        var result = await m_CustomerService.UpdateCustomerAsync(id, request);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "CUS_NOT_FOUND")
            {
                return NotFound(result);
            }
            return BadRequest(result);
        }

        return Ok(result);
    }
}