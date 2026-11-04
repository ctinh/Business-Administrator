# Business Management System (QTKD) - Phase 2

A backend Web API for the Business Management System (QTKD) at ABC Logistics & Port Operations Company, built with **C# ASP.NET Core Web API**, **Entity Framework Core**, and **PostgreSQL**.

---

## 1. Solution Architecture & Project Structure

The solution enforces a clear separation of concerns across three logical layers: **Presentation (API/Controllers/DTOs)**, **Business Logic (Services)**, and **Data Access (EF Core DbContext/Entities)**.

### Component Architecture

```text
[Client / Swagger / Postman]
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
                   └──> [(PostgreSQL Database: qtkd_db)]
```

### Repository Layout

```text
QTKD/
├── docs/                           # Architecture diagrams, API documentation, and demo evidence
│   └── Week06_API_and_Rules.md     # Detailed Week 06 API specs & business rule mapping
├── tests/                          # Automated unit and integration test projects (xUnit)
├── QTKD.Api/                       # Main ASP.NET Core Web API project
│   ├── Controllers/                # REST API endpoints (Customers, Contracts, PriceLists)
│   ├── DTOs/                       # Data Transfer Objects & standardized ServiceResult<T>
│   ├── Services/                   # Business logic and domain rule validation (CTR-01..03, US03)
│   ├── Data/                       # QtkdDbContext and EF Core Fluent API configurations
│   ├── Entities/                   # Database entities (Customer, Contract, PriceList, PriceItem)
│   ├── Migrations/                 # EF Core database migrations and seed data
│   ├── Program.cs                  # Application bootstrap and Dependency Injection setup
│   └── appsettings.json            # Configuration template (no plain-text passwords)
├── QTKD.slnx                       # Solution file
└── README.md                       # Project overview, setup, and execution guide
```

---

## 2. Prerequisites

Ensure the following tools are installed on your machine before setting up the project:
- **.NET SDK** (8.0 or later)
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

---

## 4. Database Migrations & Initial Seed Data

Apply the EF Core migrations (`InitialCreate` and `AddContractSubmittedAt`) to create the `qtkd_db` database, generate the tables (`Customers`, `Contracts`, `PriceLists`, `PriceItems`), configure primary/foreign keys, and populate initial seed data:

```bash
dotnet ef database update --project QTKD.Api
```

### Included Seed Data (Week 05 Baseline)
- **Customers (2 records):** `cus-001` and `cus-002` (Status: `active`)
- **Contracts (1 record):** `CON-2026-018` linked to `CustomerId = 1` (Status: `draft`, `AttachmentUrl = null`)
- **PriceLists (1 record) & PriceItems (2 records):** Standard Port Operations Price List 2026 (`effective`) with 2 service items (`THC-20DC` and `STORAGE-20DC`)

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
- `GET /api/customers` — Get all customers
- `GET /api/customers/{id}` — Get customer details by ID
- `POST /api/customers` — Create a new customer (validates required code/name and uniqueness)
- `PUT /api/customers/{id}` — Update customer details and status (`active` / `suspended`)

### Contracts (`/api/contracts`)
- `GET /api/contracts` — Get all contracts
- `GET /api/contracts/{id}` — Get contract details by ID
- `POST /api/contracts` — Create a new contract in `draft` status (enforces **CTR-03**)
- `PUT /api/contracts/{id}` — Update an existing contract (enforces **CTR-01** and **CTR-03**)
- `POST /api/contracts/{id}/submit` — Submit a contract for approval (**US03**, enforces **CTR-02**, **CTR-03**, and prevents **Double Submit**)

### Pricing (`/api/pricelists`)
- `GET /api/pricelists` — Get all price lists with their items
- `GET /api/pricelists/{id}` — Get a specific price list by ID
- `POST /api/pricelists` — Create a new price list (enforces **PRC-02** effective date validation)
- `GET /api/pricelists/{id}/items` — Get all price items belonging to a price list
- `POST /api/pricelists/{id}/items` — Add a new service price item to a price list (validates positive unit price and unique service code)

> **Detailed Week 06 Documentation:** For full request/response JSON samples, US03 workflow assumptions, and business rule (`CTR-01`, `CTR-02`, `CTR-03`) code mappings, see [`docs/Week06_API_and_Rules.md`](docs/Week06_API_and_Rules.md).