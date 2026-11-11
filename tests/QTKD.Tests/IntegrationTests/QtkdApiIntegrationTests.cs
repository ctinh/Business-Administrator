using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QTKD.Api.Data;
using QTKD.Api.DTOs;
using Xunit;

namespace QTKD.Tests.IntegrationTests;

public class QtkdApiIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory m_factory;
    private readonly HttpClient m_client;

    public QtkdApiIntegrationTests(CustomWebApplicationFactory factory)
    {
        m_factory = factory;
        m_client = factory.CreateClient();
    }

    // TC01: Create a valid Customer -> Created and readable from DB
    
    [Fact]
    public async Task TC01_CreateValidCustomer_ShouldReturn201_AndPersistInDatabase()
    {
        // arrange
        var request = new CreateCustomerRequest
        {
            CustomerCode = "KH-TC01",
            Name = "TC01 Port Logistics Co., Ltd.",
            TaxCode = "0311112222",
            Address = "District 7, HCMC",
            ContactEmail = "tc01@portlogistics.vn"
        };

        // act
        var response = await m_client.PostAsJsonAsync("/api/customers", request);
        var result = await response.Content.ReadFromJsonAsync<ServiceResult<CustomerDto>>();

        // assert 1: HTTP status & response payload
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("KH-TC01", result.Data.CustomerCode);
        Assert.Equal("active", result.Data.Status);

        // assert 2: read back via GET API and verify directly in PostgreSQL test DB
        var getResponse = await m_client.GetAsync($"/api/customers/{result.Data.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        using var scope = m_factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QtkdDbContext>();
        var dbCustomer = await db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == result.Data.Id);
        Assert.NotNull(dbCustomer);
        Assert.Equal("TC01 Port Logistics Co., Ltd.", dbCustomer.Name);
        Assert.Equal("active", dbCustomer.Status);
    }

    // TC02: Create Customer missing required info -> Rejected with error message
    
    [Fact]
    public async Task TC02_CreateCustomerMissingRequiredFields_ShouldReturn400_AndNotSaveToDatabase()
    {
        // arrange
        var invalidRequest = new CreateCustomerRequest
        {
            CustomerCode = "KH-TC02",
            Name = "", // missing required Name
            TaxCode = "0319998888"
        };

        // act
        var response = await m_client.PostAsJsonAsync("/api/customers", invalidRequest);
        var result = await response.Content.ReadFromJsonAsync<ServiceResult<CustomerDto>>();

        // assert 1: HTTP status & error payload
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(result);
        Assert.False(result.IsSuccess);
        Assert.Equal("CUS_INVALID_NAME", result.ErrorCode);
        Assert.False(string.IsNullOrWhiteSpace(result.Message));

        // assert 2: verify record was NOT created in PostgreSQL test DB
        using var scope = m_factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QtkdDbContext>();
        var exists = await db.Customers.AnyAsync(c => c.CustomerCode == "KH-TC02");
        Assert.False(exists);
    }

    // TC03: Update Contract in Draft status -> Succeeds when data is valid
    
    [Fact]
    public async Task TC03_UpdateContractInDraft_ShouldReturn200_AndUpdateDatabase()
    {
        // arrange: create a new draft contract first
        var createReq = new CreateContractRequest
        {
            ContractNo = "HD-TC03-2026",
            CustomerId = 1,
            StartDate = new DateOnly(2026, 6, 1),
            EndDate = new DateOnly(2026, 12, 31),
            ContractValue = 120000000m,
            ServiceTerms = "initial draft terms",
            AttachmentUrl = null,
            Status = "draft"
        };
        var createResp = await m_client.PostAsJsonAsync("/api/contracts", createReq);
        var created = await createResp.Content.ReadFromJsonAsync<ServiceResult<ContractDetailDto>>();
        int contractId = created!.Data!.Id;

        var updateReq = new UpdateContractRequest
        {
            CustomerId = 1,
            StartDate = new DateOnly(2026, 6, 1),
            EndDate = new DateOnly(2026, 12, 31),
            ContractValue = 195000000m,
            ServiceTerms = "updated container handling terms",
            AttachmentUrl = "https://storage.abc-logistics.vn/contracts/HD-TC03-2026.pdf",
            Status = "draft"
        };

        // act
        var updateResp = await m_client.PutAsJsonAsync($"/api/contracts/{contractId}", updateReq);
        var updateResult = await updateResp.Content.ReadFromJsonAsync<ServiceResult<ContractDetailDto>>();

        // assert 1: HTTP status & response body
        Assert.Equal(HttpStatusCode.OK, updateResp.StatusCode);
        Assert.NotNull(updateResult);
        Assert.True(updateResult.IsSuccess);
        Assert.Equal(195000000m, updateResult.Data!.ContractValue);

        // assert 2: verify updated fields in PostgreSQL test DB
        using var scope = m_factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QtkdDbContext>();
        var dbContract = await db.Contracts.AsNoTracking().FirstAsync(c => c.Id == contractId);
        Assert.Equal(195000000m, dbContract.ContractValue);
        Assert.Equal("updated container handling terms", dbContract.ServiceTerms);
        Assert.Equal("https://storage.abc-logistics.vn/contracts/HD-TC03-2026.pdf", dbContract.AttachmentUrl);
    }

    // TC04: Update Contract in PendingApproval -> Rejected; data unchanged
    
    [Fact]
    public async Task TC04_UpdateContractInPendingApproval_ShouldReturn400Ctr01_AndKeepDataUnchanged()
    {
        // arrange: create and submit a valid contract so its status becomes 'pending_approval'
        var createReq = new CreateContractRequest
        {
            ContractNo = "HD-TC04-2026",
            CustomerId = 1,
            StartDate = new DateOnly(2026, 6, 1),
            EndDate = new DateOnly(2026, 12, 31),
            ContractValue = 200000000m,
            ServiceTerms = "original approved-ready terms",
            AttachmentUrl = "https://storage.abc-logistics.vn/contracts/HD-TC04-2026.pdf",
            Status = "draft"
        };
        var createResp = await m_client.PostAsJsonAsync("/api/contracts", createReq);
        var created = await createResp.Content.ReadFromJsonAsync<ServiceResult<ContractDetailDto>>();
        int contractId = created!.Data!.Id;

        await m_client.PostAsync($"/api/contracts/{contractId}/submit", null);

        // attempt to modify the contract while it is in 'pending_approval'
        var illegalUpdateReq = new UpdateContractRequest
        {
            CustomerId = 1,
            StartDate = new DateOnly(2026, 6, 1),
            EndDate = new DateOnly(2026, 12, 31),
            ContractValue = 999999999m,
            ServiceTerms = "tampered terms",
            AttachmentUrl = "https://storage.abc-logistics.vn/contracts/HD-TC04-2026.pdf",
            Status = "draft"
        };

        // act
        var updateResp = await m_client.PutAsJsonAsync($"/api/contracts/{contractId}", illegalUpdateReq);
        var updateResult = await updateResp.Content.ReadFromJsonAsync<ServiceResult<ContractDetailDto>>();

        // assert 1: HTTP 400 & CTR-01 error code
        Assert.Equal(HttpStatusCode.BadRequest, updateResp.StatusCode);
        Assert.NotNull(updateResult);
        Assert.False(updateResult.IsSuccess);
        Assert.Equal("CTR-01", updateResult.ErrorCode);

        // assert 2: verify data in PostgreSQL test DB remains untouched
        using var scope = m_factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QtkdDbContext>();
        var dbContract = await db.Contracts.AsNoTracking().FirstAsync(c => c.Id == contractId);
        Assert.Equal(200000000m, dbContract.ContractValue);
        Assert.Equal("original approved-ready terms", dbContract.ServiceTerms);
        Assert.Equal("pending_approval", dbContract.Status);
    }

    // TC05: Submit Contract missing CTR-02 condition -> Rejected; remains Draft
    
    [Fact]
    public async Task TC05_SubmitContractMissingCtr02Condition_ShouldReturn400Ctr02_AndRemainDraft()
    {
        // arrange: create draft contract without attachmentUrl
        var createReq = new CreateContractRequest
        {
            ContractNo = "HD-TC05-2026",
            CustomerId = 1,
            StartDate = new DateOnly(2026, 6, 1),
            EndDate = new DateOnly(2026, 12, 31),
            ContractValue = 175000000m,
            ServiceTerms = "valid service terms",
            AttachmentUrl = null, // missing attachment violates CTR-02
            Status = "draft"
        };
        var createResp = await m_client.PostAsJsonAsync("/api/contracts", createReq);
        var created = await createResp.Content.ReadFromJsonAsync<ServiceResult<ContractDetailDto>>();
        int contractId = created!.Data!.Id;

        // act
        var submitResp = await m_client.PostAsync($"/api/contracts/{contractId}/submit", null);
        var submitResult = await submitResp.Content.ReadFromJsonAsync<ServiceResult<ContractDetailDto>>();

        // assert 1: HTTP 400 & CTR-02 error code
        Assert.Equal(HttpStatusCode.BadRequest, submitResp.StatusCode);
        Assert.NotNull(submitResult);
        Assert.False(submitResult.IsSuccess);
        Assert.Equal("CTR-02", submitResult.ErrorCode);

        // assert 2: verify contract in PostgreSQL test DB is still 'draft' and SubmittedAt is null
        using var scope = m_factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QtkdDbContext>();
        var dbContract = await db.Contracts.AsNoTracking().FirstAsync(c => c.Id == contractId);
        Assert.Equal("draft", dbContract.Status);
        Assert.Null(dbContract.SubmittedAt);
    }

    // TC06: Submit valid Contract -> Transitions to PendingApproval
    
    [Fact]
    public async Task TC06_SubmitValidContract_ShouldReturn200_AndTransitionToPendingApprovalInDatabase()
    {
        // arrange
        var createReq = new CreateContractRequest
        {
            ContractNo = "HD-TC06-2026",
            CustomerId = 1,
            StartDate = new DateOnly(2026, 6, 1),
            EndDate = new DateOnly(2026, 12, 31),
            ContractValue = 300000000m,
            ServiceTerms = "complete logistics service terms",
            AttachmentUrl = "https://storage.abc-logistics.vn/contracts/HD-TC06-2026.pdf",
            Status = "draft"
        };
        var createResp = await m_client.PostAsJsonAsync("/api/contracts", createReq);
        var created = await createResp.Content.ReadFromJsonAsync<ServiceResult<ContractDetailDto>>();
        int contractId = created!.Data!.Id;

        // act
        var submitResp = await m_client.PostAsync($"/api/contracts/{contractId}/submit", null);
        var submitResult = await submitResp.Content.ReadFromJsonAsync<ServiceResult<ContractDetailDto>>();

        // assert 1: HTTP 200 & response status
        Assert.Equal(HttpStatusCode.OK, submitResp.StatusCode);
        Assert.NotNull(submitResult);
        Assert.True(submitResult.IsSuccess);
        Assert.Equal("pending_approval", submitResult.Data!.Status);
        Assert.NotNull(submitResult.Data.SubmittedAt);

        // assert 2: verify persisted state in PostgreSQL test DB
        using var scope = m_factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QtkdDbContext>();
        var dbContract = await db.Contracts.AsNoTracking().FirstAsync(c => c.Id == contractId);
        Assert.Equal("pending_approval", dbContract.Status);
        Assert.NotNull(dbContract.SubmittedAt);
    }

    // TC07: Attempt direct transition from Draft to Approved -> Rejected
    
    [Fact]
    public async Task TC07_AttemptDirectTransitionFromDraftToApproved_ShouldReturn400Ctr03_AndRemainDraft()
    {
        // arrange
        var createReq = new CreateContractRequest
        {
            ContractNo = "HD-TC07-2026",
            CustomerId = 1,
            StartDate = new DateOnly(2026, 6, 1),
            EndDate = new DateOnly(2026, 12, 31),
            ContractValue = 150000000m,
            ServiceTerms = "standard terms",
            AttachmentUrl = "https://storage.abc-logistics.vn/contracts/HD-TC07-2026.pdf",
            Status = "draft"
        };
        var createResp = await m_client.PostAsJsonAsync("/api/contracts", createReq);
        var created = await createResp.Content.ReadFromJsonAsync<ServiceResult<ContractDetailDto>>();
        int contractId = created!.Data!.Id;

        var directApproveReq = new UpdateContractRequest
        {
            CustomerId = 1,
            StartDate = new DateOnly(2026, 6, 1),
            EndDate = new DateOnly(2026, 12, 31),
            ContractValue = 150000000m,
            ServiceTerms = "standard terms",
            AttachmentUrl = "https://storage.abc-logistics.vn/contracts/HD-TC07-2026.pdf",
            Status = "approved" // violates CTR-03
        };

        // act
        var updateResp = await m_client.PutAsJsonAsync($"/api/contracts/{contractId}", directApproveReq);
        var updateResult = await updateResp.Content.ReadFromJsonAsync<ServiceResult<ContractDetailDto>>();

        // assert 1: HTTP 400 & CTR-03 error code
        Assert.Equal(HttpStatusCode.BadRequest, updateResp.StatusCode);
        Assert.NotNull(updateResult);
        Assert.False(updateResult.IsSuccess);
        Assert.Equal("CTR-03", updateResult.ErrorCode);

        // assert 2: verify status in PostgreSQL test DB is still 'draft'
        using var scope = m_factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QtkdDbContext>();
        var dbContract = await db.Contracts.AsNoTracking().FirstAsync(c => c.Id == contractId);
        Assert.Equal("draft", dbContract.Status);
    }

    // TC08: Submit the same Contract twice -> Rejects duplicate processing
    
    [Fact]
    public async Task TC08_SubmitSameContractTwice_ShouldReturn409Conflict_AndPreserveOriginalTimestamp()
    {
        // arrange
        var createReq = new CreateContractRequest
        {
            ContractNo = "HD-TC08-2026",
            CustomerId = 1,
            StartDate = new DateOnly(2026, 6, 1),
            EndDate = new DateOnly(2026, 12, 31),
            ContractValue = 220000000m,
            ServiceTerms = "warehouse and container services",
            AttachmentUrl = "https://storage.abc-logistics.vn/contracts/HD-TC08-2026.pdf",
            Status = "draft"
        };
        var createResp = await m_client.PostAsJsonAsync("/api/contracts", createReq);
        var created = await createResp.Content.ReadFromJsonAsync<ServiceResult<ContractDetailDto>>();
        int contractId = created!.Data!.Id;

        // first submission (succeeds)
        var firstSubmitResp = await m_client.PostAsync($"/api/contracts/{contractId}/submit", null);
        var firstResult = await firstSubmitResp.Content.ReadFromJsonAsync<ServiceResult<ContractDetailDto>>();
        Assert.Equal(HttpStatusCode.OK, firstSubmitResp.StatusCode);
        var originalSubmittedAt = firstResult!.Data!.SubmittedAt;

        // act: second submission on the same contract
        var secondSubmitResp = await m_client.PostAsync($"/api/contracts/{contractId}/submit", null);
        var secondResult = await secondSubmitResp.Content.ReadFromJsonAsync<ServiceResult<ContractDetailDto>>();

        // assert 1: HTTP 409 Conflict & CTR_ALREADY_SUBMITTED error code
        Assert.Equal(HttpStatusCode.Conflict, secondSubmitResp.StatusCode);
        Assert.NotNull(secondResult);
        Assert.False(secondResult.IsSuccess);
        Assert.Equal("CTR_ALREADY_SUBMITTED", secondResult.ErrorCode);

        // assert 2: verify SubmittedAt timestamp in PostgreSQL test DB was not overwritten
        using var scope = m_factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QtkdDbContext>();
        var dbContract = await db.Contracts.AsNoTracking().FirstAsync(c => c.Id == contractId);
        Assert.Equal("pending_approval", dbContract.Status);
        Assert.NotNull(dbContract.SubmittedAt);
        Assert.Equal(originalSubmittedAt.Value.ToString("yyyy-MM-dd HH:mm:ss"), dbContract.SubmittedAt.Value.ToString("yyyy-MM-dd HH:mm:ss"));
    }
}