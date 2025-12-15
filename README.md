# WexCorporatePayments API

API para gerenciar transações de compra corporativa em USD e converter para moedas estrangeiras usando o U.S. Treasury Fiscal Data API.

## ?? Descrição

Esta solução implementa uma API REST ASP.NET Core (.NET 8 LTS) que permite:

- Criar transações de compra em USD
- Converter transações para moedas estrangeiras usando taxas de câmbio do Treasury API
- Persistir dados em banco SQLite local
- Aplicar regras de negócio para validação de taxas (janela de 6 meses)
- Arredondar valores monetários para 2 casas decimais com `MidpointRounding.ToEven`

## ??? Arquitetura

A solução segue os princípios de **Clean Architecture** com separação em camadas:

```
WexCorporatePayments/
??? WexCorporatePayments.Domain/          # Entidades, regras de negócio, exceções
??? WexCorporatePayments.Application/     # Casos de uso (Handlers), DTOs, portas
??? WexCorporatePayments.Infrastructure/  # EF Core, SQLite, integração com Treasury API
??? WexCorporatePayments.Api/             # Controllers, Program.cs, configurações
??? WexCorporatePayments.Tests/           # Testes unitários e de integração
```

### Dependências entre Camadas

- **Domain**: Não possui dependências externas
- **Application**: Referencia apenas **Domain**
- **Infrastructure**: Referencia **Domain** e **Application**
- **Api**: Referencia **Application** e **Infrastructure** (não referencia Domain diretamente)
- **Tests**: Referencia **Domain**, **Application** e **Api**

## ?? Começando

### Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Editor de código (Visual Studio, VS Code, Rider, etc.)

### Instalação e Execução

1. **Clone ou extraia o projeto**

```bash
cd WexCorporatePayments
```

2. **Restaure as dependências**

```bash
dotnet restore
```

3. **Compile a solução**

```bash
dotnet build
```

4. **Crie a migração inicial do banco de dados**

```bash
dotnet ef migrations add InitialCreate -p WexCorporatePayments.Infrastructure -s WexCorporatePayments.Api
```

5. **Aplique as migrations ao banco de dados**

```bash
dotnet ef database update -p WexCorporatePayments.Infrastructure -s WexCorporatePayments.Api
```

> **Nota**: As migrations são aplicadas automaticamente no startup da aplicação, mas você pode executar manualmente se preferir.

6. **Execute a API**

```bash
dotnet run --project WexCorporatePayments.Api
```

A API estará disponível em:
- HTTPS: `https://localhost:7XXX` (a porta é atribuída dinamicamente)
- HTTP: `http://localhost:5XXX`

7. **Acesse o Swagger UI**

Abra o navegador e acesse: `https://localhost:7XXX/swagger`

## ?? Executando os Testes

```bash
dotnet test
```

Para visualizar a cobertura de testes:

```bash
dotnet test --collect:"XPlat Code Coverage"
```

## ?? Endpoints da API

### 1. Criar Transação de Compra

**POST** `/api/transactions`

Cria uma nova transação de compra em USD.

**Request Body:**
```json
{
  "description": "Laptop Dell",
  "transactionDate": "2025-08-15",
  "amountUsd": 1234.56
}
```

**Validações:**
- `description`: obrigatória, máximo 50 caracteres
- `transactionDate`: obrigatória, data válida
- `amountUsd`: obrigatório, maior que zero

**Responses:**
- `201 Created`: Transação criada com sucesso
  ```json
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
  }
  ```
- `400 Bad Request`: Dados inválidos (validação de DataAnnotations)
- `422 Unprocessable Entity`: Erro de validação de domínio

**Exemplo com cURL:**
```bash
curl -X POST "https://localhost:7XXX/api/transactions" \
  -H "Content-Type: application/json" \
  -d '{
    "description": "Laptop Dell",
    "transactionDate": "2025-08-15",
    "amountUsd": 1234.56
  }'
```

