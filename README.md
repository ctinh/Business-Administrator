# Business Management System (QTKD) - Phase 2

A backend Web API for the Business Management System (QTKD) at ABC Logistics & Port Operations Company, built with **C# ASP.NET Core Web API**, **Entity Framework Core**, and **PostgreSQL**.

---

## 1. Solution Architecture & Project Structure

The solution enforces a clear separation of concerns across three logical layers: **Presentation (API/Controllers/DTOs)**, **Business Logic (Services)**, and **Data Access (EF Core DbContext/Entities)**.

### Component Architecture

```text
[Client / Swagger / Postman]
 └──> API Layer (Controllers)
       ├── CustomersController (GET /api/customers)
       ├── ContractsController (GET /api/contracts/{id})
       └──> Business Logic Layer (Services & DTOs)
             ├── CustomerService  -.-> CustomerDto
             ├── ContractService  -.-> ContractDetailDto
             └──> Data Access Layer (EF Core)
                   ├── QtkdDbContext
                   ├── Entities (Customer, Contract, PriceList, PriceItem)
                   └──> [(PostgreSQL Database: qtkd_db)]
```

### Repository Layout

```text
QTKD/
├── docs/                  # Architecture diagrams, API documentation, and demo evidence
├── tests/                 # Automated unit and integration test projects (xUnit)
├── QTKD.Api/              # Main ASP.NET Core Web API project
│   ├── Controllers/       # REST API endpoints
│   ├── DTOs/              # Data Transfer Objects
│   ├── Services/          # Business logic and domain rule validation
│   ├── Data/              # QtkdDbContext and EF Core Fluent API configurations
│   ├── Entities/          # Database entities (Customer, Contract, PriceList, PriceItem)
│   ├── Migrations/        # EF Core database migrations and seed data
│   ├── Program.cs         # Application bootstrap and Dependency Injection setup
│   └── appsettings.json   # Configuration template (no plain-text passwords)
├── QTKD.slnx              # Solution file
└── README.md              # Project setup and execution guide
```

---

## 2. Prerequisites

Ensure the following tools are installed on your machine before setting up the project:
* **.NET SDK** (8.0 or later)
* **PostgreSQL** (14 or later)
* **Git**
* **EF Core CLI Tool (`dotnet-ef`)**:
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

## 4. Database Migration & Initial Seed Data

Apply the EF Core migrations to create the `qtkd_db` database, generate the tables (`Customers`, `Contracts`, `PriceLists`, `PriceItems`), configure primary/foreign keys, and populate initial seed data:

```bash
dotnet ef database update --project QTKD.Api
```

### Included Seed Data (Week 05 Baseline)
* **Customers (2 records):**
* **Contracts (1 record):**
* **PriceLists (1 record) & PriceItems (2 records):**

---

## 5. Running the Application

Start the Web API server from the solution root directory:

```bash
dotnet run --project QTKD.Api
```

Once the application starts, open your browser and navigate to the **Swagger UI** URL shown in the terminal output (e.g., `http://localhost:5xxx/swagger`).

---

## 6. Testing the Endpoints

You can test the Week 05 read endpoints via **Swagger UI**, **Postman**, or `curl`, and verify the output against the records in PostgreSQL:

### 1. Get All Customers
* **Endpoint:** `GET /api/customers`
* **Sample Request:**
  ```bash
  curl -X GET "http://localhost:5000/api/customers" -H "accept: application/json"
  ```
* **Expected Response (`200 OK`):** Returns the list of seeded customers (`KH-001` and `KH-002`).

### 2. Get Contract by ID
* **Endpoint:** `GET /api/contracts/{id}`
* **Sample Request (Valid ID):**
  ```bash
  curl -X GET "http://localhost:5000/api/contracts/1" -H "accept: application/json"
  ```
* **Expected Response (`200 OK`):** Returns the details of contract `HD-2026-018` in `Draft` status along with the customer name.
* **Sample Request (Non-existent ID):**
  ```bash
  curl -X GET "http://localhost:5000/api/contracts/999" -H "accept: application/json"
  ```
* **Expected Response (`404 Not Found`):** Returns an error message indicating the contract does not exist.