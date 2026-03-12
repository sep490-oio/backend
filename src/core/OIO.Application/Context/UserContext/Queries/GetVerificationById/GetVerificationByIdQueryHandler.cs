using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.DTOs;
using OIO.Application.Context.UserContext.Mappings;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Errors;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Queries.GetVerificationById;

internal sealed class GetVerificationByIdQueryHandler
    : IQueryHandler<GetVerificationByIdQuery, VerificationDto>
{
    private readonly IDbContext _dbContext;

    public GetVerificationByIdQueryHandler(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<VerificationDto, Error>> Handle(
        GetVerificationByIdQuery request,
        CancellationToken cancellationToken)
    {
        var verificationId = IdentityVerificationId.From(request.VerificationId);

        var verification = await _dbContext.GetByIdAsync<IdentityVerification, IdentityVerificationId>(
            verificationId,
            q => q.AsNoTracking().Include(v => v.Documents),
            cancellationToken);

        if (verification is null)
            return UserErrors.Verification.NotFound(verificationId);

        return verification.ToDto();
    }
}
