using Microsoft.EntityFrameworkCore;
using QTKD.Api.Data;
using QTKD.Api.DTOs;

namespace QTKD.Api.Services;

public interface ICustomerService
{
    Task<IEnumerable<CustomerDto>> GetAllCustomersAsync();
}

public class CustomerService : ICustomerService
{
    private readonly QtkdDbContext m_context;

    public CustomerService(QtkdDbContext context)
    {
        m_context = context;
    }

    public async Task<IEnumerable<CustomerDto>> GetAllCustomersAsync()
    {
        return await m_context.Customers
            .AsNoTracking()
            .OrderBy(c => c.Id)
            .Select(c => new CustomerDto
            {
                Id = c.Id,
                CustomerCode = c.CustomerCode,
                Name = c.Name,
                TaxCode = c.TaxCode,
                Address = c.Address,
                ContactEmail = c.ContactEmail,
                Status = c.Status
            })
            .ToListAsync();
    }
}