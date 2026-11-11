using Microsoft.EntityFrameworkCore;
using QTKD.Api.Data;
using QTKD.Api.DTOs;
using QTKD.Api.Entities;
using QTKD.Api.Services;

namespace QTKD.Tests.UnitTests;

public class ContractRulesUnitTests
{
    private static QtkdDbContext CreateIsolatedDbContext()
    {
        var options = new DbContextOptionsBuilder<QtkdDbContext>()
            .UseInMemoryDatabase(databaseName: $"qtkd_unit_db_{Guid.NewGuid()}")
            .Options;

        var context = new QtkdDbContext(options);

        // seed active and suspended customers for unit testing
        context.Customers.AddRange(
            new Customer
            {
                Id = 1,
                CustomerCode = "CUS-001",
                Name = "Minh Phat Co., Ltd.",
                Status = "active"
            },
            new Customer
            {
                Id = 2,
                CustomerCode = "CUS-002",
                Name = "Inactive Logistics Co.",
                Status = "suspended"
            }
        );

        context.SaveChanges();
        return context;
    }

    // CTR-01 UNIT TESTS: only allow editing in 'draft' or 'revision_requested'

    [Theory]
    [InlineData("draft")]
    [InlineData("revision_requested")]
    public async Task CTR01_ValidCase_UpdateContract_WhenStatusIsDraftOrRevisionRequested_ShouldSucceed(string initialStatus)
    {
        // arrange
        using var context = CreateIsolatedDbContext();
        var contract = new Contract
        {
            Id = 10,
            ContractNo = "HD-UNIT-001",
            CustomerId = 1,
            StartDate = new DateOnly(2026, 6, 1),
            EndDate = new DateOnly(2026, 12, 31),
            ContractValue = 100000000m,
            ServiceTerms = "initial terms",
            Status = initialStatus
        };
        context.Contracts.Add(contract);
        await context.SaveChangesAsync();

        var service = new ContractService(context);
        var updateRequest = new UpdateContractRequest
        {
            CustomerId = 1,
            StartDate = new DateOnly(2026, 6, 1),
            EndDate = new DateOnly(2026, 12, 31),
            ContractValue = 180000000m,
            ServiceTerms = "updated service terms",
            AttachmentUrl = "https://storage.abc-logistics.vn/contracts/HD-UNIT-001.pdf",
            Status = initialStatus
        };

        // act
        var result = await service.UpdateContractAsync(10, updateRequest);

        // assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(180000000m, result.Data.ContractValue);
        Assert.Equal("updated service terms", result.Data.ServiceTerms);
    }

    [Fact]
    public async Task CTR01_InvalidCase_UpdateContract_WhenStatusIsPendingApproval_ShouldRejectWithCtr01()
    {
        // arrange
        using var context = CreateIsolatedDbContext();
        var contract = new Contract
        {
            Id = 11,
            ContractNo = "HD-UNIT-002",
            CustomerId = 1,
            StartDate = new DateOnly(2026, 6, 1),
            EndDate = new DateOnly(2026, 12, 31),
            ContractValue = 100000000m,
            ServiceTerms = "locked terms",
            AttachmentUrl = "https://storage.abc-logistics.vn/contracts/HD-UNIT-002.pdf",
            Status = "pending_approval",
            SubmittedAt = DateTime.UtcNow
        };
        context.Contracts.Add(contract);
        await context.SaveChangesAsync();

        var service = new ContractService(context);
        var updateRequest = new UpdateContractRequest
        {
            CustomerId = 1,
            StartDate = new DateOnly(2026, 6, 1),
            EndDate = new DateOnly(2026, 12, 31),
            ContractValue = 999000000m,
            ServiceTerms = "unauthorized modification",
            AttachmentUrl = "https://storage.abc-logistics.vn/contracts/HD-UNIT-002.pdf",
            Status = "draft"
        };

        // act
        var result = await service.UpdateContractAsync(11, updateRequest);

        // assert
        Assert.False(result.IsSuccess);
        Assert.Equal("CTR-01", result.ErrorCode);

        // verify data in db remains unchanged
        var dbContract = await context.Contracts.FindAsync(11);
        Assert.Equal(100000000m, dbContract!.ContractValue);
        Assert.Equal("locked terms", dbContract.ServiceTerms);
        Assert.Equal("pending_approval", dbContract.Status);
    }

    // CTR-02 UNIT TESTS: validate customer, dates, value, terms & attachment

    [Fact]
    public async Task CTR02_ValidCase_SubmitContract_WhenAllMandatoryFieldsAreValid_ShouldSucceed()
    {
        // arrange
        using var context = CreateIsolatedDbContext();
        var contract = new Contract
        {
            Id = 20,
            ContractNo = "HD-UNIT-003",
            CustomerId = 1,
            StartDate = new DateOnly(2026, 6, 1),
            EndDate = new DateOnly(2026, 12, 31),
            ContractValue = 250000000m,
            ServiceTerms = "full port handling terms",
            AttachmentUrl = "https://storage.abc-logistics.vn/contracts/HD-UNIT-003.pdf",
            Status = "draft"
        };
        context.Contracts.Add(contract);
        await context.SaveChangesAsync();

        var service = new ContractService(context);

        // act
        var result = await service.SubmitContractAsync(20);

        // assert
        Assert.True(result.IsSuccess);
        Assert.Equal("pending_approval", result.Data!.Status);
        Assert.NotNull(result.Data.SubmittedAt);
    }

