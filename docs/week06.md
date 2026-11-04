# Week 06: API Specification & Business Rules Mapping

## 1. US03 Specification (Submit Contract for Approval) & Simulation Scope

### 1.1. Pre-conditions, Validation Rules, and State Transitions
- **Allowed Pre-submission States:** A contract must exist and currently be in `draft` or `revision_requested` status.
- **Submission Validation Conditions (CTR-02):**
  1. **Valid Customer:** The referenced customer (`CustomerId`) must exist and be in `active` status.
  2. **Valid Effective Dates:** `StartDate` must be less than or equal to `EndDate` (`StartDate <= EndDate`).
  3. **Valid Contract Value:** `ContractValue` must be strictly greater than zero (`ContractValue > 0`).
  4. **Mandatory Content & Attachment:** Both `ServiceTerms` and `AttachmentUrl` must be non-empty strings.
- **Post-submission State (CTR-03):** When all validations pass, the contract status transitions to `pending_approval` and records the exact UTC timestamp in `SubmittedAt`. Direct transition from `draft` to `approved` is strictly prohibited.
- **Rejection Scenarios:**
  - Non-existent contract ID $\rightarrow$ `404 Not Found` (`CTR_NOT_FOUND`).
  - Contract is already in `pending_approval` $\rightarrow$ `409 Conflict` (`CTR_ALREADY_SUBMITTED`).
  - Contract is in another non-submittable status $\rightarrow$ `400 Bad Request` (`CTR_INVALID_STATUS_FOR_SUBMIT`).
  - Missing any required field or violating `CTR-02` $\rightarrow$ `400 Bad Request` (`CTR-02`); the contract remains unchanged in PostgreSQL.

### 1.2. Demo Assumptions & Current Version Limitations
- **Mandatory Document Assumption:** Because physical file upload storage is outside the mandatory scope of Phase 2 Week 06, a contract is considered to have complete mandatory content/attachments when both `ServiceTerms` and `AttachmentUrl` contain valid non-empty strings.
- **Approval Simulation & Idempotency (Double Submit Prevention):** The full multi-step approval engine is not yet implemented in Week 06. Submitting a contract simulates workflow initiation by transitioning `Status` to `pending_approval` and setting `SubmittedAt`. If a client submits the same contract a second time while it is in `pending_approval`, the service rejects the duplicate request with `409 Conflict` (`CTR_ALREADY_SUBMITTED`) without overwriting `SubmittedAt` or creating duplicate processing entries.

---

## 2. Business Rules to Source Code Mapping

| Rule Code | Rule Description | Source File | Method & Validation Logic |
| :--- | :--- | :--- | :--- |
| **US03** | Submit contract for approval | `QTKD.Api/Services/IContractService.cs` | `SubmitContractAsync(int id)`: Checks status (`draft`/`revision_requested`), validates `CTR-02`, sets `Status = "pending_approval"` and `SubmittedAt = DateTime.UtcNow`. |
| **CTR-01** | Only allow editing contracts in `draft` or `revision_requested` status | `QTKD.Api/Services/IContractService.cs` | `UpdateContractAsync(int id, UpdateContractRequest request)`: Verifies `editableStatuses.Contains(contract.Status)`. Rejects modifications when status is `pending_approval`. |
| **CTR-02** | Only allow submission when customer, dates, value, terms, and attachment are valid | `QTKD.Api/Services/IContractService.cs` | `SubmitContractAsync(int id)`: Checks `Customer.Status == "active"`, `StartDate <= EndDate`, `ContractValue > 0`, and non-empty `ServiceTerms` and `AttachmentUrl`. |
| **CTR-03** | Prohibit direct transition from `draft` to `approved` | `QTKD.Api/Services/IContractService.cs` | `CreateContractAsync` & `UpdateContractAsync` reject `request.Status == "approved"`; `SubmitContractAsync` only transitions to `pending_approval`. |
| **Double Submit** | Submitting the same contract twice must not create duplicate processing | `QTKD.Api/Services/IContractService.cs` | `SubmitContractAsync(int id)`: Checks `if (contract.Status == "pending_approval")` and returns `CTR_ALREADY_SUBMITTED` (`409 Conflict`). |
| **PRC-02** | Price list effective date must not be later than expiry date | `QTKD.Api/Services/IPriceListService.cs` | `CreatePriceListAsync(CreatePriceListRequest request)`: Checks `request.EffectiveDate > request.ExpiryDate.Value`. |

---

## 3. Standardized API Response Format (`ServiceResult<T>`)

All write and domain-validated endpoints return a unified JSON envelope defined in `QTKD.Api/DTOs/ServiceResult.cs`:

```json
{
  "isSuccess": true,
  "errorCode": null,
  "message": "operation completed successfully",
  "data": { ... }
}
```

---

## 4. API Reference & Sample Payloads (Week 06 Demo Flow)

### 4.1. Customer Endpoints

#### 1. Create Customer (`POST /api/customers`)
- **Sample Request:**
```json
{
  "customerCode": "KH-003",
  "name": "Tan Cang Transport Co., Ltd.",
  "taxCode": "0301122334",
  "address": "Binh Thanh District, HCMC",
  "contactEmail": "info@tancangtransport.vn"
}
```
- **Sample Response (`201 Created`):**
```json
{
  "isSuccess": true,
  "errorCode": null,
  "message": "customer created successfully",
  "data": {
    "id": 3,
    "customerCode": "KH-003",
    "name": "Tan Cang Transport Co., Ltd.",
    "taxCode": "0301122334",
    "address": "Binh Thanh District, HCMC",
    "contactEmail": "info@tancangtransport.vn",
    "status": "active"
  }
}
```

#### 2. Update Customer (`PUT /api/customers/{id}`)
- **Sample Request (`PUT /api/customers/3`):**
```json
{
  "name": "Tan Cang Logistics & Transport Co., Ltd.",
  "taxCode": "0301122334",
  "address": "Thu Duc City, HCMC",
  "contactEmail": "contact@tancangtransport.vn",
  "status": "active"
}
```

---

### 4.2. Contract Endpoints & US03 Submission Flow

#### 1. Create Draft Contract Missing Attachment (`POST /api/contracts`)
- **Sample Request:**
```json
{
  "contractNo": "HD-2026-019",
  "customerId": 3,
  "startDate": "2026-06-01",
  "endDate": "2026-12-31",
  "contractValue": 250000000,
  "serviceTerms": "reefer container handling and storage services",
  "attachmentUrl": null,
  "status": "draft"
}
```
- **Sample Response (`201 Created`):**
```json
{
  "isSuccess": true,
  "errorCode": null,
  "message": "draft contract created successfully",
  "data": {
    "id": 2,
    "contractNo": "HD-2026-019",
    "customerId": 3,
    "customerName": "Tan Cang Transport Co., Ltd.",
    "startDate": "2026-06-01",
    "endDate": "2026-12-31",
    "contractValue": 250000000,
    "serviceTerms": "reefer container handling and storage services",
    "attachmentUrl": null,
    "status": "draft",
    "submittedAt": null
  }
}
```

#### 2. Submit Contract with Missing Attachment — CTR-02 Failure (`POST /api/contracts/2/submit`)
- **Sample Response (`400 Bad Request`):**
```json
{
  "isSuccess": false,
  "errorCode": "CTR-02",
  "message": "CTR-02 violation: the required contract attachment is missing before submission for approval",
  "data": null
}
```

#### 3. Update Draft Contract to Supply Missing Attachment (`PUT /api/contracts/2`)
- **Sample Request:**
```json
{
  "customerId": 3,
  "startDate": "2026-06-01",
  "endDate": "2026-12-31",
  "contractValue": 250000000,
  "serviceTerms": "reefer container handling and storage services",
  "attachmentUrl": "[https://storage.abc-logistics.vn/contracts/HD-2026-019.pdf](https://storage.abc-logistics.vn/contracts/HD-2026-019.pdf)",
  "status": "draft"
}
```
- **Sample Response (`200 OK`):**
```json
{
  "isSuccess": true,
  "errorCode": null,
  "message": "contract updated successfully",
  "data": {
    "id": 2,
    "contractNo": "HD-2026-019",
    "customerId": 3,
    "customerName": "Tan Cang Transport Co., Ltd.",
    "startDate": "2026-06-01",
    "endDate": "2026-12-31",
    "contractValue": 250000000,
    "serviceTerms": "reefer container handling and storage services",
    "attachmentUrl": "[https://storage.abc-logistics.vn/contracts/HD-2026-019.pdf](https://storage.abc-logistics.vn/contracts/HD-2026-019.pdf)",
    "status": "draft",
    "submittedAt": null
  }
}
```

