# Business Management System (QTKD) - Phase 2

A backend Web API for the Business Management System (QTKD) at ABC Logistics & Port Operations Company, built with **C# ASP.NET Core Web API**, **Entity Framework Core**, **PostgreSQL**, and **xUnit**.

---

## Phase 2 Implementation Roadmap (Weeks 05 – 07)

| Week | Primary Focus | Key Deliverables & Milestones | Detailed Documentation |
| :--- | :--- | :--- | :--- |
| **Week 05** | Project Foundation, Database & Read APIs | Layered architecture, EF Core entities (`Customer`, `Contract`, `PriceList`, `PriceItem`), PostgreSQL migrations, seed data, and baseline `GET` endpoints. | [`docs/Week05_Foundation_and_Database.md`](docs/Week05_Foundation_and_Rules.md) |
| **Week 06** | Business Logic, CRUD & Contract Submission (`US03`) | Create/Update APIs, standardized `ServiceResult<T>`, contract submission (`POST /api/contracts/{id}/submit`), domain rules (`CTR-01`, `CTR-02`, `CTR-03`, `PRC-02`), and double-submit prevention. | [`docs/Week06_API_and_Rules.md`](docs/Week06_API_and_Rules.md) |
| **Week 07** | Automated Testing, Bug Fixing & Stabilization | xUnit test suite (`QTKD.Tests`), isolated unit tests (`CTR-01`..`03`), integration tests (`TC01`–`TC08`) on a dedicated PostgreSQL test database (`qtkd_test_db`), defect fixes, and TRX test evidence. | [`docs/Week07_Test_Report.md`](docs/Week07_Test_Report.md) |

---

## 1. Solution Architecture & Project Structure

The solution enforces a clear separation of concerns across three logical layers: **Presentation (API/Controllers/DTOs)**, **Business Logic (Services)**, and **Data Access (EF Core DbContext/Entities)**, verified by an automated **xUnit Test Suite** (Unit & Integration Tests).

### Component Architecture

```text
[Client / Swagger / Postman / xUnit Test Host]
 └──> API Layer (Controllers)
       ├── CustomersController  (GET, POST, PUT /api/customers)
       ├── ContractsController  (GET, POST, PUT /api/contracts & POST /submit)
       ├── PriceListsController (GET, POST /api/pricelists & /items)
       └──> Business Logic Layer (Services & DTOs)
             ├── CustomerService   -.-> CustomerDto, Create/UpdateCustomerRequest
             ├── ContractService   -.-> ContractDetailDto, Create/UpdateContractRequest
             ├── PriceListService  -.-> PriceListDto, PriceItemDto, ServiceResult<T>
             └──> Data Access Layer (EF Core)
                   ├── QtkdDbContext
                   ├── Entities (Customer, Contract, PriceList, PriceItem)
                   └──> [(PostgreSQL Database: qtkd_db (Dev) | qtkd_test_db (Test))]
```

### Repository Layout

```text
QTKD/
├── docs/                                       # Architecture diagrams, API documentation, and test evidence
│   ├── Week05_Foundation_and_Database.md       # Detailed Week 05 schema design, seed data & read API verification
│   ├── Week06_API_and_Rules.md                 # Detailed Week 06 API specs, US03 & business rule mapping
│   ├── Week07_Test_Report.md                   # Detailed Week 07 TC01-TC08 test matrix & defect log
│   └── TestResults/
│       └── TestResults.trx                     # Automated xUnit test execution evidence file
├── tests/                                      # Automated unit and integration test projects (xUnit)
│   └── QTKD.Tests/                             # xUnit test project targeting QTKD.Api
│       ├── UnitTests/
│       │   └── ContractRulesUnitTests.cs       # Isolated business rule unit tests (CTR-01, CTR-02, CTR-03)
│       ├── IntegrationTests/
│       │   ├── CustomWebApplicationFactory.cs  # Test web host configured with isolated 'qtkd_test_db'
│       │   └── QtkdApiIntegrationTests.cs      # End-to-end HTTP + PostgreSQL integration tests (TC01-TC08)
│       └── QTKD.Tests.csproj                   # Test project dependencies (xUnit, Mvc.Testing, EF Core InMemory)
├── QTKD.Api/                                   # Main ASP.NET Core Web API project
│   ├── Controllers/                            # REST API endpoints (Customers, Contracts, PriceLists)
│   ├── DTOs/                                   # Data Transfer Objects & standardized ServiceResult<T>
│   ├── Services/                               # Business logic and domain rule validation (CTR-01..03, US03)
│   ├── Data/                                   # QtkdDbContext and EF Core Fluent API configurations
│   ├── Entities/                               # Database entities (Customer, Contract, PriceList, PriceItem)
│   ├── Migrations/                             # EF Core database migrations and seed data
│   ├── Program.cs                              # Application bootstrap, Dependency Injection & partial Program class
│   └── appsettings.json                        # Configuration template (no plain-text passwords)
├── QTKD.slnx                                   # Solution file
└── README.md                                   # Project overview, setup, execution, and testing guide
```

