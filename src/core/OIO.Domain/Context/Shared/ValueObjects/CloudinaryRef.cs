using CSharpFunctionalExtensions;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.Shared.ValueObjects;

public sealed class StorageRef : ValueObject
{
    public string PublicId { get; }
    public string Folder { get; }

    public StorageRef()
    {
        
    }

    private StorageRef(string publicId, string folder)
    {
        PublicId = publicId;
        Folder = folder;
    }

    public static Result<StorageRef, Error> Create(string publicId, string folder)
    {
        var check = StorageRef
            .Check()
            .Field(publicId)
            .NotWhiteSpace()
            .Field(folder)
            .NotWhiteSpace()
            .ToUnitResult();
        
        if (check.IsFailure)
            return check.Error;
        
        return new StorageRef(publicId.Trim(), folder.Trim());
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return PublicId;
        yield return Folder;
    }
}