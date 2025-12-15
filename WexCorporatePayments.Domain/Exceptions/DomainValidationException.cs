namespace WexCorporatePayments.Domain.Exceptions;

/// <summary>
/// Exception thrown when domain rules are violated.
/// </summary>
public class DomainValidationException : Exception
{
    public DomainValidationException(string message) : base(message)
    {
    }

    public DomainValidationException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}
