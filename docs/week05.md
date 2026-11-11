# Week 05: System Foundation, Database Schema & Read APIs

## 1. Week 05 Objectives

The goal of Week 05 is to establish the technical and architectural foundation for the **Business Management System (QTKD)** at ABC Logistics & Port Operations Company:
1. Initialize a single Git repository and multi-layered ASP.NET Core Web API solution (`QTKD`) used continuously across Weeks 05, 06, and 07.
2. Design and map core domain entities (`Customer`, `Contract`, `PriceList`, `PriceItem`) using Entity Framework Core and PostgreSQL.
3. Configure secure database connectivity using `.NET User Secrets` so plain-text passwords are never committed to Git.
4. Generate the initial database migration (`InitialCreate`), verify primary/foreign keys in PostgreSQL, and populate baseline seed data.
5. Implement and verify the foundational read endpoints (`GET /api/customers` and `GET /api/contracts/{id}`) against PostgreSQL records.

---

## 2. Layered Architecture Separation

Within `QTKD.Api`, responsibilities are strictly separated into three logical layers:

| Layer | Folder / Namespace | Components | Responsibility |
| :--- | :--- | :--- | :--- |
| **Presentation (API Layer)** | `QTKD.Api/Controllers` & `QTKD.Api/DTOs` | `CustomersController`, `ContractsController`, `CustomerDto`, `ContractDetailDto` | Handles HTTP routing, request binding, HTTP status codes (`200 OK`, `404 Not Found`), and JSON serialization. |
| **Business Logic Layer** | `QTKD.Api/Services` | `ICustomerService`, `CustomerService`, `IContractService`, `ContractService` | Encapsulates domain queries, data projection to DTOs, and business rule validations. |
| **Data Access Layer** | `QTKD.Api/Data` & `QTKD.Api/Entities` | `QtkdDbContext`, `Customer`, `Contract`, `PriceList`, `PriceItem` | Manages EF Core entity mappings, Fluent API constraints, relationships, migrations, and PostgreSQL queries. |

---

## 3. Database Schema & Entity Relationships

### 3.1. Entity Relationship Diagram (Logical Schema)

```text
+-----------------------+          1        *  +-----------------------+
|       Customers       |--------------------->|       Contracts       |
+-----------------------+  OnDelete: Restrict  +-----------------------+
| PK  Id (int)          |                      | PK  Id (int)          |
| UQ  CustomerCode      |                      | UQ  ContractNo        |
|     Name              |                      | FK  CustomerId (int)  |
|     TaxCode           |                      |     StartDate (date)  |
|     Address           |                      |     EndDate (date)    |
|     ContactEmail      |                      |     ContractValue     |
|     Status            |                      |     ServiceTerms      |
+-----------------------+                      |     AttachmentUrl     |
                                               |     Status            |
                                               |     SubmittedAt       |
                                               +-----------------------+

+-----------------------+          1        *  +-----------------------+
|      PriceLists       |--------------------->|      PriceItems       |
+-----------------------+  OnDelete: Cascade   +-----------------------+
| PK  Id (int)          |                      | PK  Id (int)          |
|     Name              |                      | FK  PriceListId (int) |
|     EffectiveDate     |                      |     ServiceCode       |
|     ExpiryDate        |                      |     ServiceName       |
|     Status            |                      |     UnitPrice         |
+-----------------------+                      |     Unit              |
                                               +-----------------------+
```

### 3.2. Table Constraints & Foreign Keys Configured in `QtkdDbContext`
- **`Customers` Table:**
  - Primary Key: `PK_Customers (Id)`
  - Unique Index: `IX_Customers_CustomerCode (CustomerCode)`
  - Required Fields: `CustomerCode` (max 50), `Name` (max 200), `Status` (max 50)
- **`Contracts` Table:**
  - Primary Key: `PK_Contracts (Id)`
  - Unique Index: `IX_Contracts_ContractNo (ContractNo)`
  - Foreign Key: `FK_Contracts_Customers_CustomerId` referencing `Customers(Id)` with `DeleteBehavior.Restrict` (prevents deleting a customer that has existing contracts).
  - Numeric Precision: `ContractValue` configured as `numeric(18,2)`.
