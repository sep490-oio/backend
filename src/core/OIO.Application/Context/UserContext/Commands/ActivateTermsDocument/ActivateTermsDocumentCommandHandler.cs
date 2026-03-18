using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Errors;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Commands.ActivateTermsDocument;

internal sealed class ActivateTermsDocumentCommandHandler : ICommandHandler<ActivateTermsDocumentCommand>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ActivateTermsDocumentCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<UnitResult<Error>> Handle(ActivateTermsDocumentCommand request, CancellationToken cancellationToken)
    {
        var id = TermsDocumentId.From(request.Id);
        var target = await _dbContext.GetByIdAsync<TermsDocument, TermsDocumentId>(id, cancellationToken: cancellationToken);

        if (target is null)
            return TermsErrors.TermsDocumentNotFound(id);

        var siblings = await _dbContext.Set<TermsDocument>()
            .Where(x => x.TermType == target.TermType && x.Id != target.Id && x.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var sibling in siblings)
            sibling.Deactivate();

        var activateResult = target.Activate(_clock.UtcNow);
        if (activateResult.IsFailure)
            return activateResult.Error;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return UnitResult.Success<Error>();
    }
}
