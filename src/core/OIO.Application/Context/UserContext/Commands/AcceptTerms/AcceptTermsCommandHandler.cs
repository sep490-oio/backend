using System.Net;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
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

namespace OIO.Application.Context.UserContext.Commands.AcceptTerms;

internal sealed class AcceptTermsCommandHandler : ICommandHandler<AcceptTermsCommand, TermsAcceptanceDto>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public AcceptTermsCommandHandler(
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

    public async Task<Result<TermsAcceptanceDto, Error>> Handle(AcceptTermsCommand request, CancellationToken cancellationToken)
    {
        var termDocumentId = TermsDocumentId.From(request.TermDocumentId);
        var termDocument = await _dbContext.GetByIdAsync<TermsDocument, TermsDocumentId>(
            termDocumentId,
            cancellationToken: cancellationToken);

        if (termDocument is null)
            return TermsErrors.TermsDocumentNotFound(termDocumentId);

        if (!termDocument.IsActive)
            return TermsErrors.CannotAcceptInactiveTerms;

        var alreadyAccepted = await _dbContext.Set<TermsAcceptance>()
            .AsNoTracking()
            .AnyAsync(x => x.UserId == _currentUser.UserId && x.TermDocumentId == termDocumentId, cancellationToken);

        if (alreadyAccepted)
            return TermsErrors.AlreadyAccepted(termDocumentId);

        IPAddress? ipAddress = null;
        if (!string.IsNullOrWhiteSpace(request.IpAddress))
            IPAddress.TryParse(request.IpAddress, out ipAddress);

        var acceptanceResult = TermsAcceptance.Create(
            _currentUser.UserId,
            termDocumentId,
            _clock.UtcNow,
            ipAddress,
            request.UserAgent);

        if (acceptanceResult.IsFailure)
            return acceptanceResult.Error;

        var acceptance = acceptanceResult.Value;
        
        try
        {
            _dbContext.Insert(acceptance);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message?.Contains("uq_user_terms_acceptances_user_term") == true)
        {
            // Race condition: another request accepted the same term concurrently.
            // Return the existing acceptance (idempotent behavior).
            acceptance = await _dbContext.Set<TermsAcceptance>()
                .AsNoTracking()
                .Include(x => x.TermDocument)
                .FirstAsync(x => x.UserId == _currentUser.UserId && x.TermDocumentId == termDocumentId, cancellationToken);
            return acceptance.ToDto();
        }

        acceptance = await _dbContext.Set<TermsAcceptance>()
            .AsNoTracking()
            .Include(x => x.TermDocument)
            .FirstAsync(x => x.Id == acceptance.Id, cancellationToken);

        return acceptance.ToDto();
    }
}