- **`PriceLists` Table:**
  - Primary Key: `PK_PriceLists (Id)`
  - Required Fields: `Name` (max 200), `EffectiveDate`, `Status` (max 50)
- **`PriceItems` Table:**
  - Primary Key: `PK_PriceItems (Id)`
  - Foreign Key: `FK_PriceItems_PriceLists_PriceListId` referencing `PriceLists(Id)` with `DeleteBehavior.Cascade`.
  - Numeric Precision: `UnitPrice` configured as `numeric(18,2)`.

---

## 4. Baseline Seed Data in PostgreSQL

When `dotnet ef database update --project QTKD.Api` is executed, the following baseline records are automatically seeded into `qtkd_db`:

### 4.1. `Customers` Table (2 Records)
| Id | CustomerCode | Name | TaxCode | Address | ContactEmail | Status |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| `1` | `cus-001` | `vct company` | `0778042702` | `district 01, Ho Chi Minh city` | `contact@vct.vn` | `active` |
| `2` | `cus-002` | `logistics company` | `0842702778` | `district 07, Ho Chi Minh city` | `contact@logistic.vn` | `active` |

### 4.2. `Contracts` Table (1 Record in `draft` Status)
| Id | ContractNo | CustomerId | StartDate | EndDate | ContractValue | ServiceTerms | AttachmentUrl | Status |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| `1` | `CON-2026-018` | `1` | `2026-05-01` | `2026-12-31` | `150000000.00` | `dich vu boc xep va luu bai container tai cang abc` | `NULL` | `draft` |

### 4.3. `PriceLists` (1 Record) & `PriceItems` (2 Records)
- **PriceList (`Id = 1`):** `bang gia dich vu khai thac cang tieu chuan 2026` (`EffectiveDate: 2026-01-01`, `ExpiryDate: 2026-12-31`, `Status: effective`)
- **PriceItems:**
  - `Id = 1` (`PriceListId = 1`): `THC-20DC` — `boc xep container 20 feet hang kho` (`UnitPrice: 1250000.00`, `Unit: container`)
  - `Id = 2` (`PriceListId = 1`): `STORAGE-20DC` — `luu kho bai container 20 feet` (`UnitPrice: 180000.00`, `Unit: day`)

---

## 5. Week 05 Read API Specifications & Database Verification

### 5.1. Get All Customers (`GET /api/customers`)
- **Sample Response (`200 OK`):**
```json
[
  {
    "id": 1,
    "customerCode": "cus-001",
    "name": "vct company",
    "taxCode": "0778042702",
    "address": "district 01, Ho Chi Minh city",
    "contactEmail": "contact@vct.vn",
    "status": "active"
  },
  {
    "id": 2,
    "customerCode": "cus-002",
    "name": "logistics company",
    "taxCode": "0842702778",
    "address": "district 07, Ho Chi Minh city",
    "contactEmail": "contact@logistic.vn",
    "status": "active"
  }
]
```

### 5.2. Get Contract Details by ID (`GET /api/contracts/{id}`)
- **Sample Response for `GET /api/contracts/1` (`200 OK`):**
```json
{
  "id": 1,
  "contractNo": "CON-2026-018",
  "customerId": 1,
  "customerName": "vct company",
  "startDate": "2026-05-01",
  "endDate": "2026-12-31",
  "contractValue": 150000000.00,
  "serviceTerms": "dich vu boc xep va luu bai container tai cang abc",
  "attachmentUrl": null,
  "status": "draft",
  "submittedAt": null
}
```
- **Sample Response for Non-Existent ID `GET /api/contracts/999` (`404 Not Found`):**
```json
{
  "isSuccess": false,
  "errorCode": "CTR_NOT_FOUND",
  "message": "contract with id 999 not found",
  "data": null
}
```

### 5.3. PostgreSQL Verification Queries
Use the following SQL queries in pgAdmin 4 / DBeaver to cross-check the database records against the API responses:

```sql
SELECT * FROM "Customers" ORDER BY "Id";
SELECT * FROM "Contracts" ORDER BY "Id";
SELECT * FROM "PriceLists" ORDER BY "Id";
SELECT * FROM "PriceItems" ORDER BY "Id";
```