    [Fact]
    public async Task CTR02_InvalidCase_SubmitContract_WhenAttachmentIsMissing_ShouldRejectWithCtr02()
    {
        // arrange
        using var context = CreateIsolatedDbContext();
        var contract = new Contract
        {
            Id = 21,
            ContractNo = "HD-UNIT-004",
            CustomerId = 1,
            StartDate = new DateOnly(2026, 6, 1),
            EndDate = new DateOnly(2026, 12, 31),
            ContractValue = 250000000m,
            ServiceTerms = "full port handling terms",
            AttachmentUrl = null, // missing mandatory attachment
            Status = "draft"
        };
        context.Contracts.Add(contract);
        await context.SaveChangesAsync();

        var service = new ContractService(context);

        // act
        var result = await service.SubmitContractAsync(21);

        // assert
        Assert.False(result.IsSuccess);
        Assert.Equal("CTR-02", result.ErrorCode);

        // verify status remains draft
        var dbContract = await context.Contracts.FindAsync(21);
        Assert.Equal("draft", dbContract!.Status);
        Assert.Null(dbContract.SubmittedAt);
    }

    [Fact]
    public async Task CTR02_InvalidCase_SubmitContract_WhenCustomerIsSuspended_ShouldRejectWithCtr02()
    {
        // arrange
        using var context = CreateIsolatedDbContext();
        var contract = new Contract
        {
            Id = 22,
            ContractNo = "HD-UNIT-005",
            CustomerId = 2, // suspended customer
            StartDate = new DateOnly(2026, 6, 1),
            EndDate = new DateOnly(2026, 12, 31),
            ContractValue = 250000000m,
            ServiceTerms = "full port handling terms",
            AttachmentUrl = "https://storage.abc-logistics.vn/contracts/HD-UNIT-005.pdf",
            Status = "draft"
        };
        context.Contracts.Add(contract);
        await context.SaveChangesAsync();

        var service = new ContractService(context);

        // act
        var result = await service.SubmitContractAsync(22);

        // assert
        Assert.False(result.IsSuccess);
        Assert.Equal("CTR-02", result.ErrorCode);
    }

    // CTR-03 UNIT TESTS: prohibit direct transition from 'draft' to 'approved'

    [Fact]
    public async Task CTR03_ValidCase_SubmitContract_ShouldTransitionToPendingApproval_NeverDirectlyToApproved()
    {
        // arrange
        using var context = CreateIsolatedDbContext();
        var contract = new Contract
        {
            Id = 30,
            ContractNo = "HD-UNIT-006",
            CustomerId = 1,
            StartDate = new DateOnly(2026, 6, 1),
            EndDate = new DateOnly(2026, 12, 31),
            ContractValue = 150000000m,
            ServiceTerms = "container storage terms",
            AttachmentUrl = "https://storage.abc-logistics.vn/contracts/HD-UNIT-006.pdf",
            Status = "draft"
        };
        context.Contracts.Add(contract);
        await context.SaveChangesAsync();

        var service = new ContractService(context);

        // act
        var result = await service.SubmitContractAsync(30);

        // assert
        Assert.True(result.IsSuccess);
        Assert.Equal("pending_approval", result.Data!.Status);
        Assert.NotEqual("approved", result.Data.Status);
    }

    [Fact]
    public async Task CTR03_InvalidCase_CreateContract_WithApprovedStatus_ShouldRejectWithCtr03()
    {
        // arrange
        using var context = CreateIsolatedDbContext();
        var service = new ContractService(context);
        var createRequest = new CreateContractRequest
        {
            ContractNo = "HD-UNIT-007",
            CustomerId = 1,
            StartDate = new DateOnly(2026, 6, 1),
            EndDate = new DateOnly(2026, 12, 31),
            ContractValue = 150000000m,
            ServiceTerms = "container storage terms",
            AttachmentUrl = "https://storage.abc-logistics.vn/contracts/HD-UNIT-007.pdf",
            Status = "approved"
        };

        // act
        var result = await service.CreateContractAsync(createRequest);

        // assert
        Assert.False(result.IsSuccess);
        Assert.Equal("CTR-03", result.ErrorCode);
    }

    [Fact]
    public async Task CTR03_InvalidCase_UpdateContract_DirectTransitionToApproved_ShouldRejectWithCtr03()
    {
        // arrange
        using var context = CreateIsolatedDbContext();
        var contract = new Contract
        {
            Id = 31,
            ContractNo = "HD-UNIT-008",
            CustomerId = 1,
            StartDate = new DateOnly(2026, 6, 1),
            EndDate = new DateOnly(2026, 12, 31),
            ContractValue = 150000000m,
            ServiceTerms = "container storage terms",
            AttachmentUrl = "https://storage.abc-logistics.vn/contracts/HD-UNIT-008.pdf",
            Status = "draft"
        };
        context.Contracts.Add(contract);
        await context.SaveChangesAsync();

        var service = new ContractService(context);
        var updateRequest = new UpdateContractRequest
        {
            CustomerId = 1,
            StartDate = new DateOnly(2026, 6, 1),
            EndDate = new DateOnly(2026, 12, 31),
            ContractValue = 150000000m,
            ServiceTerms = "container storage terms",
            AttachmentUrl = "https://storage.abc-logistics.vn/contracts/HD-UNIT-008.pdf",
            Status = "approved"
        };

        // act
        var result = await service.UpdateContractAsync(31, updateRequest);

        // assert
        Assert.False(result.IsSuccess);
        Assert.Equal("CTR-03", result.ErrorCode);

        var dbContract = await context.Contracts.FindAsync(31);
        Assert.Equal("draft", dbContract!.Status);
    }
}