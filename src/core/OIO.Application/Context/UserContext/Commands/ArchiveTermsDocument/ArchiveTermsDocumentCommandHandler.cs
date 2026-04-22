using CSharpFunctionalExtensions;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.DTOs;
using OIO.Application.Context.UserContext.Mappings;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Errors;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Commands.ArchiveTermsDocument;

/// <summary>
/// Manual archive of an <c>Active</c> <see cref="TermsDocument"/> (plan B3 / §3.3 Archive).
/// Reason is optional (Q1 ralplan §6); an auto-generated reason is used by the activation path
/// when superseding siblings — that path lives in <c>ActivateTermsDocumentCommandHandler</c>.
/// </summary>
internal sealed class ArchiveTermsDocumentCommandHandler
    : ICommandHandler<ArchiveTermsDocumentCommand, TermsDocumentDto>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public ArchiveTermsDocumentCommandHandler(
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
        ArchiveTermsDocumentCommand request,
        CancellationToken cancellationToken)
    {
        var id = TermsDocumentId.From(request.Id);
        var target = await _dbContext.GetByIdAsync<TermsDocument, TermsDocumentId>(
            id, cancellationToken: cancellationToken);

        if (target is null)
            return TermsErrors.TermsDocumentNotFound(id);

        var archiveResult = target.Archive(_clock.UtcNow, _currentUser.UserId, request.Reason);
        if (archiveResult.IsFailure)
            return archiveResult.Error;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return target.ToDto();
    }
}
