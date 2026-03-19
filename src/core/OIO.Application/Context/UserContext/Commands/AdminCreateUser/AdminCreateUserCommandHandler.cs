using System.Security.Cryptography;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.AppDefinitions;
using OIO.Domain.Context.Shared.Enums;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Errors;
using OIO.Domain.Context.UserContext.Services;
using OIO.Domain.Context.UserContext.ValueObjects;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Commands.AdminCreateUser;

internal sealed class AdminCreateUserCommandHandler
    : ICommandHandler<AdminCreateUserCommand, AdminUserCreatedDto>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public AdminCreateUserCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ICurrentUser currentUser,
        IClock clock)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result<AdminUserCreatedDto, Error>> Handle(
        AdminCreateUserCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Validate value objects
        var (_, isFailure, email, error) = UserEmail.Create(request.Email);
        if (isFailure)
            return error;

        (_, isFailure, var userName, error) = UserName.Create(request.UserName);
        if (isFailure)
            return error;

        // 2. Generate or validate password
        string? temporaryPassword = null;
        string plainPassword;

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            temporaryPassword = GenerateTemporaryPassword();
            plainPassword = temporaryPassword;
        }
        else
        {
            plainPassword = request.Password;
        }

        (_, isFailure, var password, error) = Password.Create(plainPassword, _passwordHasher);
        if (isFailure)
            return error;

        var currency = Currency.FromId(request.Currency);
        if (currency.HasNoValue)
            return Currency.Errors.NotSupported;

        var personName = PersonName.Create(request.FirstName, request.LastName, request.DisplayName ?? request.UserName);

        // 3. Check uniqueness
        var existByEmail = await _dbContext.Set<User>()
            .AnyAsync(x => x.Email == email, cancellationToken);

        if (existByEmail)
            return UserErrors.User.EmailAlreadyExists;

        var existByUserName = await _dbContext.Set<User>()
            .AnyAsync(x => x.UserName == userName, cancellationToken);

        if (existByUserName)
            return UserErrors.User.UserNameAlreadyExists;

        // 4. Validate roles and check escalation prevention
        var actorId = _currentUser.UserId;

        var actor = await _dbContext.GetByIdAsync<User, UserId>(
            actorId,
            queryBuilder: q => q
                .Include(x => x.Roles)
                .ThenInclude(x => x.Role),
            cancellationToken: cancellationToken);

        if (actor is null)
            return UserErrors.User.NotFound(actorId);

        var actorMaxLevel = actor.GetMaxRoleLevel();

        var requestedRoles = request.Roles is { Count: > 0 }
            ? request.Roles
            : new List<string> { App.Roles.Catalogs.Bidder, App.Roles.Catalogs.User };

        foreach (var roleName in requestedRoles)
        {
            if (!App.Roles.Definitions.All.TryGetValue(roleName, out var roleDef))
                return RoleErrors.Role.NotFound(roleName);

            // Actor must have higher level than the role being assigned
            if (actorMaxLevel <= roleDef.Level)
                return RoleErrors.Role.CannotAssignHigherOrEqualRole;
        }

        // 5. Create user
        var nowUtc = _clock.UtcNow;

        var user = User.Create(
            userName,
            email,
            nowUtc,
            personName,
            currency.Value,
            password);

        // 6. Confirm email if requested
        if (request.EmailConfirmed)
        {
            var confirmResult = user.ConfirmEmail(nowUtc);
            if (confirmResult.IsFailure)
                return confirmResult.Error;
        }

        // 7. Assign roles
        foreach (var roleName in requestedRoles)
        {
            user.AssignRole(roleName, nowUtc);
        }

        // 8. Clear domain events if skip notifications
        if (request.SkipNotifications)
        {
            user.ClearDomainEvents();
        }

        // 9. Persist
        _dbContext.Insert(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 10. Return DTO
        var assignedRoles = requestedRoles.ToList().AsReadOnly();

        return new AdminUserCreatedDto(
            UserId: user.Id.Value,
            UserName: user.UserName,
            Email: user.Email.Value,
            Status: user.Status.Id,
            Roles: assignedRoles,
            EmailConfirmed: user.EmailConfirmed,
            TemporaryPassword: temporaryPassword);
    }

    private static string GenerateTemporaryPassword()
    {
        const string upperChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lowerChars = "abcdefghijklmnopqrstuvwxyz";
        const string digitChars = "0123456789";
        const string specialChars = "!@#$%^&*";
        const string allChars = upperChars + lowerChars + digitChars + specialChars;
        const int length = 12;

        Span<char> password = stackalloc char[length];

        // Ensure at least one of each required character type
        password[0] = upperChars[RandomNumberGenerator.GetInt32(upperChars.Length)];
        password[1] = lowerChars[RandomNumberGenerator.GetInt32(lowerChars.Length)];
        password[2] = digitChars[RandomNumberGenerator.GetInt32(digitChars.Length)];
        password[3] = specialChars[RandomNumberGenerator.GetInt32(specialChars.Length)];

        // Fill the rest randomly
        for (var i = 4; i < length; i++)
        {
            password[i] = allChars[RandomNumberGenerator.GetInt32(allChars.Length)];
        }

        // Shuffle using Fisher-Yates
        for (var i = length - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (password[i], password[j]) = (password[j], password[i]);
        }

        return new string(password);
    }
}
