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
        var customers await = m_CustomerService.GetAllCustomersAsync();
        return Ok(customers);
    }
}