# WexCorporatePayments API

API for managing corporate purchase transactions in USD and converting them to foreign currencies using the U.S. Treasury Fiscal Data API.

## 📋 Description

This solution implements an ASP.NET Core REST API (.NET 8 LTS) that allows:

- Create purchase transactions in USD
- Convert transactions to foreign currencies using exchange rates from the Treasury API
- Persist data in a local SQLite database
- Apply business rules for rate validation (6-month window)
- Round monetary values to 2 decimal places using MidpointRounding.ToEven

## 🏗️ Architecture

The solution follows **Clean Architecture** principles with layer separation:

```n WexCorporatePayments/
├── WexCorporatePayments.Domain/          # Entities, business rules, exceptions
├── WexCorporatePayments.Application/     # Use cases (Handlers), DTOs, ports
├── WexCorporatePayments.Infrastructure/  # EF Core, SQLite, Treasury API integration
├── WexCorporatePayments.Api/             # Controllers, Program.cs, configurations
└── WexCorporatePayments.Tests/           # Unit and integration tests
```n
### Layer Dependencies

- **Domain**: No external dependencies
- **Application**: References only **Domain**
- **Infrastructure**: References **Domain** and **Application**
- **Api**: References **Application** and **Infrastructure** (does not reference Domain directly)
- **Tests**: References **Domain**, **Application**, and **Api**

## 🚀 Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Code editor (Visual Studio, VS Code, Rider, etc.)

### Installation and Execution

1. **Clone or extract the project**

```bash
cd WexCorporatePayments
```n
2. **Restore dependencies**

```bash
dotnet restore
```n
3. **Build the solution**

```bash
dotnet build
```

4. **Create the initial database migration**

```bash
dotnet ef migrations add InitialCreate -p WexCorporatePayments.Infrastructure -s WexCorporatePayments.Api
```n
5. **Apply migrations to the database**

```bash
dotnet ef database update -p WexCorporatePayments.Infrastructure -s WexCorporatePayments.Api
```n
> **Note**: Migrations are automatically applied on application startup, but you can run them manually if preferred.

6. **Run the API**

```bash
dotnet run --project WexCorporatePayments.Api
```n
The API will be available at:
- HTTPS: https://localhost:7XXX (port is dynamically assigned)
- HTTP: http://localhost:5XXX

7. **Access Swagger UI**

Open your browser and navigate to: https://localhost:7XXX/swagger

## 🧪 Running Tests

```bash
dotnet test
```n
To view test coverage:

```bash
dotnet test --collect:"XPlat Code Coverage"
```n
## 📡 API Endpoints

### 1. Create Purchase Transaction

**POST** /api/transactions

Creates a new purchase transaction in USD.

**Request Body:**
```json
{
  "description": "Dell Laptop",
  "transactionDate": "2025-08-15",
  "amountUsd": 1234.56
}
```

**Validations:**
- description: required, maximum 50 characters
- transactionDate: required, valid date
- amountUsd: required, greater than zero

**Responses:**
- 201 Created: Transaction created successfully
  ```json
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
  }
  ```n- 400 Bad Request: Invalid data (DataAnnotations validation)
- 422 Unprocessable Entity: Domain validation error

**Example with cURL:**
```bash
curl -X POST "https://localhost:7XXX/api/transactions" \
  -H "Content-Type: application/json" \
  -d '{"description": "Dell Laptop", "transactionDate": "2025-08-15", "amountUsd": 1234.56}'
```

### 2. Convert Transaction to Foreign Currency

**GET** /api/transactions/{id}/convert?country={country}&currency={currency}

Converts an existing transaction to a foreign currency using Treasury API rates.

**Path Parameters:**
- id (Guid): Transaction ID

**Query Parameters:**
- country (string): Country name (e.g., "Brazil")
- currency (string): Currency name (e.g., "Real")

**Business Rules:**
- Exchange rate must have record_date ≤ transactionDate
- Exchange rate must have record_date ≥ transactionDate - 6 months
- If no valid rate exists, returns 422 error
- Converted amount is rounded to 2 decimal places using MidpointRounding.ToEven

**Responses:**
- 200 OK: Conversion successful
  ```json
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "description": "Dell Laptop",
    "transactionDate": "2025-08-15",
    "amountUsd": 1234.56,
    "exchangeRate": 5.10,
    "convertedAmount": 6296.26,
    "country": "Brazil",
    "currency": "Real",
    "recordDate": "2025-08-15"
  }
  ```n- 404 Not Found: Transaction not found
- 422 Unprocessable Entity: Exchange rate not available for the period

**Example with cURL:**
```bash
curl -X GET "https://localhost:7XXX/api/transactions/3fa85f64-5717-4562-b3fc-2c963f66afa6/convert?country=Brazil&currency=Real"
```

## ⚙️ Configuration

Application settings are in the appsettings.json file:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=wex.db"
  },
  "TreasuryApi": {
    "BaseUrl": "https://api.fiscaldata.treasury.gov/services/api/fiscal_service/"
  }
}
```n
### Connection String
- By default, uses SQLite with wex.db file in the project root
- Database is automatically created on first startup

### Treasury API
- Base URL for U.S. Treasury Fiscal Data API
- Endpoint used: v1/accounting/od/rates_of_exchange

## 📦 NuGet Packages Used

### WexCorporatePayments.Infrastructure
- Microsoft.EntityFrameworkCore.Sqlite (8.0.0)
- Microsoft.EntityFrameworkCore.Design (8.0.0)

### WexCorporatePayments.Tests
- xunit (included in template)
- FluentAssertions (8.8.0)
- Moq (4.20.72)
- Microsoft.AspNetCore.Mvc.Testing (8.0.0)
- Microsoft.EntityFrameworkCore.InMemory (8.0.0)

## 💡 Practical Examples

### Complete Flow

1. **Create a transaction:**

```bash
curl -X POST "https://localhost:7001/api/transactions" \
  -H "Content-Type: application/json" \
  -d '{"description": "MacBook Pro", "transactionDate": "2025-10-01", "amountUsd": 2499.99}'
