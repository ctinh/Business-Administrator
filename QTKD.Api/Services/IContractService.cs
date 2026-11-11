using Microsoft.EntityFrameworkCore;
using QTKD.Api.Data;
using QTKD.Api.DTOs;
using QTKD.Api.Entities;

namespace QTKD.Api.Services;

public interface IContractService
{
    Task<IEnumerable<ContractDetailDto>> GetAllContractsAsync();
    Task<ContractDetailDto?> GetContractByIdAsync(int id);
    Task<ServiceResult<ContractDetailDto>> CreateContractAsync(CreateContractRequest request);
    Task<ServiceResult<ContractDetailDto>> UpdateContractAsync(int id, UpdateContractRequest request);
    Task<ServiceResult<ContractDetailDto>> SubmitContractAsync(int id);
}

public class ContractService : IContractService
{
    private readonly QtkdDbContext m_context;

    public ContractService(QtkdDbContext context)
    {
        m_context = context;
    }

    public async Task<IEnumerable<ContractDetailDto>> GetAllContractsAsync()
    {
        return await m_context.Contracts
            .AsNoTracking()
            .Include(c => c.Customer)
            .OrderBy(c => c.Id)
            .Select(c => MapToDto(c))
            .ToListAsync();
    }

    public async Task<ContractDetailDto?> GetContractByIdAsync(int id)
    {
        var contract = await m_context.Contracts
            .AsNoTracking()
            .Include(c => c.Customer)
            .FirstOrDefaultAsync(c => c.Id == id);

        return contract == null ? null : MapToDto(contract);
    }

