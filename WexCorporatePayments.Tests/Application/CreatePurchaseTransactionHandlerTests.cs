using FluentAssertions;
using Moq;
using WexCorporatePayments.Application.DTOs;
using WexCorporatePayments.Application.Handlers;
using WexCorporatePayments.Domain.Entities;
using WexCorporatePayments.Domain.Exceptions;
using WexCorporatePayments.Domain.Repositories;

namespace WexCorporatePayments.Tests.Application;

public class CreatePurchaseTransactionHandlerTests
{
    private readonly Mock<IPurchaseTransactionRepository> _repositoryMock;
    private readonly CreatePurchaseTransactionHandler _handler;

    public CreatePurchaseTransactionHandlerTests()
    {
        _repositoryMock = new Mock<IPurchaseTransactionRepository>();
        _handler = new CreatePurchaseTransactionHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task HandleAsync_WithValidRequest_ShouldCreateTransactionAndReturnId()
    {
        // Arrange
        var request = new CreatePurchaseTransactionRequest
        {
            Description = "Laptop",
            TransactionDate = new DateTime(2025, 8, 15),
            AmountUsd = 1234.56m
        };

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<PurchaseTransaction>()))
            .Returns(Task.CompletedTask);

        // Act
        var id = await _handler.HandleAsync(request);

        // Assert
        id.Should().NotBe(Guid.Empty);
        _repositoryMock.Verify(r => r.AddAsync(It.Is<PurchaseTransaction>(t =>
            t.Description == request.Description &&
            t.TransactionDate == request.TransactionDate &&
            t.AmountUsd == request.AmountUsd
        )), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithNullRequest_ShouldThrowArgumentNullException()
    {
        // Act
        Func<Task> act = async () => await _handler.HandleAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task HandleAsync_WithInvalidDescription_ShouldThrowDomainValidationException()
    {
        // Arrange
        var request = new CreatePurchaseTransactionRequest
        {
            Description = new string('A', 51), // More than 50 characters
            TransactionDate = DateTime.Now,
            AmountUsd = 100m
        };

        // Act
        Func<Task> act = async () => await _handler.HandleAsync(request);

        // Assert
        await act.Should().ThrowAsync<DomainValidationException>();
    }
}
