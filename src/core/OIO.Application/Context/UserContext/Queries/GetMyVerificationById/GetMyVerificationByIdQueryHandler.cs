using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.DTOs;
using OIO.Application.Context.UserContext.Mappings;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Errors;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Queries.GetMyVerificationById;

internal sealed class GetMyVerificationByIdQueryHandler
    : IQueryHandler<GetMyVerificationByIdQuery, VerificationDto>
{
    private readonly IDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public GetMyVerificationByIdQueryHandler(IDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<VerificationDto, Error>> Handle(
        GetMyVerificationByIdQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var verificationId = IdentityVerificationId.From(request.VerificationId);

        var verification = await _dbContext.GetByIdAsync<IdentityVerification, IdentityVerificationId>(
            verificationId,
            q => q.AsNoTracking().Include(v => v.Documents),
            cancellationToken);

        if (verification is null || verification.UserId != userId)
            return UserErrors.Verification.NotFound(verificationId);

        return verification.ToDto();
    }
}
