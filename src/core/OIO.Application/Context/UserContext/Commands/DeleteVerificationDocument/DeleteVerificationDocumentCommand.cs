using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Errors;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Commands.DeleteVerificationDocument;

public sealed record DeleteVerificationDocumentCommand(
    Guid VerificationId,
    Guid DocumentId) : ICommand, IHasValidate
{
    public ViolationsError Validate()
    {
        return DeleteVerificationDocumentCommand.Check()
            .WithOwnerName("DeleteVerificationDocument")
            .Field(VerificationId).NotEmptyGuid()
            .Field(DocumentId).NotEmptyGuid();
    }
}

internal sealed class DeleteVerificationDocumentCommandHandler
    : ICommandHandler<DeleteVerificationDocumentCommand>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ICurrentUser _currentUser;

    public DeleteVerificationDocumentCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        IClock clock,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _currentUser = currentUser;
    }

    public async Task<UnitResult<Error>> Handle(
        DeleteVerificationDocumentCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var verificationId = IdentityVerificationId.From(request.VerificationId);

        var verification = await _dbContext.GetByIdAsync<IdentityVerification, IdentityVerificationId>(
            verificationId,
            q => q.Include(v => v.Documents),
            cancellationToken);

        if (verification is null || verification.UserId != userId)
            return UserErrors.Verification.NotFound(verificationId);

        var documentId = VerificationDocumentId.From(request.DocumentId);
        var result = verification.RemoveDocument(documentId, _clock.UtcNow);

        if (result.IsFailure)
            return result.Error;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return UnitResult.Success<Error>();
    }
}
