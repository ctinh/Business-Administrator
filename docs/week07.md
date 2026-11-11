# Week 07: Automated Testing Report & Defect Log

## 1. Test Architecture Overview

The `QTKD.Tests` xUnit project separates automated verification into two distinct suites (17 test executions in total):
1. **Unit Tests (`UnitTests/ContractRulesUnitTests.cs` — 9 tests):** Tests `ContractService` business rules (`CTR-01`, `CTR-02`, `CTR-03`) in isolation using `Microsoft.EntityFrameworkCore.InMemory`. Each business rule includes both positive (valid) and negative (invalid) test cases.
2. **Integration Tests (`IntegrationTests/QtkdApiIntegrationTests.cs` — 8 tests):** Uses `WebApplicationFactory<Program>` (`CustomWebApplicationFactory`) to execute end-to-end HTTP requests against an isolated PostgreSQL test database (`qtkd_test_db`), ensuring development data in `qtkd_db` is never modified. Every test verifies:
   - The HTTP status code (`200 OK`, `201 Created`, `400 Bad Request`, `409 Conflict`).
   - The returned `ServiceResult<T>` JSON body (`IsSuccess`, `ErrorCode`, `Message`, `Data`).
   - The actual persisted state in the PostgreSQL `qtkd_test_db` database after the API call.

---

## 2. Test Execution Matrix (TC01 – TC08)

All 8 required test cases (`TC01` – `TC08`) are **100% automated** (no manual-only fallback required):

| Test ID | Scenario / Operation | Expected Result | Execution Type | Test Method Name | Status |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **TC01** | Create a valid Customer | Created (`201 Created`) and readable from PostgreSQL DB | Automated (Integration) | `TC01_CreateValidCustomer_ShouldReturn201_AndPersistInDatabase` | **PASS** |
| **TC02** | Create Customer missing mandatory `Name` | Rejected (`400 Bad Request`, `CUS_INVALID_NAME`); not saved in DB | Automated (Integration) | `TC02_CreateCustomerMissingRequiredFields_ShouldReturn400_AndNotSaveToDatabase` | **PASS** |
| **TC03** | Update Contract in `draft` status | Succeeds (`200 OK`) when data is valid; updated in DB | Automated (Unit + Integration) | `TC03_UpdateContractInDraft_ShouldReturn200_AndUpdateDatabase` | **PASS** |
| **TC04** | Update Contract in `pending_approval` status | Rejected (`400 Bad Request`, `CTR-01`); DB record remains unchanged | Automated (Unit + Integration) | `TC04_UpdateContractInPendingApproval_ShouldReturn400Ctr01_AndKeepDataUnchanged` | **PASS** |
| **TC05** | Submit Contract missing `CTR-02` attachment | Rejected (`400 Bad Request`, `CTR-02`); status in DB remains `draft` | Automated (Unit + Integration) | `TC05_SubmitContractMissingCtr02Condition_ShouldReturn400Ctr02_AndRemainDraft` | **PASS** |
| **TC06** | Submit valid Contract | Succeeds (`200 OK`); transitions to `pending_approval` in DB | Automated (Unit + Integration) | `TC06_SubmitValidContract_ShouldReturn200_AndTransitionToPendingApprovalInDatabase` | **PASS** |
| **TC07** | Attempt direct transition from `draft` to `approved` | Rejected (`400 Bad Request`, `CTR-03`); status remains `draft` | Automated (Unit + Integration) | `TC07_AttemptDirectTransitionFromDraftToApproved_ShouldReturn400Ctr03_AndRemainDraft` | **PASS** |
| **TC08** | Submit the same Contract twice (Double Submit) | Rejected (`409 Conflict`, `CTR_ALREADY_SUBMITTED`); original `SubmittedAt` preserved | Automated (Integration) | `TC08_SubmitSameContractTwice_ShouldReturn409Conflict_AndPreserveOriginalTimestamp` | **PASS** |

---

## 3. Unit Test Breakdown (`CTR-01`, `CTR-02`, `CTR-03`)

| Business Rule | Test Case Type | Unit Test Method | Result |
| :--- | :--- | :--- | :--- |
| **CTR-01** | Valid (`draft`) | `CTR01_ValidCase_UpdateContract_WhenStatusIsDraftOrRevisionRequested_ShouldSucceed("draft")` | **PASS** |
| **CTR-01** | Valid (`revision_requested`) | `CTR01_ValidCase_UpdateContract_WhenStatusIsDraftOrRevisionRequested_ShouldSucceed("revision_requested")` | **PASS** |
| **CTR-01** | Invalid (`pending_approval`) | `CTR01_InvalidCase_UpdateContract_WhenStatusIsPendingApproval_ShouldRejectWithCtr01` | **PASS** |
| **CTR-02** | Valid (All fields valid) | `CTR02_ValidCase_SubmitContract_WhenAllMandatoryFieldsAreValid_ShouldSucceed` | **PASS** |
| **CTR-02** | Invalid (Missing attachment) | `CTR02_InvalidCase_SubmitContract_WhenAttachmentIsMissing_ShouldRejectWithCtr02` | **PASS** |
| **CTR-02** | Invalid (Suspended customer) | `CTR02_InvalidCase_SubmitContract_WhenCustomerIsSuspended_ShouldRejectWithCtr02` | **PASS** |
| **CTR-03** | Valid (Transitions to `pending_approval`) | `CTR03_ValidCase_SubmitContract_ShouldTransitionToPendingApproval_NeverDirectlyToApproved` | **PASS** |
| **CTR-03** | Invalid (Create directly as `approved`) | `CTR03_InvalidCase_CreateContract_WithApprovedStatus_ShouldRejectWithCtr03` | **PASS** |
| **CTR-03** | Invalid (Update directly to `approved`) | `CTR03_InvalidCase_UpdateContract_DirectTransitionToApproved_ShouldRejectWithCtr03` | **PASS** |

---

## 4. Defect Log & Bug Fix Record

### Defect #01: Case-Sensitivity in Status Validation and Incomplete Target Status Guard in `UpdateContractAsync`
- **How to Reproduce:**
  1. Call `PUT /api/contracts/{id}` on a `draft` contract while passing `"status": "active"` or `"status": "pending_approval"` in the request body, OR
  2. Have a contract or customer record with mixed-case status (e.g., `"Draft"` or `"Active"`) and invoke `PUT /api/contracts/{id}` or `POST /api/contracts/{id}/submit`.
- **Root Cause:**
  1. In Week 06, `UpdateContractAsync` only checked `request.Status.Equals("approved", StringComparison.OrdinalIgnoreCase)` for `CTR-03`, overlooking other non-draft lifecycle states (`"active"`, `"pending_approval"`).
  2. Status array checks (`editableStatuses.Contains(contract.Status)` and `SubmittableStatuses.Contains(contract.Status)`) used default case-sensitive comparison.
- **Resolution / Fix Commit:**
  - **Commit Hash:** `<YOUR_BUGFIX_COMMIT_HASH>` (`fix(contract): enforce case-insensitive status checks and block invalid target statuses in CTR-01 and CTR-03`)
  - Updated `ContractService.UpdateContractAsync` in `QTKD.Api/Services/IContractService.cs` to block any direct transition to `"approved"`, `"active"`, or `"pending_approval"` under rule `CTR-03`.
  - Applied `StringComparer.OrdinalIgnoreCase` and `StringComparison.OrdinalIgnoreCase` across all status checks in `CreateContractAsync`, `UpdateContractAsync`, and `SubmitContractAsync`.
- **Verification:** Re-ran `dotnet test`; all 17 automated tests passed (`Failed: 0, Passed: 17`).