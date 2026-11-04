using Microsoft.EntityFrameworkCore;
using QTKD.Api.Data;
using QTKD.Api.DTOs;
using QTKD.Api.Entities;

namespace QTKD.Api.Services;

public interface ICustomerService
{
    Task<IEnumerable<CustomerDto>> GetAllCustomersAsync();
    Task<CustomerDto?> GetCustomerByIdAsync(int id);
    Task<ServiceResult<CustomerDto>> CreateCustomerAsync(CreateCustomerRequest request);
    Task<ServiceResult<CustomerDto>> UpdateCustomerAsync(int id, UpdateCustomerRequest request)
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
            .Select(c => MapToDto(c))
            .ToListAsync();
    }

    public async Task<CustomerDto?> GetCustomerByIdAsync(int id)
    {
        var customer = await m_context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);
    
        return customer == null ? null : MapToDto(customer);
    }

    public async Task<ServiceResult<CustomerDto>> CreateCustomerAsync(CreateCustomerRequest request)
    {
        // check required fields
        if (string.IsNullOrWhiteSpace(request.CustomerCode))
        {
            return ServiceResult<CustomerDto>.Failure("CUS_INVALID_CODE", "customer code is required");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return ServiceResult<CustomerDto>.Failure("CUS_INVALID_NAME", "customer name is required");
        }

        var NormalizedCode = request.CustomerCode.Trim().ToUpperInvariant();
        var exists = await m_context.Customers.AnyAsync(c => c.CustomerCode == NormalizedCode);
        if (exists)
        {
            return ServiceResult<CustomerDto>.Failure("CUS_DUPLICATE_CODE", $"customer code '{NormalizedCode}' already exists");
        }

        var customer = new Customer
        {
            CustomerCode = NormalizedCode,
            Name = request.Name.Trim(),
            TaxCode = request.TaxCode?.Trim(),
            Address = request.Address?.Trim(),
            ContactEmail = request.ContactEmail?.Trim(),
            Status = "active"
        };

        m_context.Customers.Add(customer);
        await m_context.SaveChangesAsync();

        return ServiceResult<CustomerDto>.Success(MapToDto(customer), "customer created successfully");
    }

    public async Task<ServiceResult<CustomerDto>> UpdateCustomerAsync(int id, UpdateCustomerRequest request)
    {
        var customer = await m_context.Customers.FirstOrDefaultAsync(c => c.Id == id);
        if (customer == null)
        {
            return ServiceResult<CustomerDto>.Failure("CUS_NOT_FOUND", $"customer with id {id} not found");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return ServiceResult<CustomerDto>.Failure("CUS_INVALID_NAME", "customer name cannot be empty");
        }

        var AllowedStatuses = new[] { "active", "suspended" };
        if (!AllowedStatuses.Contains(request.Status))
        {
            return ServiceResult<CustomerDto>.Failure("CUS_INVALID_STATUS", "customer status must be either 'active' or 'suspended'");
        }

        customer.Name = request.Name.Trim();
        customer.TaxCode = request.TaxCode?.Trim();
        customer.Address = request.Address?.Trim();
        customer.ContactEmail = request.ContactEmail?.Trim();
        customer.Status = request.Status;

        await m_context.SaveChangesAsync();

        return ServiceResult<CustomerDto>.Success(MapToDto(customer), "customer updated successfully");
    }

    private static CustomerDto MapToDto(Customer c) => new()
    {
        Id = c.Id,
        CustomerCode = c.CustomerCode,
        Name = c.Name,
        TaxCode = c.TaxCode,
        Address = c.Address,
        ContactEmail = c.ContactEmail,
        Status = c.Status
    };
}