### 2. Converter Transação para Moeda Estrangeira

**GET** `/api/transactions/{id}/convert?country={country}&currency={currency}`

Converte uma transação existente para uma moeda estrangeira usando taxas do Treasury API.

**Path Parameters:**
- `id` (Guid): Id da transação

**Query Parameters:**
- `country` (string): Nome do país (ex: "Brazil")
- `currency` (string): Nome da moeda (ex: "Real")

**Regras de Negócio:**
- A taxa de câmbio deve ter `record_date` ? `transactionDate`
- A taxa de câmbio deve ter `record_date` ? `transactionDate - 6 meses`
- Se não houver taxa válida, retorna erro 422
- O valor convertido é arredondado para 2 casas decimais com `MidpointRounding.ToEven`

**Responses:**
- `200 OK`: Conversão realizada com sucesso
  ```json
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "description": "Laptop Dell",
    "transactionDate": "2025-08-15",
    "amountUsd": 1234.56,
    "exchangeRate": 5.10,
    "convertedAmount": 6296.26,
    "country": "Brazil",
    "currency": "Real",
    "recordDate": "2025-08-15"
  }
  ```
- `404 Not Found`: Transação não encontrada
- `422 Unprocessable Entity`: Taxa de câmbio não disponível para o período

**Exemplo com cURL:**
```bash
curl -X GET "https://localhost:7XXX/api/transactions/3fa85f64-5717-4562-b3fc-2c963f66afa6/convert?country=Brazil&currency=Real"
```

## ?? Configurações

As configurações da aplicação estão no arquivo `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=wex.db"
  },
  "TreasuryApi": {
    "BaseUrl": "https://api.fiscaldata.treasury.gov/services/api/fiscal_service/"
  }
}
```

### Connection String
- Por padrão, usa SQLite com arquivo `wex.db` na raiz do projeto
- O banco é criado automaticamente no primeiro startup

### Treasury API
- URL base do U.S. Treasury Fiscal Data API
- Endpoint usado: `v1/accounting/od/rates_of_exchange`

## ?? Pacotes NuGet Utilizados

### WexCorporatePayments.Infrastructure
- `Microsoft.EntityFrameworkCore.Sqlite` (8.0.0)
- `Microsoft.EntityFrameworkCore.Design` (8.0.0)

### WexCorporatePayments.Tests
- `xunit` (incluso no template)
- `FluentAssertions` (8.8.0)
- `Moq` (4.20.72)
- `Microsoft.AspNetCore.Mvc.Testing` (8.0.0)
- `Microsoft.EntityFrameworkCore.InMemory` (8.0.0)

## ?? Exemplos Práticos

### Fluxo Completo

1. **Criar uma transação:**

```bash
curl -X POST "https://localhost:7001/api/transactions" \
  -H "Content-Type: application/json" \
  -d '{
    "description": "MacBook Pro",
    "transactionDate": "2025-10-01",
    "amountUsd": 2499.99
  }'
```

Resposta:
```json
{
  "id": "abc12345-6789-4def-gh12-ijklmnopqrst"
}
```

2. **Converter para Real (BRL):**

```bash
curl -X GET "https://localhost:7001/api/transactions/abc12345-6789-4def-gh12-ijklmnopqrst/convert?country=Brazil&currency=Real"
```

Resposta:
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

### Exemplos de Países e Moedas (Treasury API)

| País | Moeda | Exemplo de Query |
|------|-------|------------------|
| Brazil | Real | `country=Brazil&currency=Real` |
| Canada | Dollar | `country=Canada&currency=Dollar` |
| Mexico | Peso | `country=Mexico&currency=Peso` |
| United Kingdom | Pound | `country=United Kingdom&currency=Pound` |
| Japan | Yen | `country=Japan&currency=Yen` |
| Euro Zone | Euro | `country=Euro Zone&currency=Euro` |

> **Nota**: Os nomes de países e moedas devem corresponder exatamente aos valores retornados pela Treasury API.

