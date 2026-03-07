using OIO.Domain.Context.Shared.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.Shared.Errors;

public static class MediaErrors
{
    public static Error NotFound(MediaUploadId id)
    {
        return Error.NotFound(
            "Media.NotFound",
            $"Media upload with id {id} not found."); 
    }

    public static Error NotFounds(string ids)
    {
        return Error.NotFound(
            "Media.NotFound",
            $"Media upload not found with these id: {ids}."); 
    }
    
    public static Error NotOwnedByUser(MediaUploadId id)
    {
        return Error.Forbidden(
            "Media.NotOwnedByUser",
            $"Media upload with id {id} is not owned by the user."); 
    }
    
    public static Error NotOwnedByUser(string ids)
    {
        return Error.Forbidden(
            "Media.NotOwnedByUser",
            $"Media upload with these id: {ids} is not owned by the user."); 
    }
    
    public static readonly Error NotConfirm = Error.Conflict(
        "Media.NotConfirm",
        "Media upload is not confirmed yet.");
    
    public static readonly Error AlreadyLinked = Error.Conflict(
        "Media.AlreadyLinked",
        "Media upload is already linked to another entity.");
    
    public static Error WrongContext(string type, string[] validContext) => Error.Conflict(
        "Media.WrongContext",
        $"Media upload is not valid for the context of {type}. Valid contexts are: {string.Join(", ", validContext)}.");
    
    
    
    public static readonly  Error PublicIdMismatch = Error.Conflict(
        "Media.PublicIdMismatch",
        "The provided public ID does not match the expected value.");
    
    
}