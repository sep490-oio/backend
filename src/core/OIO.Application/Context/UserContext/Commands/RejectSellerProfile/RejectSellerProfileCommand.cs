using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Errors;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Commands.RejectSellerProfile;

public sealed record RejectSellerProfileCommand(Guid SellerId) : ICommand, IHasValidate
{
    public ViolationsError Validate()
    {
        return RejectSellerProfileCommand.Check()
            .WithOwnerName("RejectSellerProfile")
            .Field(SellerId).NotEmptyGuid();
    }
}

internal sealed class RejectSellerProfileCommandHandler
    : ICommandHandler<RejectSellerProfileCommand>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public RejectSellerProfileCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<UnitResult<Error>> Handle(
        RejectSellerProfileCommand request,
        CancellationToken cancellationToken)
    {
        var sellerId = UserId.From(request.SellerId);

        var profile = await _dbContext.Set<SellerProfile>()
            .FirstOrDefaultAsync(p => p.Id == sellerId, cancellationToken);

        if (profile is null)
            return UserErrors.SellerProfile.NotFoundById(sellerId);

        var result = profile.Reject(_clock.UtcNow);

        if (result.IsFailure)
            return result.Error;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return UnitResult.Success<Error>();
    }
}