---

## 2. Prerequisites

Ensure the following tools are installed on your machine before setting up the project:
- **.NET SDK** (8.0 / 10.0 or later — keep the same SDK version across all builds and tests)
- **PostgreSQL** (14 or later)
- **Git**
- **EF Core CLI Tool (`dotnet-ef`)**:
  ```bash
  dotnet tool install --global dotnet-ef
  ```

---

## 3. Database Configuration (Security Best Practices)

Database passwords are **not** stored in `appsettings.json` or committed to the Git repository. Configure your local PostgreSQL connection string using **.NET User Secrets** from the root directory:

```bash
dotnet user-secrets init --project QTKD.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=qtkd_db;Username=postgres;Password=YOUR_POSTGRES_PASSWORD" --project QTKD.Api
```

*(Replace `YOUR_POSTGRES_PASSWORD` with your actual local PostgreSQL password).*

> **Note on Test Database Isolation:** When running integration tests in Week 07, `CustomWebApplicationFactory` automatically reads your configured PostgreSQL credentials and switches the target database name from `qtkd_db` to **`qtkd_test_db`**, ensuring your development data in `qtkd_db` is never modified or wiped.

---

## 4. Database Migrations & Initial Seed Data

Apply the EF Core migrations (`InitialCreate` from Week 05 and `AddContractSubmittedAt` from Week 06) to create the `qtkd_db` database, generate the tables (`Customers`, `Contracts`, `PriceLists`, `PriceItems`), configure primary/foreign keys, and populate initial seed data:

```bash
dotnet ef database update --project QTKD.Api
```

### Migration History
1. **`InitialCreate` (Week 05):** Creates the `Customers`, `Contracts`, `PriceLists`, and `PriceItems` tables, sets up unique indexes (`CustomerCode`, `ContractNo`), configures foreign key relationships (`Contract` $\rightarrow$ `Customer` with `Restrict`, `PriceItem` $\rightarrow$ `PriceList` with `Cascade`), and inserts baseline seed data.
2. **`AddContractSubmittedAt` (Week 06):** Adds the nullable `SubmittedAt` (`timestamp with time zone`) column to `Contracts` to track approval submission timestamps and enforce idempotency against duplicate submissions.

### Included Seed Data (Week 05 Baseline)
- **Customers (2 records):**
  - `Id = 1`: `cus-001` — `vct company` (TaxCode: `0778042702`, Address: `district 01, Ho Chi Minh city`, Status: `active`)
  - `Id = 2`: `cus-002` — `logistics company` (TaxCode: `0842702778`, Address: `district 07, Ho Chi Minh city`, Status: `active`)
- **Contracts (1 record):**
  - `Id = 1`: `CON-2026-018` linked to `CustomerId = 1` (`StartDate: 2026-05-01`, `EndDate: 2026-12-31`, `ContractValue: 150,000,000`, Status: `draft`, `AttachmentUrl: null`)
