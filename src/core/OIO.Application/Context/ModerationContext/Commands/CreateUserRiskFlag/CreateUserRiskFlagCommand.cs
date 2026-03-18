using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.ModerationContext.DTOs;
using OIO.Application.Context.ModerationContext.Mappings;
using OIO.Application.Context.ModerationContext.Services;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.ModerationContext.Commands.CreateUserRiskFlag;

public sealed record CreateUserRiskFlagCommand(
    Guid UserId,
    string FlagType,
    string? Reason,
    string Severity = "medium") : ICommand<UserRiskFlagDto>, IHasValidate
{
    public ViolationsError Validate() =>
        CreateUserRiskFlagCommand.Check()
            .WithOwnerName("CreateUserRiskFlag")
            .Field(UserId).NotEmptyGuid()
            .Field(FlagType).NotWhiteSpace()
            .Field(Severity).NotWhiteSpace();
}

internal sealed class CreateUserRiskFlagCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    IClock clock,
    ICurrentUser currentUser,
    ModerationAuditService auditService)
    : ICommandHandler<CreateUserRiskFlagCommand, UserRiskFlagDto>
{
    public async Task<Result<UserRiskFlagDto, Error>> Handle(
        CreateUserRiskFlagCommand request,
        CancellationToken cancellationToken)
    {
        var user = await dbContext.Set<User>()
            .FirstOrDefaultAsync(x => x.Id == UserId.From(request.UserId), cancellationToken);

        if (user is null)
            return Error.NotFound("User.NotFound", "User was not found.");

        var severityResult = ModerationValueParsers.ParseRiskSeverity(request.Severity);
        if (severityResult.IsFailure)
            return severityResult.Error;

        var flag = UserRiskFlag.Create(
            userId: user.Id,
            flagType: request.FlagType.Trim(),
            reason: request.Reason,
            severity: severityResult.Value,
            createdBy: currentUser.UserId,
            nowUtc: clock.UtcNow);

        dbContext.Insert(flag);
        auditService.Log(
            action: "user_risk_flag_created",
            entityType: "User",
            entityId: user.Id.Value,
            newData: new
            {
                userId = user.Id.Value,
                flagType = flag.FlagType,
                severity = flag.Severity.Id,
                reason = flag.Reason
            });

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return flag.ToDto();
    }
}
