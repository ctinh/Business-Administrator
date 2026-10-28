using Microsoft.EntityFrameworkCore;
using QTKD.Api.Data;
using QTKD.Api.DTOs;

namespace QTKD.Api.Services;

public interface IContractService
{
    Task<ContractDetailDto?> GetContractByIdAsync(int id);
}

public class ContractService : IContractService
{
    private readonly QtkdDbContext m_context;

    public ContractService(QtkdDbContext context)
    {
        m_context = context;
    }

    public async Task<ContractDetailDto?> GetContractByIdAsync(int id)
    {
        return await m_context.Contracts
            .AsNoTracking()
            .Include(c => c.Customer)
            .Where(c => c.Id == id)
            .Select(c => new ContractDetailDto
            {
                Id = c.Id,
                ContractNo = c.ContractNo,
                CustomerId = c.CustomerId,
                CustomerName = c.Customer != null ? c.Customer.Name : string.Empty,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                ContractValue = c.ContractValue,
                ServiceTerms = c.ServiceTerms,
                AttachmentUrl = c.AttachmentUrl,
                Status = c.Status
            })
            .FirstOrDefaultAsync();
    }
}