- **PriceLists (1 record) & PriceItems (2 records):**
  - `Id = 1`: `bang gia dich vu khai thac cang tieu chuan 2026` (`EffectiveDate: 2026-01-01`, `ExpiryDate: 2026-12-31`, Status: `effective`)
  - `PriceItem 1`: `THC-20DC` — `boc xep container 20 feet hang kho` (`UnitPrice: 1,250,000`, `Unit: container`)
  - `PriceItem 2`: `STORAGE-20DC` — `luu kho bai container 20 feet` (`UnitPrice: 180,000`, `Unit: day`)

---

## 5. Running the Application

Start the Web API server from the solution root directory:

```bash
dotnet run --project QTKD.Api
```

Once the application starts, open your browser and navigate to the **Swagger UI** URL shown in the terminal output (e.g., `http://localhost:5245/swagger`).

---

## 6. API Endpoints Overview (Week 05 & Week 06)

### Customers (`/api/customers`)
- `GET /api/customers` — Get all customers *(Week 05)*
- `GET /api/customers/{id}` — Get customer details by ID *(Week 06)*
- `POST /api/customers` — Create a new customer; validates required `CustomerCode`, `Name`, and code uniqueness *(Week 06)*
- `PUT /api/customers/{id}` — Update customer details and status (`active` / `suspended`) *(Week 06)*

### Contracts (`/api/contracts`)
- `GET /api/contracts` — Get all contracts *(Week 06)*
- `GET /api/contracts/{id}` — Get contract details by ID *(Week 05)*
- `POST /api/contracts` — Create a new contract in `draft` status; enforces **CTR-03** and active customer reference *(Week 06)*
- `PUT /api/contracts/{id}` — Update an existing contract; enforces **CTR-01** (`draft` or `revision_requested` only) and **CTR-03** *(Week 06)*
- `POST /api/contracts/{id}/submit` — Submit a contract for approval (**US03**); enforces **CTR-02**, **CTR-03**, and prevents **Double Submit** (`409 Conflict`) *(Week 06)*

### Pricing (`/api/pricelists`)
- `GET /api/pricelists` — Get all price lists with their items *(Week 06)*
- `GET /api/pricelists/{id}` — Get a specific price list by ID *(Week 06)*
- `POST /api/pricelists` — Create a new price list; enforces **PRC-02** effective date validation *(Week 06)*
- `GET /api/pricelists/{id}/items` — Get all price items belonging to a price list *(Week 06)*
- `POST /api/pricelists/{id}/items` — Add a new service price item to a price list; validates `UnitPrice > 0` and unique `ServiceCode` per price list *(Week 06)*

### Quick Smoke Test Commands (Week 05 Baseline Endpoints)
```bash
# 1. Get all customers (200 OK)
curl -X GET "http://localhost:5245/api/customers" -H "accept: application/json"

# 2. Get seeded draft contract by ID (200 OK)
curl -X GET "http://localhost:5245/api/contracts/1" -H "accept: application/json"

# 3. Get non-existent contract by ID (404 Not Found)
curl -X GET "http://localhost:5245/api/contracts/999" -H "accept: application/json"
```

---

## 7. Automated Testing & Quality Assurance (Week 07)

In Week 07, the solution includes an automated xUnit test project (`tests/QTKD.Tests`) that validates both domain logic and full HTTP-to-PostgreSQL execution flows.

### 7.1. Running the Automated Test Suite

From the solution root directory (`QTKD`), execute the following command to run all 17 tests and export the `.trx` evidence file:

```bash
dotnet test --logger "console;verbosity=normal" --logger "trx;LogFileName=TestResults.trx" --results-directory ./docs/TestResults
```

To run only Unit Tests or only Integration Tests:
```bash
# Run only isolated Unit Tests (EF Core InMemory)
dotnet test --filter "FullyQualifiedName~QTKD.Tests.UnitTests"

# Run only API & Database Integration Tests (PostgreSQL qtkd_test_db)
dotnet test --filter "FullyQualifiedName~QTKD.Tests.IntegrationTests"
```