    public async Task<ServiceResult<ContractDetailDto>> CreateContractAsync(CreateContractRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ContractNo))
        {
            return ServiceResult<ContractDetailDto>.Failure("CTR_INVALID_NO", "contract number is required");
        }

        // CRT-03 validation: creating a new contract directly with the approved status is not allowed
        if (!string.IsNullOrWhiteSpace(request.Status) &&
            !request.Status.Equals("draft", StringComparison.OrdinalIgnoreCase))
        {
            return ServiceResult<ContractDetailDto>.Failure(
                "CTR-03",
                "CTR-03 violation: newly created contracts must have the 'draft' status and cannot be directly set to 'approved' or any other status");
        }

        // validate that the referenced customer exists and is active
        var customer = await m_context.Customers.FirstOrDefaultAsync(c => c.Id == request.CustomerId);
        if (customer == null)
        {
            return ServiceResult<ContractDetailDto>.Failure("CTR_INVALID_CUSTOMER", $"customer with id {request.CustomerId} does not exist");
        }

        if (customer.Status != "active")
        {
            return ServiceResult<ContractDetailDto>.Failure("CTR_INACTIVE_CUSTOMER", $"customer '{customer.Name}' is not in active status");
        }

        // validate the effective date
        if (request.StartDate > request.EndDate)
        {
            return ServiceResult<ContractDetailDto>.Failure("CTR_INVALID_DATES", "start date must not be later than end date");
        }

        if (request.ContractValue < 0)
        {
            return ServiceResult<ContractDetailDto>.Failure("CTR_INVALID_VALUE", "contract value cannot be negative");
        }

        var NormalizedNo = request.ContractNo.Trim().ToUpperInvariant();
        if (await m_context.Contracts.AnyAsync(c => c.ContractNo == NormalizedNo))
        {
            return ServiceResult<ContractDetailDto>.Failure("CTR_DUPLICATE_NO", $"contract number '{NormalizedNo}' already exists");
        }

        var contract = new Contract
        {
            ContractNo = NormalizedNo,
            CustomerId = request.CustomerId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            ContractValue = request.ContractValue,
            ServiceTerms = request.ServiceTerms?.Trim(),
            AttachmentUrl = request.AttachmentUrl?.Trim(),
            Status = "draft"
        };

        m_context.Contracts.Add(contract);
        await m_context.SaveChangesAsync();

        contract.Customer = customer;
        return ServiceResult<ContractDetailDto>.Success(MapToDto(contract), "draft contract created successfully");
    }

    public async Task<ServiceResult<ContractDetailDto>> UpdateContractAsync(int id, UpdateContractRequest request)
    {
        var contract = await m_context.Contracts
            .Include(c => c.Customer)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (contract == null)
        {
            return ServiceResult<ContractDetailDto>.Failure("CTR_NOT_FOUND", $"contract with id {id} not found");
        }

        // CTR-03: direct status transition from draft to approved, active, or pending_approval via update is not allowed
        var ForbiddenTargetStatuses = new[] { "approved", "active", "pending_approval" };
        if (!string.IsNullOrWhiteSpace(request.Status) && ForbiddenTargetStatuses.Contains(request.Status.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            return ServiceResult<ContractDetailDto>.Failure(
                "CTR-03",
                "CTR-03 violation: contracts cannot be moved directly to 'approved', 'active', or 'pending_approval', they must go through the submission process first");
        }

        // CTR-01: contracts can only be edited when their status is 'draft' or 'revision_requested'.
        var editableStatuses = new[] { "draft", "revision_requested" };
        if (!editableStatuses.Contains(contract.Status, StringComparer.OrdinalIgnoreCase))
        {
            return ServiceResult<ContractDetailDto>.Failure(
                "CTR-01",
                $"CTR-01 violation: cannot edit a contract with status '{contract.Status}', editing is only allowed when the status is 'draft' or 'revision_requested'");
        }

        // validate the customer
        var customer = await m_context.Customers.FirstOrDefaultAsync(c => c.Id == request.CustomerId);
        if (customer == null || !customer.Status.Equals("active", StringComparison.OrdinalIgnoreCase))
        {
            return ServiceResult<ContractDetailDto>.Failure(
                "CTR_INVALID_CUSTOMER",
                "the referenced customer does not exist or is not in 'active' status");
        }

        // validate the effective date
        if (request.StartDate > request.EndDate)
        {
            return ServiceResult<ContractDetailDto>.Failure(
                "CTR_INVALID_DATES",
                "start date must not be later than end date");
        }

        if (request.ContractValue < 0)
        {
            return ServiceResult<ContractDetailDto>.Failure("CTR_INVALID_VALUE", "contract value cannot be negative");
        }

        contract.CustomerId = request.CustomerId;
        contract.Customer = customer;
        contract.StartDate = request.StartDate;
        contract.EndDate = request.EndDate;
        contract.ContractValue = request.ContractValue;
        contract.ServiceTerms = request.ServiceTerms?.Trim();
        contract.AttachmentUrl = request.AttachmentUrl?.Trim();

        await m_context.SaveChangesAsync();

        return ServiceResult<ContractDetailDto>.Success(MapToDto(contract), "contract updated successfully");
    }

    // US03: submit contract for approval: post /api/contracts/{id}/submit
    public async Task<ServiceResult<ContractDetailDto>> SubmitContractAsync(int id)
    {
        var contract = await m_context.Contracts
            .Include(c => c.Customer)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (contract == null)
        {
            return ServiceResult<ContractDetailDto>.Failure("CTR_NOT_FOUND", $"contract with id {id} not found.");
        }

        // double submit handling: if the contract is already in 'pending_approval' status, reject the duplicate submission attempt
        if (contract.Status.Equals("pending_approval", StringComparison.OrdinalIgnoreCase))
        {
            return ServiceResult<ContractDetailDto>.Failure(
                "CTR_ALREADY_SUBMITTED",
                $"the contract '{contract.ContractNo}' was already submitted for approval at {contract.SubmittedAt:yyyy-MM-dd HH:mm:ss} UTC and is currently pending approval");
        }

        // validate the contract status before submission (only 'draft' or 'revision_requested' are allowed)
        var SubmittableStatuses = new[] { "draft", "revision_requested" };
        if (!SubmittableStatuses.Contains(contract.Status, StringComparer.OrdinalIgnoreCase))
        {
            return ServiceResult<ContractDetailDto>.Failure(
                "CTR_INVALID_STATUS_FOR_SUBMIT",
                $"contracts can only be submitted for approval when their status is 'draft' or 'revision_requested' (current status: '{contract.Status}')");
        }

        // CTR-02: perform comprehensive validation: ensure the customer is valid, the effective period is valid, the contract value is greater than zero, and all required content/documents are provided
        if (contract.Customer == null || !contract.Customer.Status.Equals("active", StringComparison.OrdinalIgnoreCase))
        {
            return ServiceResult<ContractDetailDto>.Failure(
                "CTR-02",
                "CTR-02 violation: the contract's customer does not exist or is no longer in 'active' status");
        }

        if (contract.StartDate > contract.EndDate)
        {
            return ServiceResult<ContractDetailDto>.Failure(
                "CTR-02",
                "CTR-02 violation: the contract's effective period is invalid (start date > end date)");
        }

        if (contract.ContractValue <= 0)
        {
            return ServiceResult<ContractDetailDto>.Failure(
                "CTR-02",
                "CTR-02 violation: the contract value must be greater than 0 before submission for approval");
        }

        if (string.IsNullOrWhiteSpace(contract.ServiceTerms))
        {
            return ServiceResult<ContractDetailDto>.Failure(
                "CTR-02",
                "CTR-02 violation: required service terms are missing before submission for approval");
        }

        if (string.IsNullOrWhiteSpace(contract.AttachmentUrl))
        {
            return ServiceResult<ContractDetailDto>.Failure(
                "CTR-02",
                "CTR-02 violation: the required contract attachment is missing before submission for approval");
        }

        // CTR-03 RULE: a successful submission must transition the contract to 'pending_approval' only, it must never transition directly to 'approved'
        contract.Status = "pending_approval";
        contract.SubmittedAt = DateTime.UtcNow;

        await m_context.SaveChangesAsync();

        return ServiceResult<ContractDetailDto>.Success(
            MapToDto(contract),
            "contract submitted for approval successfully, status has been changed to 'pending_approval'");
    }

    private static ContractDetailDto MapToDto(Contract c) => new()
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
        Status = c.Status,
        SubmittedAt = c.SubmittedAt
    };
}