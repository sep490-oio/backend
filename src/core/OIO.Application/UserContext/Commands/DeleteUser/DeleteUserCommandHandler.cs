using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Errors;
using OIO.Domain.Context.UserContext.ValueObjects;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.UserContext.Commands.DeleteUser;

internal sealed class DeleteUserCommandHandler
    : ICommandHandler<DeleteUserCommand>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public DeleteUserCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<UnitResult<Error>> Handle(
        DeleteUserCommand request,
        CancellationToken cancellationToken)
    {
        var nowUtc = _clock.UtcNow;
        
        var userId = UserId.From(request.UserId);
        
        var user = await _dbContext.GetByIdAsync<User, UserId>(userId,
            queryBuilder: query => query
                .Include(x => x.RefreshTokenFamilies)
                .ThenInclude(x => x.Tokens),
            cancellationToken: cancellationToken);
        
        if (user is null)
            return UserErrors.User.NotFound(userId);

        user.SoftDelete(nowUtc);
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return UnitResult.Success<Error>();
    }
}