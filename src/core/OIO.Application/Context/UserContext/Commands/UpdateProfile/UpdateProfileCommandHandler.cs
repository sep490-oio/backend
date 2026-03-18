using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Media;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.MediaContext.Services;
using OIO.Application.Context.UserContext.DTOs;
using OIO.Application.Context.UserContext.Mappings;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.Shared.Entities;
using OIO.Domain.Context.Shared.Errors;
using OIO.Domain.Context.Shared.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Enums;
using OIO.Domain.Context.UserContext.Errors;
using OIO.Domain.Context.UserContext.ValueObjects;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Commands.UpdateProfile;

internal sealed class UpdateProfileCommandHandler
    : ICommandHandler<UpdateProfileCommand, UserProfileDto>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    private readonly UploadContextRegistry _contextRegistry;
    private readonly IMediaRelocationService _mediaRelocationService;

    public UpdateProfileCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IClock clock,
        UploadContextRegistry contextRegistry,
        IMediaRelocationService mediaRelocationService)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
        _contextRegistry = contextRegistry;
        _mediaRelocationService = mediaRelocationService;
    }

    public async Task<Result<UserProfileDto, Error>> Handle(
        UpdateProfileCommand request,
        CancellationToken cancellationToken)
    {
        var nowUtc = _clock.UtcNow;

        var user = await _dbContext.GetByIdAsync<User, UserId>(
            _currentUser.UserId,
            query => query.Include(x => x.Profile),
            cancellationToken);
        
        if (user is null)
            return UserErrors.User.NotFound(_currentUser.UserId);

        AvatarUrl? avatarUrl = null;
        MediaUpload? avatarUpload = null;

        if (request.AvatarMediaUploadId.HasValue)
        {
            var mediaUploadId = MediaUploadId.From(request.AvatarMediaUploadId.Value);
            avatarUpload = await _dbContext.GetByIdAsync<MediaUpload, MediaUploadId>(
                mediaUploadId,
                cancellationToken: cancellationToken);

            if (avatarUpload is null)
                return MediaErrors.NotFound(mediaUploadId);

            if (avatarUpload.UserId != _currentUser.UserId)
                return MediaErrors.NotOwnedByUser(mediaUploadId);

            if (!avatarUpload.IsConfirmed)
                return MediaErrors.NotConfirm;

            if (!_contextRegistry.IsUserAvatarContext(avatarUpload.Context))
                return MediaErrors.WrongContext(avatarUpload.Context, _contextRegistry.GetAllContext());

            if (string.IsNullOrWhiteSpace(avatarUpload.Info.SecureUrl))
                return MediaErrors.NotContainUrl;

            var avatarUrlResult = AvatarUrl.Create(avatarUpload.Info.SecureUrl);
            if (avatarUrlResult.IsFailure)
                return avatarUrlResult.Error;

            var linkResult = avatarUpload.LinkToEntity(user.Id, nowUtc);
            if (linkResult.IsFailure)
                return linkResult.Error;

            avatarUrl = avatarUrlResult.Value;
        }

        //if 'request.Gender' is null then it value when in string interpolation will be "",
        //then FromId will return null, and GetValueOrDefault will return
        var gender = Gender.FromId($"{request.Gender}").GetValueOrDefault();
        
        var personName = PersonName.Create(request.FirstName ?? user.Profile.Name?.FirstName, request.LastName ?? user.Profile.Name?.LastName, request.DisplayName ?? user.Profile.Name?.DisplayName);

        var updateProfileR = user.UpdateProfile(
            name: personName,
            avatarUrl: avatarUrl,
            dateOfBirth: request.DateOfBirth,
            gender: gender,
            now: nowUtc);

        if (updateProfileR.IsFailure)
        {
            return updateProfileR.Error;
        }

        if (avatarUpload is not null)
            await _mediaRelocationService.RelocateLinkedUploadAsync(avatarUpload, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return user.Profile!.ToDto();
    }
}
