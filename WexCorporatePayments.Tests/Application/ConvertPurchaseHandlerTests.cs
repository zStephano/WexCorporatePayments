using FluentAssertions;
using Moq;
using WexCorporatePayments.Application.Handlers;
using WexCorporatePayments.Application.Services;
using WexCorporatePayments.Domain.Entities;
using WexCorporatePayments.Domain.Repositories;

namespace WexCorporatePayments.Tests.Application;

public class ConvertPurchaseHandlerTests
{
    private readonly Mock<IPurchaseTransactionRepository> _repositoryMock;
    private readonly Mock<IExchangeRateService> _exchangeRateServiceMock;
    private readonly ConvertPurchaseHandler _handler;

    public ConvertPurchaseHandlerTests()
    {
        _repositoryMock = new Mock<IPurchaseTransactionRepository>();
        _exchangeRateServiceMock = new Mock<IExchangeRateService>();
        _handler = new ConvertPurchaseHandler(_repositoryMock.Object, _exchangeRateServiceMock.Object);
    }

    [Fact]
    public async Task HandleAsync_WithValidData_ShouldReturnConvertedPurchase()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var transactionDate = new DateTime(2025, 9, 30);
        var transaction = new PurchaseTransaction("Laptop", transactionDate, 1000m);

        var exchangeRate = new ExchangeRateResult
        {
            Country = "Brazil",
            Currency = "Real",
            ExchangeRate = 5.10m,
            RecordDate = new DateTime(2025, 9, 30)
        };

        _repositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(transaction);

        _exchangeRateServiceMock
            .Setup(s => s.GetLatestRateAsync("Brazil", "Real", transactionDate))
            .ReturnsAsync(exchangeRate);

        // Act
        var result = await _handler.HandleAsync(transactionId, "Brazil", "Real");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(transaction.Id);
        result.Description.Should().Be("Laptop");
        result.AmountUsd.Should().Be(1000m);
        result.ExchangeRate.Should().Be(5.10m);
        result.ConvertedAmount.Should().Be(5100.00m);
        result.Country.Should().Be("Brazil");
        result.Currency.Should().Be("Real");
        result.RecordDate.Should().Be(new DateTime(2025, 9, 30));
    }

    [Fact]
    public async Task HandleAsync_WithRoundingNeeded_ShouldRoundToTwoDecimalsWithToEven()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var transactionDate = new DateTime(2025, 9, 30);
        var transaction = new PurchaseTransaction("Product", transactionDate, 123.45m);

        var exchangeRate = new ExchangeRateResult
        {
            Country = "Brazil",
            Currency = "Real",
            ExchangeRate = 5.123m,
            RecordDate = transactionDate
        };

        _repositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(transaction);

        _exchangeRateServiceMock
            .Setup(s => s.GetLatestRateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()))
            .ReturnsAsync(exchangeRate);

        // Act
        var result = await _handler.HandleAsync(transactionId, "Brazil", "Real");

        // Assert
        // 123.45 * 5.123 = 632.42835, should round to 632.43
        result!.ConvertedAmount.Should().Be(632.43m);
    }

    [Fact]
    public async Task HandleAsync_WhenTransactionNotFound_ShouldReturnNull()
    {
        // Arrange
        var transactionId = Guid.NewGuid();

        _repositoryMock
            .Setup(r => r.GetByIdAsync(transactionId))
            .ReturnsAsync((PurchaseTransaction?)null);

        // Act
        var result = await _handler.HandleAsync(transactionId, "Brazil", "Real");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_WhenExchangeRateNotFound_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var transaction = new PurchaseTransaction("Laptop", DateTime.Now, 1000m);

        _repositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(transaction);

        _exchangeRateServiceMock
            .Setup(s => s.GetLatestRateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()))
            .ReturnsAsync((ExchangeRateResult?)null);

        // Act
        Func<Task> act = async () => await _handler.HandleAsync(transactionId, "InvalidCountry", "InvalidCurrency");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*valid exchange rate*");
    }

    [Fact]
    public async Task HandleAsync_WhenExchangeRateOutsideSixMonthWindow_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var transactionDate = new DateTime(2025, 9, 30);
        var transaction = new PurchaseTransaction("Laptop", transactionDate, 1000m);

        var exchangeRate = new ExchangeRateResult
        {
            Country = "Brazil",
            Currency = "Real",
            ExchangeRate = 5.10m,
            RecordDate = new DateTime(2025, 3, 1) // More than 6 months before
        };

        _repositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(transaction);

        _exchangeRateServiceMock
            .Setup(s => s.GetLatestRateAsync("Brazil", "Real", transactionDate))
            .ReturnsAsync(exchangeRate);

        // Act
        Func<Task> act = async () => await _handler.HandleAsync(transactionId, "Brazil", "Real");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*outside the valid period*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HandleAsync_WithInvalidCountry_ShouldThrowArgumentException(string? country)
    {
        // Act
        Func<Task> act = async () => await _handler.HandleAsync(Guid.NewGuid(), country!, "Real");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Country is required*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HandleAsync_WithInvalidCurrency_ShouldThrowArgumentException(string? currency)
    {
        // Act
        Func<Task> act = async () => await _handler.HandleAsync(Guid.NewGuid(), "Brazil", currency!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Currency is required*");
    }
}
