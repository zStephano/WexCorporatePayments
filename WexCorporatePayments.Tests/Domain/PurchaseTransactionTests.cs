using FluentAssertions;
using WexCorporatePayments.Domain.Entities;
using WexCorporatePayments.Domain.Exceptions;

namespace WexCorporatePayments.Tests.Domain;

public class PurchaseTransactionTests
{
    [Fact]
    public void PurchaseTransaction_WithValidData_ShouldCreateSuccessfully()
    {
        // Arrange
        var description = "Laptop Dell";
        var transactionDate = new DateTime(2025, 1, 15);
        var amountUsd = 1234.56m;

        // Act
        var transaction = new PurchaseTransaction(description, transactionDate, amountUsd);

        // Assert
        transaction.Id.Should().NotBe(Guid.Empty);
        transaction.Description.Should().Be(description);
        transaction.TransactionDate.Should().Be(transactionDate);
        transaction.AmountUsd.Should().Be(1234.56m);
    }

    [Fact]
    public void PurchaseTransaction_WithAmountNeedingRounding_ShouldRoundToTwoDecimals()
    {
        // Arrange
        var description = "Service";
        var transactionDate = DateTime.Now;
        var amountUsd = 123.456m; // Should round to 123.46

        // Act
        var transaction = new PurchaseTransaction(description, transactionDate, amountUsd);

        // Assert
        transaction.AmountUsd.Should().Be(123.46m);
    }

    [Fact]
    public void PurchaseTransaction_WithAmountNeedingRoundingMidpoint_ShouldUseToEven()
    {
        // Arrange
        var description = "Service";
        var transactionDate = DateTime.Now;
        var amountUsd = 123.445m; // Should round to 123.44 (ToEven)

        // Act
        var transaction = new PurchaseTransaction(description, transactionDate, amountUsd);

        // Assert
        transaction.AmountUsd.Should().Be(123.44m);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void PurchaseTransaction_WithInvalidDescription_ShouldThrowDomainValidationException(string? description)
    {
        // Arrange
        var transactionDate = DateTime.Now;
        var amountUsd = 100m;

        // Act
        Action act = () => new PurchaseTransaction(description!, transactionDate, amountUsd);

        // Assert
        act.Should().Throw<DomainValidationException>()
            .WithMessage("Description is required.");
    }

    [Fact]
    public void PurchaseTransaction_WithDescriptionExceeding50Characters_ShouldThrowDomainValidationException()
    {
        // Arrange
        var description = new string('A', 51); // 51 characters
        var transactionDate = DateTime.Now;
        var amountUsd = 100m;

        // Act
        Action act = () => new PurchaseTransaction(description, transactionDate, amountUsd);

        // Assert
        act.Should().Throw<DomainValidationException>()
            .WithMessage("Description cannot exceed 50 characters.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100.50)]
    public void PurchaseTransaction_WithInvalidAmount_ShouldThrowDomainValidationException(decimal amountUsd)
    {
        // Arrange
        var description = "Product";
        var transactionDate = DateTime.Now;

        // Act
        Action act = () => new PurchaseTransaction(description, transactionDate, amountUsd);

        // Assert
        act.Should().Throw<DomainValidationException>()
            .WithMessage("Amount in USD must be greater than zero.");
    }

    [Fact]
    public void PurchaseTransaction_WithDefaultTransactionDate_ShouldThrowDomainValidationException()
    {
        // Arrange
        var description = "Product";
        var transactionDate = default(DateTime);
        var amountUsd = 100m;

        // Act
        Action act = () => new PurchaseTransaction(description, transactionDate, amountUsd);

        // Assert
        act.Should().Throw<DomainValidationException>()
            .WithMessage("Transaction date is required.");
    }
}