### 7.2. Test Suite Structure (17 Automated Tests)
1. **Unit Tests (`UnitTests/ContractRulesUnitTests.cs` — 9 tests):**
   - **CTR-01:** Verifies updating contracts in `draft` and `revision_requested` succeeds (2 valid cases), while updating in `pending_approval` is rejected with `CTR-01` (1 invalid case).
   - **CTR-02:** Verifies submitting a complete contract succeeds (1 valid case), while missing `AttachmentUrl` or having a `suspended` customer is rejected with `CTR-02` (2 invalid cases).
   - **CTR-03:** Verifies submission transitions strictly to `pending_approval` (1 valid case), while creating or updating a contract directly to `approved` is rejected with `CTR-03` (2 invalid cases).
2. **Integration Tests (`IntegrationTests/QtkdApiIntegrationTests.cs` — 8 tests covering `TC01`–`TC08`):**
   - Executes real HTTP requests against `CustomWebApplicationFactory` backed by the isolated PostgreSQL `qtkd_test_db` database.
   - Asserts HTTP status codes, `ServiceResult<T>` response payloads, and actual database rows in PostgreSQL after each operation.

| Test ID | Scenario / Operation | Expected Outcome | Status |
| :--- | :--- | :--- | :--- |
| **TC01** | Create a valid Customer (`POST /api/customers`) | `201 Created`; persisted and readable from PostgreSQL | **PASS** |
| **TC02** | Create Customer missing required `Name` (`POST /api/customers`) | `400 Bad Request` (`CUS_INVALID_NAME`); not saved in DB | **PASS** |
| **TC03** | Update Contract in `draft` status (`PUT /api/contracts/{id}`) | `200 OK`; updated values persisted in PostgreSQL | **PASS** |
| **TC04** | Update Contract in `pending_approval` status (`PUT /api/contracts/{id}`) | `400 Bad Request` (`CTR-01`); DB record remains unchanged | **PASS** |
| **TC05** | Submit Contract missing `CTR-02` attachment (`POST /api/contracts/{id}/submit`) | `400 Bad Request` (`CTR-02`); status in DB remains `draft` | **PASS** |
| **TC06** | Submit valid Contract (`POST /api/contracts/{id}/submit`) | `200 OK`; transitions to `pending_approval` with `SubmittedAt` | **PASS** |
| **TC07** | Attempt direct transition from `draft` to `approved` (`PUT /api/contracts/{id}`) | `400 Bad Request` (`CTR-03`); status in DB remains `draft` | **PASS** |
| **TC08** | Submit the same Contract twice (`POST /api/contracts/{id}/submit`) | `409 Conflict` (`CTR_ALREADY_SUBMITTED`); no duplicate processing | **PASS** |

---

## 8. Known Limitations & Remaining Scope

- **Approval Workflow Simulation:** Submitting a contract (`POST /api/contracts/{id}/submit`) transitions the contract to `pending_approval` and records `SubmittedAt` while preventing duplicate submissions (`409 Conflict`). Multi-step approval routing (Legal, Accounting, Board of Directors), step-level assignee authorization (`APR-01`), and e-signature webhook integration (`APR-06`) are outside the mandatory scope of Phase 2 and reserved for subsequent phases.
- **Document Attachment Storage:** `AttachmentUrl` validates a non-empty document reference URL rather than handling multipart binary file uploads to cloud object storage.
- **High-Concurrency Optimistic Locking:** Duplicate submission is guarded at the service status level (`pending_approval`); high-concurrency production environments can additionally configure PostgreSQL `xmin` row-version tokens.

---

## 9. Detailed Weekly Documentation

- **Week 05 Documentation (Foundation, Schema & Seed Data):** [`docs/week05.md`](docs/week05.md)
- **Week 06 Documentation (API Payloads, US03 & Business Rules):** [`docs/week06.md`](docs/week06.md)
- **Week 07 Documentation (Test Matrix, Unit/Integration Tests & Defect Log):** [`docs/week07.md`](docs/week07.md)