using System.Net;
using CSharpFunctionalExtensions;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.PaymentContext.Aggregates.Wallets;
using OIO.Domain.Context.Shared.Enums;
using OIO.Domain.Context.UserContext.Aggregates.Roles;
using OIO.Domain.Context.UserContext.Aggregates.Users.Events;
using OIO.Domain.Context.UserContext.Enums;
using OIO.Domain.Context.UserContext.Errors;
using OIO.Domain.Context.UserContext.ValueObjects;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
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
    private readonly List<UserRefreshToken> _refreshTokens = [];
    private readonly List<UserSession> _sessions = [];
    private readonly List<AuctionDeposit> _auctionDeposits = [];
    private readonly List<AuctionEmergency> _emergencies = [];

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
    
    public int Version { get; private set; }

    public bool IsDeleted => DeletedAt is not null;
    
    public Wallet Wallet { get; private set; }
    public UserProfile Profile { get; private set; }
    public SellerProfile SellerProfile { get; private set; }
    public IReadOnlyCollection<UserAddress> Addresses => _addresses.AsReadOnly();
    public IReadOnlyCollection<UserRole> Roles => _roles.AsReadOnly();
    public IReadOnlyCollection<UserPermission> Permissions => _permissions.AsReadOnly();
    public IReadOnlyCollection<UserLoginHistory> LoginHistories => _loginHistories.AsReadOnly();
    public IReadOnlyCollection<UserSession> Sessions => _sessions.AsReadOnly();
    public IReadOnlyCollection<UserRefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();
    public IReadOnlyCollection<AuctionDeposit> AuctionDeposits => _auctionDeposits.AsReadOnly();
    public IReadOnlyCollection<AuctionEmergency> Emergencies => _emergencies.AsReadOnly();
    
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
        PersonName personName,
        Currency currency,
        Password? password = null)
    {
        var user = new User(
            UserId.From(Guid.CreateVersion7()),
            userName,
            email,
            now,
            password);

        // Initialize profile
        user.Profile = new UserProfile(user.Id, now);
        user.Profile.Update(
            now: now,
            name: personName);
        
        user.Wallet = Wallet.Create(user.Id, currency, now);

        user.RaiseDomainEvent(new UserCreatedEvent(
            user.Id.ToString(),
            user.UserName, 
            user.Email, 
            now));

        return user;
    }
    
    public UnitResult<Error> ChangePassword(
        Password newPassword,
        DateTime nowUtc)
    {
        var unitResult = EnsureNotDeleted()
            .Bind(() => EnsureNotLockedOut(nowUtc));

        if (unitResult.IsFailure)
        {
            return unitResult.Error;
        }
        
        Password = newPassword;
        ModifiedAt = nowUtc;

        RaiseDomainEvent(new UserPasswordChangedEvent(Id.ToString(), nowUtc));
        
        return unitResult;
    }
    
    public UnitResult<Error> RequestEmailVerification(DateTime nowUtc)
    {
        var unitResult = EnsureNotDeleted()
            .Bind(() => EnsureNotLockedOut(nowUtc));

        if (unitResult.IsFailure)
        {
            return unitResult.Error;
        }
        
        if (EmailConfirmedAt is not null)
            return UserErrors.User.EmailNotConfirmed;

        RaiseDomainEvent(new EmailVerificationRequestedEvent(
            UserId:$"{Id}",
            Email: Email.Value,
            UserName: UserName.Value,
            OccurredAt: nowUtc));

        return UnitResult.Success<Error>();
    }
    
    public UnitResult<Error> RequestPasswordReset(DateTime nowUtc)
    {
        var unitResult = EnsureNotDeleted()
            .Bind(() => EnsureNotLockedOut(nowUtc));

        if (unitResult.IsFailure)
        {
            return unitResult.Error;
        }

        if (EmailConfirmedAt is null)
            return UserErrors.User.EmailNotConfirmed;

        RaiseDomainEvent(new PasswordResetRequestedEvent(
            UserId: $"{Id}",
            Email: Email.Value,
            UserName: UserName.Value,
            OccurredAt: nowUtc));

        return UnitResult.Success<Error>();
    }
    
    public UnitResult<Error> ChangeEmail(
        UserEmail newEmail,
        DateTime nowUtc)
    {
        var unitResult = EnsureNotDeleted()
            .Bind(() => EnsureNotLockedOut(nowUtc));

        if (unitResult.IsFailure)
        {
            return unitResult.Error;
        }
        
        Email = newEmail;
        EmailConfirmed = false;
        EmailConfirmedAt = null;
        ModifiedAt = nowUtc;
        
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

        RaiseDomainEvent(new UserEmailConfirmedEvent(Id.ToString(), Email, now));
        
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

        RaiseDomainEvent(new UserPhoneConfirmedEvent(Id.ToString(), PhoneNumber, now));
        
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

        RaiseDomainEvent(new UserStatusChangedEvent(Id.ToString(), oldStatus.Id, newStatus.Id, now));
        
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
            RaiseDomainEvent(new UserLockedOutEvent(Id.ToString(), LockoutEnd.Value, AccessFailedCount, now));
        }

        RaiseDomainEvent(new LoginAttemptedEvent(Id.ToString(), ipAddress.ToString(), userAgent, false, now));

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

        RaiseDomainEvent(new LoginAttemptedEvent(Id.ToString(), ipAddress.ToString(), userAgent, true, now));

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
        PersonName? name,
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
        
        unitResult = Profile.Update(now, name, avatarUrl, dateOfBirth, gender);
        
        if (unitResult.IsFailure)
        {
            return unitResult.Error;
        }
        ModifiedAt = now;
        
        return unitResult;
    }
    
    public Result<UserAddress, Error> AddAddress(
        AddressType type,
        RecipientInfo recipient,
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

        var userAddress = new UserAddress(
            Id,
            type,
            recipient,
            address,
            now,
            isDefault);
        _addresses.Add(userAddress);
        ModifiedAt = now;

        return userAddress;
    }
    
    public UnitResult<Error> UpdateAddress(
        UserAddressId addressId,
        DateTime now,
        AddressType? type = null,
        RecipientInfo? recipient = null,
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

        unitResult = existing.Update(type, recipient, address, now);

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
    
    public UnitResult<Error> AssignRole(string roleId, DateTime now)
    {
        var unitResult = EnsureNotDeleted();
        
        if (unitResult.IsFailure)
        {
            return unitResult.Error;
        }

        if (_roles.Any(r => r.RoleName == roleId))
            return unitResult;

        _roles.Add(new UserRole(Id, roleId, now));
        
        ModifiedAt = now;
        
        return unitResult;
    }
    
    public int GetMaxRoleLevel()
    {
        return _roles.Count == 0
            ? 0
            : _roles.Max(x => x.Role.Level);
    }

    public bool CanManage(User targetUser)
    {
        ArgumentNullException.ThrowIfNull(targetUser);
        return GetMaxRoleLevel() > targetUser.GetMaxRoleLevel();
    }
    
    public UnitResult<Error> RevokeRole(string roleName, DateTime now)
    {
        var unitResult = EnsureNotDeleted();
        
        if (unitResult.IsFailure)
        {
            return unitResult.Error;
        }

        var role = _roles.FirstOrDefault(r => r.RoleName == roleName);

        if (role is null) 
            return  unitResult;

        _roles.Remove(role);
        
        ModifiedAt = now;
        
        return unitResult;
    }
    
    public bool HasRole(string roleName) => _roles.Any(r => r.RoleName.Equals(roleName, StringComparison.CurrentCultureIgnoreCase));
    
    public UnitResult<Error> GrantPermission(string permissionCode, DateTime now)
    {
        var unitResult = EnsureNotDeleted();
        
        if (unitResult.IsFailure)
        {
            return unitResult.Error;
        }

        var existing = _permissions.FirstOrDefault(p => p.PermissionCode == permissionCode);
        if (existing is not null)
        {
            existing.Grant();
            return unitResult;
        }

        _permissions.Add(UserPermission.Grant(Id, permissionCode));
        
        ModifiedAt = now;
        
        return unitResult;
    }

    public UnitResult<Error> DenyPermission(string permissionCode, DateTime now)
    {
        var unitResult = EnsureNotDeleted();
        
        if (unitResult.IsFailure)
        {
            return unitResult.Error;
        }


        var existing = _permissions.FirstOrDefault(p => p.PermissionCode == permissionCode);
        if (existing is not null)
        {
            existing.Deny();
            return  unitResult;
        }

        _permissions.Add(UserPermission.Deny(Id, permissionCode));
        
        ModifiedAt = now;
        
        return unitResult;
    }

    public UnitResult<Error> RevokePermission(string permissionCode, DateTime now)
    {
        var unitResult = EnsureNotDeleted();
        
        if (unitResult.IsFailure)
        {
            return unitResult.Error;
        }

        var permission = _permissions.FirstOrDefault(p => p.PermissionCode == permissionCode);
        
        if (permission is null)
        {
            return unitResult;
        }

        _permissions.Remove(permission);
        
        ModifiedAt = now;
        
        return unitResult;
    }
    
    public Result<UserSession, Error> CreateSession(
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
        var activeSessions = _sessions
            .Where(f => f.IsActive && !f.IsExpired(now)).ToList();
        
        if (activeSessions.Count >= MaxTokenFamilies)
        {
            // Revoke oldest family
            var oldest = activeSessions
                .OrderBy(f => f.CreatedAt)
                .First();
            oldest.Revoke("Max active families exceeded", now);

            RaiseDomainEvent(new SessionRevokedEvent(Id.ToString(), oldest.Id.ToString(), oldest.DeviceId, "Max active families exceeded", now));
        }

        // Check if device already has an active family — revoke it
        var existingForDevices = _sessions
            .Where(f => f.DeviceId == deviceId && f.IsActive);

        foreach (var existingForDevice in existingForDevices)
        {
            existingForDevice.Revoke("New session started on same device", now);
        }

        var session = UserSession
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
        
        _sessions.Add(session.Value);

        return session;
    }
    
    public Result<UserRefreshToken, Error> CreateRefreshToken(
        UserSessionId sessionId,
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

        var family = _sessions.FirstOrDefault(f => f.Id == sessionId);

        return family?.CreateToken(tokenHash, ipAddress, timeRefreshTokenExpiration, now) ?? UserErrors.Auth.SessionNotFound;
    }

    public Result<UserRefreshToken, Error> RotateRefreshToken(
        UserSessionId sessionId,
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

        var family = _sessions.FirstOrDefault(f => f.Id == sessionId);

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
        
        RaiseDomainEvent(new RefreshTokenRotatedEvent(Id.ToString(), sessionId.ToString(), currentToken.Id.ToString(), newToken.Value.Id.ToString(), now));
        
        if (family.IsNearingAbsoluteExpiration(now))
        {
            RaiseDomainEvent(new SessionNearingExpirationEvent(
                UserId: Id.ToString(),
                SessionId: sessionId.ToString(),
                AbsoluteExpiresAt: family.AbsoluteExpiresAt,
                RemainingTime: family.RemainingAbsoluteTime(now),
                now));
        }

        return newToken;
    }
    
    public void RevokeSessionByDevice(Guid deviceId, string reason, DateTime now)
    {
        var session = _sessions
            .FirstOrDefault(f => f.DeviceId == deviceId && f.IsActive);
        
        if (session is null) 
            return;

        session.Revoke(reason, now);

        RaiseDomainEvent(new SessionRevokedEvent(Id.ToString(), session.Id.ToString(), deviceId, reason, now));
    }
    
    public void RevokeSession(UserSessionId sessionId, string reason, DateTime now)
    {
        var session = _sessions.FirstOrDefault(f => f.Id == sessionId);
        
        if (session is null || !session.IsActive) 
            return;

        session.Revoke(reason, now);
        RaiseDomainEvent(new SessionRevokedEvent(Id.ToString(), session.Id.ToString(), session.DeviceId, reason, now));
    }

    public void RevokeAllSession(string reason, DateTime now)
    {
        foreach (var session in _sessions.Where(f => f.IsActive))
        {
            session.Revoke(reason, now);
            RaiseDomainEvent(new SessionRevokedEvent(Id.ToString(), session.Id.ToString(), session.DeviceId, reason, now));
        }
    }
    
    public void SoftDelete(DateTime deletedAt)
    {
        if (IsDeleted) 
            return;

        DeletedAt = deletedAt;
        Status = UserStatus.Inactive;
        ModifiedAt = deletedAt;
        
        RevokeAllSession("User account deleted", deletedAt);
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