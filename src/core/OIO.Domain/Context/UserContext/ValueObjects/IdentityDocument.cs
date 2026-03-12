using CSharpFunctionalExtensions;
using OIO.Domain.Context.UserContext.Enums;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.UserContext.ValueObjects;

public sealed class IdentityDocument : ValueObject
{
    public IdType IdType { get; }
    public string IdNumber { get; }
    public DateOnly? IssuedDate { get; }
    public DateOnly? ExpiredDate { get; }
    public string? IssuedPlace { get; }

    public IdentityDocument()
    {
        
    }

    private IdentityDocument(
        IdType idType, string idNumber,
        DateOnly? issuedDate, DateOnly? expiredDate, string? issuedPlace)
    {
        IdType = idType;
        IdNumber = idNumber;
        IssuedDate = issuedDate;
        ExpiredDate = expiredDate;
        IssuedPlace = issuedPlace;
    }

    public static Result<IdentityDocument, Error> Create(
        IdType idType, string idNumber,
        DateOnly? issuedDate = null, 
        DateOnly? expiredDate = null, 
        string? issuedPlace = null)
    {
        if (string.IsNullOrWhiteSpace(idNumber))
            return Error.Validation("idNumber", "IdentityDocument.IdNumberEmpty", "ID number is required.");

        if (expiredDate.HasValue && issuedDate.HasValue && expiredDate.Value <= issuedDate.Value)
            return Error.Validation("idExpiredDate", "IdentityDocument.ExpiredBeforeIssued", "Expired date must be after issued date.");

        return new IdentityDocument(idType, idNumber.Trim(), issuedDate, expiredDate, issuedPlace?.Trim());
    }

    public bool IsExpired(DateOnly today) => ExpiredDate.HasValue && today > ExpiredDate.Value;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return IdType.Id;
        yield return IdNumber;
    }
}