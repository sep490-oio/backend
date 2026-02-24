using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Errors;
using OIO.Domain.Context.UserContext.ValueObjects;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.UserContext.Commands.AssignRole;

internal sealed class AssignRoleCommandHandler
    : ICommandHandler<AssignRoleCommand>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    
    public AssignRoleCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<UnitResult<Error>> Handle(
        AssignRoleCommand request,
        CancellationToken cancellationToken)
    {
        var nowUtc =  _clock.UtcNow;
        
        var userId = UserId.From(request.UserId);
        var roleId = RoleId.From(request.RoleId);
        
        var user = await _dbContext.GetByIdAsync<User, UserId>(
            userId,
            queryBuilder: query => query
                .Include(x => x.Roles),
            cancellationToken: cancellationToken);
        
        if (user is null)
            return UserErrors.User.NotFound(userId);

        var assignRoleResult = user.AssignRole(roleId, nowUtc);

        if (assignRoleResult.IsFailure)
        {
            return assignRoleResult;
        }

        _dbContext.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return assignRoleResult;
    }
}