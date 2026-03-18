using CSharpFunctionalExtensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.UserContext.ValueObjects;

public sealed class RecipientInfo : ValueObject
{
    public string RecipientName { get; }
    public PhoneNumber Phone { get; }

    public RecipientInfo()
    {
        
    }
    
    private RecipientInfo(string recipientName, PhoneNumber phone)
    {
        RecipientName = recipientName;
        Phone = phone;
    }

    public static Result<RecipientInfo, Error> Create(
        string recipientName, 
        string phoneNumber, 
        string countryCode)
    {
        if (string.IsNullOrWhiteSpace(recipientName))
            return Error.Validation("recipientName", "RecipientInfo.NameEmpty", "Recipient name is required.");

        var phoneResult = PhoneNumber.Create(phoneNumber, countryCode);
        
        if (phoneResult.IsFailure) return 
            phoneResult.Error;

        return new RecipientInfo(recipientName.Trim(), phoneResult.Value);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return RecipientName;
        foreach (var c in Phone.GetEqualityComponentsPublic())
            yield return c;
    }
}