#### 4. Re-submit Valid Contract — US03 & CTR-03 Success (`POST /api/contracts/2/submit`)
- **Sample Response (`200 OK`):**
```json
{
  "isSuccess": true,
  "errorCode": null,
  "message": "contract submitted for approval successfully, status has been changed to 'pending_approval'",
  "data": {
    "id": 2,
    "contractNo": "HD-2026-019",
    "customerId": 3,
    "customerName": "Tan Cang Transport Co., Ltd.",
    "startDate": "2026-06-01",
    "endDate": "2026-12-31",
    "contractValue": 250000000,
    "serviceTerms": "reefer container handling and storage services",
    "attachmentUrl": "[https://storage.abc-logistics.vn/contracts/HD-2026-019.pdf](https://storage.abc-logistics.vn/contracts/HD-2026-019.pdf)",
    "status": "pending_approval",
    "submittedAt": "2026-11-04T14:39:59.799537Z"
  }
}
```

#### 5. Duplicate Submission Attempt — Double Submit Prevention (`POST /api/contracts/2/submit`)
- **Sample Response (`409 Conflict`):**
```json
{
  "isSuccess": false,
  "errorCode": "CTR_ALREADY_SUBMITTED",
  "message": "the contract 'HD-2026-019' was already submitted for approval at 2026-11-04 14:39:59 UTC and is currently pending approval",
  "data": null
}
```

#### 6. Edit Contract in `pending_approval` — CTR-01 Failure (`PUT /api/contracts/2`)
- **Sample Request:**
```json
{
  "customerId": 3,
  "startDate": "2026-06-01",
  "endDate": "2026-12-31",
  "contractValue": 999000000,
  "serviceTerms": "try to modify contract while pending approval",
  "attachmentUrl": "[https://storage.abc-logistics.vn/contracts/HD-2026-019.pdf](https://storage.abc-logistics.vn/contracts/HD-2026-019.pdf)",
  "status": "draft"
}
```
- **Sample Response (`400 Bad Request`):**
```json
{
  "isSuccess": false,
  "errorCode": "CTR-01",
  "message": "CTR-01 violation: cannot edit a contract with status 'pending_approval', editing is only allowed when the status is 'draft' or 'revision_requested'",
  "data": null
}
```

#### 7. Direct Transition from `draft` to `approved` — CTR-03 Failure (`PUT /api/contracts/1`)
- **Sample Request:**
```json
{
  "customerId": 1,
  "startDate": "2026-05-01",
  "endDate": "2026-12-31",
  "contractValue": 150000000,
  "serviceTerms": "port handling and container storage services",
  "attachmentUrl": "[https://storage.abc-logistics.vn/contracts/HD-2026-018.pdf](https://storage.abc-logistics.vn/contracts/HD-2026-018.pdf)",
  "status": "approved"
}
```
- **Sample Response (`400 Bad Request`):**
```json
{
  "isSuccess": false,
  "errorCode": "CTR-03",
  "message": "CTR-03 violation: contracts cannot be moved directly to 'approved', they must go through the submission process first",
  "data": null
}
```

---

### 4.3. Pricing Endpoints (`PriceList` & `PriceItem`)

#### 1. Create Price List (`POST /api/pricelists`)
- **Sample Request:**
```json
{
  "name": "Reefer Container Services Price List Q3-Q4/2026",
  "effectiveDate": "2026-06-01",
  "expiryDate": "2026-12-31"
}
```
- **Sample Response (`201 Created`):**
```json
{
  "isSuccess": true,
  "errorCode": null,
  "message": "price list created successfully",
  "data": {
    "id": 2,
    "name": "Reefer Container Services Price List Q3-Q4/2026",
    "effectiveDate": "2026-06-01",
    "expiryDate": "2026-12-31",
    "status": "draft",
    "items": []
  }
}
```

#### 2. Add Invalid Price Item (`POST /api/pricelists/2/items`)
- **Sample Request (`unitPrice <= 0`):**
```json
{
  "serviceCode": "REEFER-PLUG-20",
  "serviceName": "Reefer power supply and monitoring (20ft)",
  "unitPrice": -50000,
  "unit": "day"
}
```
- **Sample Response (`400 Bad Request`):**
```json
{
  "isSuccess": false,
  "errorCode": "PRI_INVALID_PRICE",
  "message": "unit price must be greater than 0",
  "data": null
}
```

#### 3. Add Valid Price Item (`POST /api/pricelists/2/items`)
- **Sample Request:**
```json
{
  "serviceCode": "REEFER-PLUG-20",
  "serviceName": "Reefer power supply and monitoring (20ft)",
  "unitPrice": 450000,
  "unit": "day"
}
```
- **Sample Response (`200 OK`):**
```json
{
  "isSuccess": true,
  "errorCode": null,
  "message": "price item added to price list successfully",
  "data": {
    "id": 3,
    "priceListId": 2,
    "serviceCode": "REEFER-PLUG-20",
    "serviceName": "Reefer power supply and monitoring (20ft)",
    "unitPrice": 450000,
    "unit": "day"
  }
}
```