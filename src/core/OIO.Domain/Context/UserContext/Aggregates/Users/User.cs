using System.Net;
using CSharpFunctionalExtensions;
using OIO.Domain.Context.UserContext.Aggregates.Users.Events;
using OIO.Domain.Context.UserContext.Enums;
using OIO.Domain.Context.UserContext.Errors;
using OIO.Domain.Context.UserContext.ValueObjects;
using OIO.Domain.SeedWork.Entities;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.UserContext.Aggregates.Users;

public sealed class User : AggregateRoot<UserId>, IAuditableEntity, ISoftDeletableEntity, IVersionEntity
{
    private const int MaxFailedAccessAttempts = 5;
    private const int DefaultLockoutMinutes = 30;
    private const int MaxAddresses = 10;
    private const int MaxTokenFamilies = 5;
    
    private readonly List<UserAddress> _addresses = [];
    private readonly List<UserRole> _roles = [];
    private readonly List<UserPermission> _permissions = [];
    private readonly List<UserLoginHistory> _loginHistories = [];
    private readonly List<UserRefreshTokenFamily> _refreshTokenFamilies = [];

    private User() {}
    
    public UserName UserName { get; private set; }

    public UserEmail Email { get; private set; } 

    public bool EmailConfirmed { get; private set; }

    public DateTime? EmailConfirmedAt { get; private set; }

    public Password? Password { get; private set; }

    public PhoneNumber? PhoneNumber { get; private set; }

    public bool PhoneNumberConfirmed { get; private set; }

    public DateTime? PhoneNumberConfirmedAt { get; private set; }

    public bool TwoFactorEnabled { get; private set; }

    public TwoFactorProvider TwoFactorProvider { get; private set; }

    public UserStatus Status { get; private set; }

    public bool LockoutEnabled { get; private set; }
    
    public string? LockoutReason { get; private set; }

    public DateTime? LockoutEnd { get; private set; }

