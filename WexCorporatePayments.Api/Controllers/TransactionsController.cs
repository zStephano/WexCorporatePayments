using Microsoft.AspNetCore.Mvc;
using WexCorporatePayments.Application.DTOs;
using WexCorporatePayments.Application.Handlers;
using WexCorporatePayments.Domain.Exceptions;

namespace WexCorporatePayments.Api.Controllers;

/// <summary>
/// Controller for managing purchase transactions.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TransactionsController : ControllerBase
{
    private readonly CreatePurchaseTransactionHandler _createHandler;
    private readonly ConvertPurchaseHandler _convertHandler;
    private readonly ILogger<TransactionsController> _logger;

    public TransactionsController(
        CreatePurchaseTransactionHandler createHandler,
        ConvertPurchaseHandler convertHandler,
        ILogger<TransactionsController> logger)
    {
        _createHandler = createHandler ?? throw new ArgumentNullException(nameof(createHandler));
        _convertHandler = convertHandler ?? throw new ArgumentNullException(nameof(convertHandler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a new purchase transaction in USD.
    /// </summary>
    /// <param name="request">Transaction data</param>
    /// <returns>Created transaction id</returns>
    /// <response code="201">Transaction created successfully</response>
    /// <response code="400">Invalid data</response>
    /// <response code="422">Domain validation error</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateTransaction([FromBody] CreatePurchaseTransactionRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var id = await _createHandler.HandleAsync(request);

            _logger.LogInformation("Transaction created successfully. Id: {TransactionId}", id);

            return CreatedAtAction(
                nameof(ConvertTransaction),
                new { id },
                new { id });
        }
        catch (DomainValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error creating transaction");
            return UnprocessableEntity(new ProblemDetails
            {
                Status = StatusCodes.Status422UnprocessableEntity,
                Title = "Validation error",
                Detail = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating transaction");
            return StatusCode(500, new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Internal error",
                Detail = "An error occurred while processing the request."
            });
        }
    }

    /// <summary>
    /// Converts a purchase transaction to a foreign currency.
    /// </summary>
    /// <param name="id">Transaction id</param>
    /// <param name="country">Country (e.g. Brazil)</param>
    /// <param name="currency">Currency (e.g. Real)</param>
    /// <returns>Converted transaction data</returns>
    /// <response code="200">Conversion performed successfully</response>
    /// <response code="404">Transaction not found</response>
    /// <response code="422">Exchange rate not available for the period</response>
    [HttpGet("{id:guid}/convert")]
    [ProducesResponseType(typeof(ConvertedPurchaseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ConvertTransaction(
        [FromRoute] Guid id,
        [FromQuery] string country,
        [FromQuery] string currency)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(country))
            {
                return BadRequest(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Invalid parameter",
                    Detail = "The 'country' parameter is required."
                });
            }

            if (string.IsNullOrWhiteSpace(currency))
            {
                return BadRequest(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Invalid parameter",
                    Detail = "The 'currency' parameter is required."
                });
            }

            var result = await _convertHandler.HandleAsync(id, country, currency);

            if (result == null)
            {
                _logger.LogWarning("Transaction not found. Id: {TransactionId}", id);
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Transaction not found",
                    Detail = $"Transaction with Id {id} was not found."
                });
            }

            _logger.LogInformation(
                "Conversion performed successfully. Id: {TransactionId}, Country: {Country}, Currency: {Currency}",
                id, country, currency);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Exchange rate not available");
            return UnprocessableEntity(new ProblemDetails
            {
                Status = StatusCodes.Status422UnprocessableEntity,
                Title = "Exchange rate not available",
                Detail = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting transaction");
            return StatusCode(500, new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Internal error",
                Detail = "An error occurred while processing the request."
            });
        }
    }
}
