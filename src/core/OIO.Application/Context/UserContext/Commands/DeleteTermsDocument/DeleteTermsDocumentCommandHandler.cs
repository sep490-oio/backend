using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Enums;
using OIO.Domain.Context.UserContext.Errors;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Commands.DeleteTermsDocument;

/// <summary>
/// Hard-deletes a <c>Draft</c> <see cref="TermsDocument"/> (plan B4). Rejects non-Draft and
/// any document with existing <see cref="TermsAcceptance"/> rows (legal retention — plan §3.1
/// principle #5). The linked <see cref="Domain.Context.Shared.Entities.MediaUpload"/> is left
/// on disk; orphan reaping is handled by <c>MediaUploadCleanupJob</c>.
/// </summary>
internal sealed class DeleteTermsDocumentCommandHandler
    : ICommandHandler<DeleteTermsDocumentCommand>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteTermsDocumentCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<UnitResult<Error>> Handle(
        DeleteTermsDocumentCommand request,
        CancellationToken cancellationToken)
    {
        var id = TermsDocumentId.From(request.Id);
        var target = await _dbContext.GetByIdAsync<TermsDocument, TermsDocumentId>(
            id, cancellationToken: cancellationToken);

        if (target is null)
            return TermsErrors.TermsDocumentNotFound(id);

        if (target.Status != TermsDocumentStatus.Draft)
        {
            return Error.Conflict(
                "TermsDocument.NotDeletable",
                $"Only draft terms documents can be deleted (current status: '{target.Status.Id}').");
        }

        // FK guard: even a Draft can have no acceptances in practice, but guard defensively.
        // An anomalous Draft referenced by an acceptance row must not be reaped — legal retention.
        var hasAcceptances = await _dbContext.Set<TermsAcceptance>()
            .AnyAsync(a => a.TermDocumentId == target.Id, cancellationToken);

        if (hasAcceptances)
        {
            return Error.Conflict(
                "TermsDocument.HasAcceptances",
                "Cannot delete a terms document with user acceptance history.");
        }

        _dbContext.Remove(target);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return UnitResult.Success<Error>();
    }
}
