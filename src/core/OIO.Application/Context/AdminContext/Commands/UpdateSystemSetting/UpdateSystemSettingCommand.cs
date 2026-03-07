using System.Text.Json;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Abstractions.Settings;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.Shared.Entities;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AdminContext.Commands.UpdateSystemSetting;

public sealed record UpdateSystemSettingCommand(
    string Key,
    string Value) : ICommand;
    
internal sealed class UpdateSystemSettingCommandHandler
    : ICommandHandler<UpdateSystemSettingCommand>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISystemSettingsService _settingsService;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public UpdateSystemSettingCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        ISystemSettingsService settingsService,
        ICurrentUser currentUser,
        IClock clock)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _settingsService = settingsService;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<UnitResult<Error>> Handle(
        UpdateSystemSettingCommand request,
        CancellationToken cancellationToken)
    {
        // Validate JSON
        try
        {
            JsonDocument.Parse(request.Value);
        }
        catch
        {
            return Error.Validation("value", "Settings.InvalidJson",
                "Value must be valid JSON.");
        }

        var setting = await _dbContext.Set<SystemSetting>()
            .FirstOrDefaultAsync(s => s.Id == request.Key, cancellationToken);

        if (setting is null)
            return Error.NotFound("Setting.NotFound",
                $"Setting '{request.Key}' not found.");
        
        setting.Update(
            _clock.UtcNow,
            request.Value,
            _currentUser.UserId.ToString());
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate cache
        await _settingsService.InvalidateCacheAsync(request.Key, cancellationToken);

        return Result.Success<Error>();
    }
}