    public short AccessFailedCount { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? ModifiedAt { get; private set; }

    public DateTime? DeletedAt { get; private set; }
    
    public bool IsDeleted => DeletedAt is not null;

    public int Version { get; private set; }
    
    public UserProfile? Profile { get; private set; }
    public IReadOnlyList<UserAddress> Addresses => _addresses;
    public IReadOnlyList<UserRole> Roles => _roles;
    public IReadOnlyList<UserPermission> Permissions => _permissions;
    public IReadOnlyList<UserLoginHistory> LoginHistories => _loginHistories;
    public IReadOnlyList<UserRefreshTokenFamily> RefreshTokenFamilies => _refreshTokenFamilies;
    
    private User(
        UserId userId,
        UserName userName, 
        UserEmail email, 
        DateTime now,
        Password? password = null)
    {
        Id = userId;
        UserName = userName;
        Email = email;
        Password = password;
        EmailConfirmed = false;
        PhoneNumberConfirmed = false;
        TwoFactorEnabled = false;
        TwoFactorProvider = TwoFactorProvider.None;
        Status = UserStatus.Inactive;
        LockoutEnabled = true;
        AccessFailedCount = 0;
        Version = 0;
        CreatedAt = now;
    }
    
    public static User Create(
        UserName userName, 
        UserEmail email, 
        DateTime now,
        Password? password = null)
    {
        var user = new User(
            UserId.Create(),
            userName,
            email,
            now,
            password);

        // Initialize profile
        user.Profile = new UserProfile(user.Id, now);

        user.RaiseDomainEvent(new UserCreatedEvent(
            user.Id.GetValueAsString(),
            user.UserName, 
            user.Email, 
            now));

        return user;
    }
    
    public UnitResult<Error> ChangePassword(
        Password newPassword,
        DateTime now)
    {
        var unitResult = EnsureNotDeleted();

        if (unitResult.IsFailure)
        {
            return unitResult.Error;
        }
        
        Password = newPassword;
        ModifiedAt = now;

        RaiseDomainEvent(new UserPasswordChangedEvent(Id.GetValueAsString(), now));
        
        return unitResult;
    }
    
    public UnitResult<Error> ChangeEmail(
        UserEmail newEmail,
        DateTime now)
    {
        var unitResult = EnsureNotDeleted();

        if (unitResult.IsFailure)
        {
            return unitResult.Error;
        }
        
        Email = newEmail;
        EmailConfirmed = false;
        EmailConfirmedAt = null;
        ModifiedAt = now;
        
        return unitResult;
    }
    
    public UnitResult<Error> ConfirmEmail(DateTime now)
    {
        var unitResult = EnsureNotDeleted();

        if (unitResult.IsFailure)
        {
            return unitResult.Error;
        }
        
        if (EmailConfirmed) 
            return unitResult;

        EmailConfirmed = true;
        EmailConfirmedAt = now;


        if (Status == UserStatus.Inactive)
        {
            unitResult = ChangeStatus(UserStatus.Active, now);
            
            return unitResult;
        }

        ModifiedAt = now;

        RaiseDomainEvent(new UserEmailConfirmedEvent(Id.GetValueAsString(), Email, now));
        
        return unitResult;
    }
    
    public UnitResult<Error> SetPhoneNumber(
        PhoneNumber phoneNumber,
        DateTime now)
    {
        var unitResult = EnsureNotDeleted();

        if (unitResult.IsFailure)
        {
            return unitResult.Error;
        }
        
        PhoneNumber = phoneNumber;
        PhoneNumberConfirmed = false;
        PhoneNumberConfirmedAt = null;
        ModifiedAt = now;
        
        return unitResult;
    }

    public UnitResult<Error> ConfirmPhoneNumber(DateTime now)
    {
        var unitResult = EnsureNotDeleted();

        if (unitResult.IsFailure)
        {
            return unitResult.Error;
        }
        
        if (PhoneNumber is null)
        {
            return UserErrors.User.PhoneNotSet;
        }

        PhoneNumberConfirmed = true;
        PhoneNumberConfirmedAt = now;
        ModifiedAt = now;

        RaiseDomainEvent(new UserPhoneConfirmedEvent(Id.GetValueAsString(), PhoneNumber, now));
        
        return unitResult;
    }
    
    public UnitResult<Error> EnableTwoFactor(
        TwoFactorProvider provider,
        DateTime now)
    {
        var unitResult = EnsureNotDeleted();

        if (unitResult.IsFailure)
        {
            return unitResult.Error;
        }

        
        if (provider == TwoFactorProvider.None)
        {
            return UserErrors.User.InvalidTwoFactorProvider;
        }

        if (provider == TwoFactorProvider.Sms && !PhoneNumberConfirmed)
        {
            return UserErrors.User.PhoneNumberNotConfirmed;
        };

        if (provider == TwoFactorProvider.Email && !EmailConfirmed)
        {
            return UserErrors.User.EmailNotConfirmed;
        };

        TwoFactorEnabled = true;
        TwoFactorProvider = provider;
        ModifiedAt = now;

        return unitResult;
    }
    
    public UnitResult<Error> DisableTwoFactor(DateTime now)
    {
        var unitResult = EnsureNotDeleted();

        if (unitResult.IsFailure)
        {
            return unitResult.Error;
        }
        
        TwoFactorEnabled = false;
        TwoFactorProvider = TwoFactorProvider.None;
        ModifiedAt = now;
        
        return unitResult;
    }
    
    public UnitResult<Error> ChangeStatus(
        UserStatus newStatus,
        DateTime now)
    {
        var unitResult = EnsureNotDeleted();

        if (unitResult.IsFailure)
        {
            return unitResult.Error;
        }

        if (Status == newStatus)
        {
            return unitResult;
        }

        var oldStatus = Status;
        Status = newStatus;
        ModifiedAt = now;

        RaiseDomainEvent(new UserStatusChangedEvent(Id.GetValueAsString(), oldStatus.Id, newStatus.Id, now));
        
        return unitResult;
    }
    
    public UnitResult<Error> RecordFailedLogin(
        IPAddress ipAddress, 
        string userAgent,
        DateTime now)
    {
        var unitResult = EnsureNotDeleted();

        if (unitResult.IsFailure)
        {
            return unitResult.Error;
        }
        
        AccessFailedCount++;
        

        _loginHistories.Add(new UserLoginHistory(Id, ipAddress, userAgent, LoginStatus.Failed, now));

        if (LockoutEnabled && AccessFailedCount >= MaxFailedAccessAttempts)
        {
            LockoutEnd = now.AddMinutes(DefaultLockoutMinutes);
            RaiseDomainEvent(new UserLockedOutEvent(Id.GetValueAsString(), LockoutEnd.Value, AccessFailedCount, now));
        }

        RaiseDomainEvent(new LoginAttemptedEvent(Id.GetValueAsString(), ipAddress.ToString(), userAgent, false, now));

        return unitResult;
    }
    
    public UnitResult<Error> RecordSuccessfulLogin(
        IPAddress ipAddress,
        string userAgent,
        DateTime now)
    {
        var unitResult = EnsureNotDeleted()
            .Bind(() => EnsureNotLockedOut(now));

        if (unitResult.IsFailure)
        {
            return unitResult.Error;
        }
        
        AccessFailedCount = 0;
        LockoutEnd = null;

        _loginHistories.Add(new UserLoginHistory(Id, ipAddress, userAgent, LoginStatus.Success, now));

        RaiseDomainEvent(new LoginAttemptedEvent(Id.GetValueAsString(), ipAddress.ToString(), userAgent, true, now));

        return unitResult;
    }
    
    public bool IsLockedOut(DateTime now) =>
        LockoutEnabled && LockoutEnd.HasValue && LockoutEnd.Value > now;
    
    public void Unlock(DateTime now)
    {
        LockoutEnd = null;
        LockoutReason = null;
        AccessFailedCount = 0;
        ModifiedAt = now;
    }
    
    public UnitResult<Error> UpdateProfile(
        DateTime now,
        FirstName? firstName = null,
        LastName? lastName = null,
        DisplayName? displayName = null,
        AvatarUrl? avatarUrl = null,
        DateOnly? dateOfBirth = null,
        Gender? gender = null)
    {
        var unitResult = EnsureNotDeleted();
        
        if (unitResult.IsFailure)
        {
            return unitResult.Error;
        }
        
        Profile ??= new UserProfile(Id, now);
        
        unitResult = Profile.Update(firstName, lastName, displayName, avatarUrl, dateOfBirth, gender, now);
        
        if (unitResult.IsFailure)
        {
            return unitResult.Error;
        }
        ModifiedAt = now;
        
        return unitResult;
    }
    
    public Result<UserAddress, Error> AddAddress(
        AddressType type,
        string recipientName,
        PhoneNumber phoneNumber,
        Address address,
        DateTime now,
        bool isDefault = false)
    {
        var unitResult = EnsureNotDeleted();
        
        if (unitResult.IsFailure)
        {
            return unitResult.Error;
        }

        if (_addresses.Count >= MaxAddresses)
            return UserErrors.User.MaxAddressesReached(MaxAddresses);

        if (isDefault || _addresses.Count == 0)
        {
            foreach (var existing in _addresses)
                existing.UnsetDefault();

            isDefault = true;
        }

        var userAddress = new UserAddress(Id, type, recipientName, phoneNumber, address, now, isDefault);
        _addresses.Add(userAddress);
        ModifiedAt = now;

        return userAddress;
    }
    
    public UnitResult<Error> UpdateAddress(
        UserAddressId addressId,
        DateTime now,
        AddressType? type = null,
        string? recipientName = null,
        PhoneNumber? phoneNumber = null,
        Address? address = null)
    {
        var unitResult = EnsureNotDeleted();
        
        if (unitResult.IsFailure)
        {
            return unitResult.Error;
        }

        var existing = _addresses.FirstOrDefault(a => a.Id == addressId);
        if (existing == null)
        {
            return UserErrors.User.AddressNotFound(addressId);
        }

        unitResult = existing.Update(type, recipientName, phoneNumber, address, now);

        if (unitResult.IsFailure)
        {
            return unitResult.Error;
        }
        
        ModifiedAt = now;
        
        return unitResult;
    }
    
    public UnitResult<Error> SetDefaultAddress(
        UserAddressId addressId,
        DateTime now)
    {
        var unitResult = EnsureNotDeleted();
        
        if (unitResult.IsFailure)
        {
            return unitResult.Error;
        }

        var address = _addresses.FirstOrDefault(a => a.Id == addressId);
        if (address == null)
        {
            return UserErrors.User.AddressNotFound(addressId);
        }

        foreach (var a in _addresses)
            a.UnsetDefault();

        address.SetAsDefault();
        ModifiedAt = now;
        
        return unitResult;
    }
    
    public UnitResult<Error> RemoveAddress(
        UserAddressId addressId,
        DateTime now)
    {
        var unitResult = EnsureNotDeleted();
        
        if (unitResult.IsFailure)
        {
            return unitResult.Error;
        }

        var address = _addresses.FirstOrDefault(a => a.Id == addressId);
        if (address == null)
        {
            return UserErrors.User.AddressNotFound(addressId);
        }

        var wasDefault = address.IsDefault;
        _addresses.Remove(address);
        
        if (wasDefault && _addresses.Count > 0)
            _addresses[0].SetAsDefault();

        ModifiedAt = now;
        
        return unitResult;
    }
    
    public UnitResult<Error> AssignRole(RoleId roleId, DateTime now)
    {
        var unitResult = EnsureNotDeleted();
        
        if (unitResult.IsFailure)
        {
            return unitResult.Error;
        }

        if (_roles.Any(r => r.RoleId == roleId))
            return unitResult;

        _roles.Add(new UserRole(Id, roleId, now));
        
        ModifiedAt = now;
        
        return unitResult;
    }

    public UnitResult<Error> RemoveRole(RoleId roleId, DateTime now)
    {
        var unitResult = EnsureNotDeleted();
        
        if (unitResult.IsFailure)
        {
            return unitResult.Error;
        }

        var role = _roles.FirstOrDefault(r => r.RoleId == roleId);

        if (role is null) 
            return  unitResult;

        _roles.Remove(role);
        
        ModifiedAt = now;
        
        return unitResult;
    }
    
    public bool HasRole(RoleId roleId) => _roles.Any(r => r.RoleId == roleId);
    
    public UnitResult<Error> GrantPermission(PermissionId permissionId, DateTime now)
    {
        var unitResult = EnsureNotDeleted();
        
        if (unitResult.IsFailure)
        {
            return unitResult.Error;
        }


        var existing = _permissions.FirstOrDefault(p => p.PermissionId == permissionId);
        if (existing is not null)
        {
            existing.SetAllowed(true);
            return unitResult;
        }

        _permissions.Add(new UserPermission(Id, permissionId, isAllowed: true));
        
        ModifiedAt = now;
        
        return unitResult;
    }

    public UnitResult<Error> DenyPermission(PermissionId permissionId, DateTime now)
    {
        var unitResult = EnsureNotDeleted();
        
        if (unitResult.IsFailure)
        {
            return unitResult.Error;
        }


        var existing = _permissions.FirstOrDefault(p => p.PermissionId == permissionId);
        if (existing is not null)
        {
            existing.SetAllowed(false);
            return  unitResult;
        }

        _permissions.Add(new UserPermission(Id, permissionId, isAllowed: false));
        
        ModifiedAt = now;
        
        return unitResult;
    }

    public UnitResult<Error> RemovePermission(PermissionId permissionId, DateTime now)
    {
        var unitResult = EnsureNotDeleted();
        
        if (unitResult.IsFailure)
        {
            return unitResult.Error;
        }


        var permission = _permissions.FirstOrDefault(p => p.PermissionId == permissionId);
        if (permission is null)
        {
            return unitResult;
        }

        _permissions.Remove(permission);
        
        ModifiedAt = now;
        
        return unitResult;
    }
    
    public Result<UserRefreshTokenFamily, Error> CreateSession(
        Guid deviceId,
        string userAgent,
        IPAddress ipAddress,
        TimeSpan slidingExpiration,
        TimeSpan absoluteExpiration,
        DateTime now)
    {
        var unitResult = EnsureNotDeleted()
            .Bind(() => EnsureNotLockedOut(now));

        if (unitResult.IsFailure)
        {
            return unitResult.Error;
        }

        // Enforce max active families
        var activeSessions = _refreshTokenFamilies
            .Where(f => f.IsActive && !f.IsExpired(now)).ToList();
        
        if (activeSessions.Count >= MaxTokenFamilies)
        {
            // Revoke oldest family
            var oldest = activeSessions
                .OrderBy(f => f.CreatedAt)
                .First();
            oldest.Revoke("Max active families exceeded", now);

            RaiseDomainEvent(new SessionRevokedEvent(Id.GetValueAsString(), oldest.Id.GetValueAsString(), oldest.DeviceId, "Max active families exceeded", now));
        }

        // Check if device already has an active family — revoke it
        var existingForDevices = _refreshTokenFamilies
            .Where(f => f.DeviceId == deviceId && f.IsActive);

        foreach (var existingForDevice in existingForDevices)
        {
            existingForDevice.Revoke("New session started on same device", now);
        }

        var session = UserRefreshTokenFamily
            .Create(
                Id,
                deviceId,
                userAgent,
                ipAddress,
                slidingExpiration, 
                absoluteExpiration,
                now);

        if (session.IsFailure)
        {
            return session.Error;
        }
        
        _refreshTokenFamilies.Add(session.Value);

        return session;
    }
    
    public Result<UserRefreshToken, Error> CreateRefreshToken(
        UserRefreshTokenFamilyId sessionId,
        string tokenHash,
        IPAddress ipAddress,
        TimeSpan timeRefreshTokenExpiration,
        DateTime now)
    {
        var unitResult = EnsureNotDeleted();

        if (unitResult.IsFailure)
        {
            return unitResult.Error;
        }

        var family = _refreshTokenFamilies.FirstOrDefault(f => f.Id == sessionId);

        return family?.CreateToken(tokenHash, ipAddress, timeRefreshTokenExpiration, now) ?? UserErrors.Auth.SessionNotFound;
    }

    public Result<UserRefreshToken, Error> RotateRefreshToken(
        UserRefreshTokenFamilyId sessionId,
        UserRefreshToken currentToken,
        string newTokenHash,
        IPAddress ipAddress,
        TimeSpan timeRefreshTokenExpiration,
        TimeSpan timeRefreshFamilySlidingExpiration,
        DateTime now
        )
    {
        var unitResult = EnsureNotDeleted();

        if (unitResult.IsFailure)
        {
            return unitResult.Error;
        }

        var family = _refreshTokenFamilies.FirstOrDefault(f => f.Id == sessionId);

        if (family is null)
        {
            return UserErrors.Auth.SessionNotFound;
        }

        var newToken = family.RotateToken(
            currentToken,
            newTokenHash,
            ipAddress, 
            timeRefreshTokenExpiration, 
            timeRefreshFamilySlidingExpiration,
            now);

        if (newToken.IsFailure)
        {
            return newToken.Error;
        }
        
        RaiseDomainEvent(new RefreshTokenRotatedEvent(Id.GetValueAsString(), sessionId.GetValueAsString(), currentToken.Id.GetValueAsString(), newToken.Value.Id.GetValueAsString(), now));
        
        if (family.IsNearingAbsoluteExpiration(now))
        {
            RaiseDomainEvent(new SessionNearingExpirationEvent(
                UserId: Id.GetValueAsString(),
                SessionId: sessionId.GetValueAsString(),
                AbsoluteExpiresAt: family.AbsoluteExpiresAt,
                RemainingTime: family.RemainingAbsoluteTime(now),
                now));
        }

        return newToken;
    }
    
    public void RevokeTokenFamilyByDevice(Guid deviceId, string reason, DateTime now)
    {
        var session = _refreshTokenFamilies
            .FirstOrDefault(f => f.DeviceId == deviceId && f.IsActive);
        
        if (session is null) 
            return;

        session.Revoke(reason, now);

        RaiseDomainEvent(new SessionRevokedEvent(Id.GetValueAsString(), session.Id.GetValueAsString(), deviceId, reason, now));
    }
    
    public void RevokeTokenFamily(UserRefreshTokenFamilyId sessionId, string reason, DateTime now)
    {
        var session = _refreshTokenFamilies.FirstOrDefault(f => f.Id == sessionId);
        
        if (session is null || !session.IsActive) 
            return;

        session.Revoke(reason, now);
        RaiseDomainEvent(new SessionRevokedEvent(Id.GetValueAsString(), session.Id.GetValueAsString(), session.DeviceId, reason, now));
    }

    public void RevokeAllTokenFamilies(string reason, DateTime now)
    {
        foreach (var session in _refreshTokenFamilies.Where(f => f.IsActive))
        {
            session.Revoke(reason, now);
            RaiseDomainEvent(new SessionRevokedEvent(Id.GetValueAsString(), session.Id.GetValueAsString(), session.DeviceId, reason, now));
        }
    }
    
    public void SoftDelete(DateTime deletedAt)
    {
        if (IsDeleted) 
            return;

        DeletedAt = deletedAt;
        Status = UserStatus.Inactive;
        ModifiedAt = deletedAt;
        
        RevokeAllTokenFamilies("User account deleted", deletedAt);
    }
    
    public UnitResult<Error> EnsureNotDeleted()
    {
        return UnitResult.FailureIf<Error>(
            IsDeleted,
            UserErrors.User.UserDeleted
            );
    }

    public UnitResult<Error> EnsureNotLockedOut(DateTime now)
    {
        return UnitResult.FailureIf<Error>(
            IsLockedOut(now),
            UserErrors.User.Locked(Id, LockoutEnd, LockoutReason)
        );
    }
}