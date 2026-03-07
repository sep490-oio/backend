using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Errors;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Commands.SetDefaultAddress;

internal sealed class SetDefaultAddressCommandHandler
    : ICommandHandler<SetDefaultAddressCommand>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public SetDefaultAddressCommandHandler(
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

    public async Task<UnitResult<Error>> Handle(
        SetDefaultAddressCommand request,
        CancellationToken cancellationToken)
    {
        var nowUtc =  _clock.UtcNow;
        

        var user = await _dbContext.GetByIdAsync<User, UserId>(
            _currentUser.UserId,
            query => query.Include(x => x.Addresses),
            cancellationToken);
        
        if (user is null)
            return UserErrors.User.NotFound(_currentUser.UserId);
        
        var addressId = UserAddressId.From(request.AddressId);

        var setDefaultAddressResult = user.SetDefaultAddress(addressId, nowUtc);

        if (setDefaultAddressResult.IsFailure)
        {
            return setDefaultAddressResult;
        }

        _dbContext.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return setDefaultAddressResult;
    }
}