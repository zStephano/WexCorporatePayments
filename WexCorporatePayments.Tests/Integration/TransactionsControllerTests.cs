using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WexCorporatePayments.Application.DTOs;
using WexCorporatePayments.Infrastructure.Persistence;

namespace WexCorporatePayments.Tests.Integration;

public class TransactionsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public TransactionsControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove o DbContext existente
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));

                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Adiciona DbContext usando banco em memória
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDatabase");
                });

                // Garante que o banco está criado
                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var scopedServices = scope.ServiceProvider;
                var db = scopedServices.GetRequiredService<AppDbContext>();
                db.Database.EnsureCreated();
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task CreateTransaction_WithValidData_ShouldReturn201Created()
    {
        // Arrange
        var request = new CreatePurchaseTransactionRequest
        {
            Description = "Laptop Dell",
            TransactionDate = new DateTime(2025, 8, 15),
            AmountUsd = 1234.56m
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/transactions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        result.Should().ContainKey("id");
        var id = result!["id"].GetGuid();
        id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task CreateTransaction_WithInvalidData_ShouldReturn400BadRequest()
    {
        // Arrange
        var request = new
        {
            Description = new string('A', 51), // Excede 50 caracteres
            TransactionDate = DateTime.Now,
            AmountUsd = 100m
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/transactions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTransaction_WithNegativeAmount_ShouldReturn422UnprocessableEntity()
    {
        // Arrange
        var request = new CreatePurchaseTransactionRequest
        {
            Description = "Product",
            TransactionDate = DateTime.Now,
            AmountUsd = -100m
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/transactions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ConvertTransaction_WithNonExistentId_ShouldReturn404NotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/transactions/{nonExistentId}/convert?country=Brazil&currency=Real");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ConvertTransaction_WithMissingCountryParameter_ShouldReturn400BadRequest()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/transactions/{id}/convert?currency=Real");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ConvertTransaction_WithMissingCurrencyParameter_ShouldReturn400BadRequest()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/transactions/{id}/convert?country=Brazil");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
