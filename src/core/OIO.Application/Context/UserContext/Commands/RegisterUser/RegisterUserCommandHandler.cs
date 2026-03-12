using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.DTOs;
using OIO.Application.Context.UserContext.Mappings;
using OIO.Domain.AppDefinitions;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Errors;
using OIO.Domain.Context.UserContext.Services;
using OIO.Domain.Context.UserContext.ValueObjects;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Commands.RegisterUser;

internal sealed class RegisterUserCommandHandler
    : ICommandHandler<RegisterUserCommand, UserDto>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IClock _clock;

    public RegisterUserCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IClock clock)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _clock = clock;
    }

    public async Task<Result<UserDto, Error>> Handle(
        RegisterUserCommand request,
        CancellationToken cancellationToken)
    {
        var (_, isFailure, email, error) = UserEmail.Create(request.Email);
        if (isFailure)
            return error;

        (_, isFailure, var userName, error) = UserName.Create(request.UserName);
        if (isFailure)
            return error;
        
        (_, isFailure, var password, error) = Password.Create(request.Password, _passwordHasher);
        if (isFailure)
            return error;

        var personName = PersonName.Create(request.FirstName, request.LastName);
        
        var existByEmail = await _dbContext.Set<User>().AnyAsync(x => x.Email == email, cancellationToken);
        
        if (existByEmail)
            return UserErrors.User.EmailAlreadyExists;

        var existByUserName = await _dbContext.Set<User>().AnyAsync(x => x.UserName == userName, cancellationToken);
        
        if (existByUserName)
            return UserErrors.User.UserNameAlreadyExists;
        
        var nowUtc = _clock.UtcNow;
        
        var user = User.Create(
            userName,
            email,
            nowUtc,
            password);

        user.UpdateProfile(nowUtc, personName);

        user.AssignRole(App.Roles.Definitions.Bidder.Name, nowUtc);
        user.AssignRole(App.Roles.Definitions.User.Name, nowUtc);
        
        _dbContext.Insert(user);
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return user.ToDto();
    }
}