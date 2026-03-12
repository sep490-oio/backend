using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.DTOs;
using OIO.Application.Context.UserContext.Mappings;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Enums;
using OIO.Domain.Context.UserContext.Errors;
using OIO.Domain.Context.UserContext.ValueObjects;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Commands.UpdateVerification;

/// <summary>
/// Admin endpoint to correct OCR data or manually fill personal info.
/// </summary>
public sealed record UpdateVerificationCommand(
    Guid VerificationId,
    string FullName,
    DateOnly DateOfBirth,
    string Gender,
    string IdType,
    string IdNumber,
    DateOnly? IdIssuedDate,
    DateOnly? IdExpiredDate,
    string? IdIssuedPlace,
    string FullAddress,
    string Province,
    string District,
    string Ward,
    string? Nationality = null) : ICommand<VerificationDto>, IHasValidate
{
    public ViolationsError Validate()
    {
        return UpdateVerificationCommand.Check()
            .WithOwnerName("UpdateVerification")
            .Field(VerificationId).NotEmptyGuid()
            .Field(FullName).NotWhiteSpace().MaxLength(200)
            .Field(Gender).NotWhiteSpace()
                .InSet(Domain.Context.UserContext.Enums.Gender.All.Select(x => x.Id))
            .Field(IdType).NotWhiteSpace()
                .InSet(Domain.Context.UserContext.Enums.IdType.All.Select(x => x.Id))
            .Field(IdNumber).NotWhiteSpace().MaxLength(50)
            .Field(FullAddress).NotWhiteSpace().MaxLength(500)
            .Field(Province).NotWhiteSpace().MaxLength(100)
            .Field(District).NotWhiteSpace().MaxLength(100)
            .Field(Ward).NotWhiteSpace().MaxLength(100)
            .Field(Nationality).WhenHasValue(x => x.NotWhiteSpace().MaxLength(100));
    }
}

internal sealed class UpdateVerificationCommandHandler
    : ICommandHandler<UpdateVerificationCommand, VerificationDto>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ICurrentUser _currentUser;

    public UpdateVerificationCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        IClock clock,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _currentUser = currentUser;
    }

    public async Task<Result<VerificationDto, Error>> Handle(
        UpdateVerificationCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var verificationId = IdentityVerificationId.From(request.VerificationId);

        var verification = await _dbContext.GetByIdAsync<IdentityVerification, IdentityVerificationId>(
            verificationId,
            q => q.Include(v => v.Documents),
            cancellationToken);

        if (verification is null)
            return UserErrors.Verification.NotFound(verificationId);

        var gender = Gender.FromId(request.Gender).Value;
        var idType = IdType.FromId(request.IdType).Value;

        var documentResult = IdentityDocument.Create(
            idType, request.IdNumber,
            request.IdIssuedDate, request.IdExpiredDate, request.IdIssuedPlace);

        if (documentResult.IsFailure)
            return documentResult.Error;

        var permanentAddress = PermanentAddress.Create(
            request.FullAddress, request.Province, request.District, request.Ward);

        var result = verification.Update(
            request.FullName, request.DateOfBirth, gender,
            documentResult.Value, permanentAddress,
            _clock.UtcNow, userId, PerformerType.Admin, request.Nationality);

        if (result.IsFailure)
            return result.Error;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return verification.ToDto();
    }
}