```n
Response:
```json
{
  "id": "abc12345-6789-4def-gh12-ijklmnopqrst"
}
```n
2. **Convert to Brazilian Real (BRL):**

```bash
curl -X GET "https://localhost:7001/api/transactions/abc12345-6789-4def-gh12-ijklmnopqrst/convert?country=Brazil&currency=Real"
```

Response:
```json
{
  "id": "abc12345-6789-4def-gh12-ijklmnopqrst",
  "description": "MacBook Pro",
  "transactionDate": "2025-10-01",
  "amountUsd": 2499.99,
  "exchangeRate": 5.25,
  "convertedAmount": 13124.95,
  "country": "Brazil",
  "currency": "Real",
  "recordDate": "2025-09-30"
}
```

### Country and Currency Examples (Treasury API)

| Country | Currency | Query Example |
|---------|----------|---------------|
| Brazil | Real | country=Brazil&currency=Real |
| Canada | Dollar | country=Canada&currency=Dollar |
| Mexico | Peso | country=Mexico&currency=Peso |
| United Kingdom | Pound | country=United Kingdom&currency=Pound |
| Japan | Yen | country=Japan&currency=Yen |
| Euro Zone | Euro | country=Euro Zone&currency=Euro |

> **Note**: Country and currency names must exactly match the values returned by the Treasury API.

## 🛠️ Development

### Folder Structure

```n WexCorporatePayments.Domain/
├── Entities/
│   └── PurchaseTransaction.cs
├── Exceptions/
│   └── DomainValidationException.cs
└── Repositories/
    └── IPurchaseTransactionRepository.cs

WexCorporatePayments.Application/
├── DTOs/
│   ├── CreatePurchaseTransactionRequest.cs
│   └── ConvertedPurchaseResponse.cs
├── Handlers/
│   ├── CreatePurchaseTransactionHandler.cs
│   └── ConvertPurchaseHandler.cs
├── Services/
│   └── IExchangeRateService.cs
└── DependencyInjection.cs

WexCorporatePayments.Infrastructure/
├── Persistence/
│   ├── AppDbContext.cs
│   └── PurchaseTransactionRepository.cs
├── ExternalServices/
│   └── ExchangeRateService.cs
└── DependencyInjection.cs

WexCorporatePayments.Api/
├── Controllers/
│   └── TransactionsController.cs
├── Program.cs
└── appsettings.json

WexCorporatePayments.Tests/
├── Domain/
│   └── PurchaseTransactionTests.cs
├── Application/
│   ├── CreatePurchaseTransactionHandlerTests.cs
│   └── ConvertPurchaseHandlerTests.cs
└── Integration/
    └── TransactionsControllerTests.cs
```

### Adding a New Migration

```bash
dotnet ef migrations add MigrationName -p WexCorporatePayments.Infrastructure -s WexCorporatePayments.Api
dotnet ef database update -p WexCorporatePayments.Infrastructure -s WexCorporatePayments.Api
```n
### Removing the Last Migration

```bash
dotnet ef migrations remove -p WexCorporatePayments.Infrastructure -s WexCorporatePayments.Api
```

## 📊 Test Coverage

The solution includes:

- **Domain Unit Tests**: Entity validations and business rules
- **Application Unit Tests**: Handlers and application logic
- **Integration Tests**: API endpoints with in-memory database

### Main Test Scenarios

✅ Transaction creation with valid data
✅ Description validation (max 50 characters)
✅ Positive amount validation
✅ Rounding to 2 decimal places (ToEven)
✅ Successful currency conversion
✅ Error when rate is not available
✅ Error when rate is outside the 6-month window
✅ Endpoints return correct status codes (201, 404, 422)

## 📝 Logs

The application logs structured information using ILogger:

- **Information**: Successful operations (creation, conversion)
- **Warning**: Failed validations, rates not found
- **Error**: Unexpected errors, network failures

Example log:
```n info: WexCorporatePayments.Api.Controllers.TransactionsController[0]
      Transaction created successfully. Id: abc12345-6789-4def-gh12-ijklmnopqrst
```

## 🚨 Error Handling

The API returns standardized responses using ProblemDetails:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Validation error",
  "status": 422,
  "detail": "Could not find a valid exchange rate for Brazil/Real..."
}
```

## 📚 Additional Resources

- [U.S. Treasury Fiscal Data API Documentation](https://fiscaldata.treasury.gov/api-documentation/)
- [ASP.NET Core Documentation](https://docs.microsoft.com/aspnet/core)
- [Entity Framework Core Documentation](https://docs.microsoft.com/ef/core)
- [xUnit Documentation](https://xunit.net/)
- [FluentAssertions Documentation](https://fluentassertions.com/)

## 👨‍💻 Author

Developed as part of the WexCorporatePayments technical challenge.

## 📄 License

This project is private and intended for technical evaluation purposes only.
