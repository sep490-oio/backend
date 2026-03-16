using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.AppDefinitions;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Errors;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Commands.VerifySellerProfile;

public sealed record VerifySellerProfileCommand(Guid SellerId) : ICommand, IHasValidate
{
    public ViolationsError Validate()
    {
        return VerifySellerProfileCommand.Check()
            .WithOwnerName("VerifySellerProfile")
            .Field(SellerId).NotEmptyGuid();
    }
}

internal sealed class VerifySellerProfileCommandHandler
    : ICommandHandler<VerifySellerProfileCommand>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPermissionService _permissionService;
    private readonly IClock _clock;

    public VerifySellerProfileCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        IPermissionService permissionService,
        IClock clock)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _permissionService = permissionService;
        _clock = clock;
    }

    public async Task<UnitResult<Error>> Handle(
        VerifySellerProfileCommand request,
        CancellationToken cancellationToken)
    {
        var sellerId = UserId.From(request.SellerId);
        
        var user = await _dbContext.GetByIdAsync<User, UserId>(
            id: sellerId,
            queryBuilder: query => query
                .Include(u => u.SellerProfile),
            cancellationToken: cancellationToken
        );

        if (user == null)
            return UserErrors.User.NotFound(sellerId);
        
        if (user.SellerProfile is null)
            return UserErrors.SellerProfile.NotFoundById(sellerId);

        var result = user.SellerProfile.Verify(_clock.UtcNow);

        if (result.IsFailure)
            return result.Error;

        result = user.AssignRole(App.Roles.Catalogs.Seller, _clock.UtcNow);
        
        if (result.IsFailure)
            return result.Error;
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _permissionService.InvalidatePermissionsCacheAsync(sellerId, cancellationToken);
        return UnitResult.Success<Error>();
    }
}
