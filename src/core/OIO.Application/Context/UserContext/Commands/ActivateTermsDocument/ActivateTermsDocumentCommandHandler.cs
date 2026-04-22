using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.DTOs;
using OIO.Application.Context.UserContext.Mappings;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Enums;
using OIO.Domain.Context.UserContext.Errors;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Commands.ActivateTermsDocument;

/// <summary>
/// Activates a <c>Draft</c> <see cref="TermsDocument"/>, auto-archiving a sibling of the same
/// <see cref="TermsDocument.TermType"/> that is currently <c>Active</c> (plan §3.3 / B1).
///
/// Idempotent replay (plan B1 AC): calling this handler on a document already <c>Active</c>
/// returns <c>Success(existingDto)</c> — byte-for-byte equal to the original response — so the
/// admin UI can re-render consistently without branching on a no-op sentinel.
///
/// Sibling-conflict 409: returned only when more than one sibling of the same TermType is
/// already Active (data anomaly). The normal single-active supersede path auto-archives the
/// sibling in-handler with reason <c>"Superseded by v{N}"</c>.
/// </summary>
internal sealed class ActivateTermsDocumentCommandHandler
    : ICommandHandler<ActivateTermsDocumentCommand, TermsDocumentDto>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public ActivateTermsDocumentCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IClock clock)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result<TermsDocumentDto, Error>> Handle(
        ActivateTermsDocumentCommand request,
        CancellationToken cancellationToken)
    {
        var id = TermsDocumentId.From(request.Id);
        var target = await _dbContext.GetByIdAsync<TermsDocument, TermsDocumentId>(
            id, cancellationToken: cancellationToken);

        if (target is null)
            return TermsErrors.TermsDocumentNotFound(id);

        // Idempotent replay: already Active with the same Id. Return the existing DTO —
        // byte-for-byte equal to what the original activation returned (plan B1 AC).
        if (target.Status == TermsDocumentStatus.Active)
        {
            return target.ToDto();
        }

        // Archived is terminal — cannot be re-activated.
        if (target.Status == TermsDocumentStatus.Archived)
        {
            return Error.Conflict(
                "TermsDocument.InvalidState",
                "Archived documents cannot be re-activated.");
        }

        // Target is Draft. Find Active siblings of the same TermType.
        var siblings = await _dbContext.Set<TermsDocument>()
            .Where(x => x.TermType == target.TermType
                        && x.Id != target.Id
                        && x.Status == TermsDocumentStatus.Active)
            .ToListAsync(cancellationToken);

        // Data-anomaly guard: more than one Active sibling means the invariant is already
        // broken. Refuse to activate and let an admin resolve before we compound the problem.
        if (siblings.Count > 1)
        {
            return Error.Conflict(
                "TermsDocument.ActiveSiblingExists",
                $"Multiple active '{target.TermType}' documents already exist. Archive the extras before activating a new version.");
        }

        var nowUtc = _clock.UtcNow;
        var activatedBy = _currentUser.UserId;
        TermsDocumentId? supersededId = null;

        // Auto-archive the single Active sibling with generated reason (Q1 ralplan §6).
        if (siblings.Count == 1)
        {
            var sibling = siblings[0];
            var archiveResult = sibling.Archive(
                nowUtc,
                activatedBy,
                $"Superseded by v{target.Version}");

            if (archiveResult.IsFailure)
                return archiveResult.Error;

            supersededId = sibling.Id;
        }

        var activateResult = target.Activate(nowUtc, activatedBy, supersededId);
        if (activateResult.IsFailure)
            return activateResult.Error;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return target.ToDto();
    }
}