## ??? Desenvolvimento

### Estrutura de Pastas

```
WexCorporatePayments.Domain/
??? Entities/
?   ??? PurchaseTransaction.cs
??? Exceptions/
?   ??? DomainValidationException.cs
??? Repositories/
    ??? IPurchaseTransactionRepository.cs

WexCorporatePayments.Application/
??? DTOs/
?   ??? CreatePurchaseTransactionRequest.cs
?   ??? ConvertedPurchaseResponse.cs
??? Handlers/
?   ??? CreatePurchaseTransactionHandler.cs
?   ??? ConvertPurchaseHandler.cs
??? Services/
?   ??? IExchangeRateService.cs
??? DependencyInjection.cs

WexCorporatePayments.Infrastructure/
??? Persistence/
?   ??? AppDbContext.cs
?   ??? PurchaseTransactionRepository.cs
??? ExternalServices/
?   ??? ExchangeRateService.cs
??? DependencyInjection.cs

WexCorporatePayments.Api/
??? Controllers/
?   ??? TransactionsController.cs
??? Program.cs
??? appsettings.json

WexCorporatePayments.Tests/
??? Domain/
?   ??? PurchaseTransactionTests.cs
??? Application/
?   ??? CreatePurchaseTransactionHandlerTests.cs
?   ??? ConvertPurchaseHandlerTests.cs
??? Integration/
    ??? TransactionsControllerTests.cs
```

### Adicionando uma Nova Migration

```bash
dotnet ef migrations add NomeDaMigration -p WexCorporatePayments.Infrastructure -s WexCorporatePayments.Api
dotnet ef database update -p WexCorporatePayments.Infrastructure -s WexCorporatePayments.Api
```

### Removendo a Última Migration

```bash
dotnet ef migrations remove -p WexCorporatePayments.Infrastructure -s WexCorporatePayments.Api
```

## ?? Cobertura de Testes

A solução inclui:

- **Testes Unitários de Domínio**: Validações de entidades e regras de negócio
- **Testes Unitários de Application**: Handlers e lógica de aplicação
- **Testes de Integração**: Endpoints da API com banco em memória

### Principais Cenários Testados

? Criação de transação com dados válidos  
? Validação de descrição (máx 50 caracteres)  
? Validação de valor positivo  
? Arredondamento para 2 casas decimais (ToEven)  
? Conversão de moeda com sucesso  
? Erro quando taxa não está disponível  
? Erro quando taxa está fora da janela de 6 meses  
? Endpoints retornam status codes corretos (201, 404, 422)  

## ?? Logs

A aplicação registra logs estruturados usando `ILogger`:

- **Information**: Operações bem-sucedidas (criação, conversão)
- **Warning**: Validações falhas, taxas não encontradas
- **Error**: Erros inesperados, falhas de rede

Exemplo de log:
```
info: WexCorporatePayments.Api.Controllers.TransactionsController[0]
      Transação criada com sucesso. Id: abc12345-6789-4def-gh12-ijklmnopqrst
```

## ?? Tratamento de Erros

A API retorna respostas padronizadas usando `ProblemDetails`:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Erro de validação",
  "status": 422,
  "detail": "Não foi possível encontrar uma taxa de câmbio válida para Brazil/Real..."
}
```

## ?? Recursos Adicionais

- [U.S. Treasury Fiscal Data API Documentation](https://fiscaldata.treasury.gov/api-documentation/)
- [ASP.NET Core Documentation](https://docs.microsoft.com/aspnet/core)
- [Entity Framework Core Documentation](https://docs.microsoft.com/ef/core)
- [xUnit Documentation](https://xunit.net/)
- [FluentAssertions Documentation](https://fluentassertions.com/)

## ?? Autor

Desenvolvido como parte do desafio técnico WexCorporatePayments.

## ?? Licença

Este projeto é privado e destinado apenas para fins de avaliação técnica.
