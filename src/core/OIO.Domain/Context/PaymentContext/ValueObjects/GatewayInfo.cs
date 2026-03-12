using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.PaymentContext.ValueObjects;

public sealed class GatewayInfo : ValueObject
{
    public string? Provider { get; }
    public string? TransactionId { get; }
    public string? Response { get; }  // jsonb

    public GatewayInfo()
    {
        
    }
    
    private GatewayInfo(
        string? provider,
        string? transactionId,
        string? response)
    {
        Provider = provider;
        TransactionId = transactionId;
        Response = response;
    }

    public static GatewayInfo Create(
        string? provider,
        string? transactionId,
        string? response = null)
        => new(provider, transactionId, response);

    public static GatewayInfo Empty => new(null, null, null);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Provider ?? string.Empty;
        yield return TransactionId ?? string.Empty;
    }
}