using CSharpFunctionalExtensions;
using OIO.Application.Abstractions.Auth;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Errors;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Commands.SetupTotp;

public sealed record SetupTotpCommand : ICommand<SetupTotpResponse>;

public sealed record SetupTotpResponse(string SharedKey, string QrCodeBase64);

internal sealed class SetupTotpCommandHandler
    : ICommandHandler<SetupTotpCommand, SetupTotpResponse>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    private readonly ITotpService _totpService;
    private readonly IAppInfo _appInfo;

    public SetupTotpCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IClock clock,
        ITotpService totpService,
        IAppInfo appInfo)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
        _totpService = totpService;
        _appInfo = appInfo;
    }

    public async Task<Result<SetupTotpResponse, Error>> Handle(
        SetupTotpCommand request,
        CancellationToken cancellationToken)
    {
        var nowUtc = _clock.UtcNow;

        var user = await _dbContext.GetByIdAsync<User, UserId>(
            _currentUser.UserId,
            cancellationToken: cancellationToken);

        if (user is null)
            return UserErrors.User.NotFound(_currentUser.UserId);

        var secret = _totpService.GenerateSecret();

        var setupResult = user.SetupTotp(secret, nowUtc);

        if (setupResult.IsFailure)
            return setupResult.Error;

        var qrCodePng = _totpService.GenerateQrCodePng(
            secret,
            user.Email.Value,
            _appInfo.AppName);

        var qrCodeBase64 = Convert.ToBase64String(qrCodePng);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new SetupTotpResponse(secret, qrCodeBase64);
    }
}
