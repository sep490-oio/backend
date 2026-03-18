using System.ComponentModel.DataAnnotations;
using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.ModerationContext.DTOs;
using OIO.Application.Context.UserContext.Commands.CreateVerificationCorrectionDispute;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.UserContext.Me;

public sealed class CreateVerificationDisputeEndpoint : IEndpoint
{
    public sealed record CorrectedInfoRequest(
        [Required] string FullName,
        [Required] DateOnly DateOfBirth,
        [Required] string Gender,
        [Required] string IdType,
        [Required] string IdNumber,
        DateOnly? IdIssuedDate,
        DateOnly? IdExpiredDate,
        string? IdIssuedPlace,
        [Required] string FullAddress,
        [Required] string Province,
        [Required] string District,
        [Required] string Ward,
        string? Nationality = null);

    public sealed record Request(
        [Required] string Reason,
        [Required] CorrectedInfoRequest CorrectedInfo,
        string? Message = null,
        IReadOnlyList<Guid>? MediaUploadIds = null);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Me.CreateVerificationDispute, async (
                Guid verificationId,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new CreateVerificationCorrectionDisputeCommand(
                    VerificationId: verificationId,
                    Reason: request.Reason,
                    FullName: request.CorrectedInfo.FullName,
                    DateOfBirth: request.CorrectedInfo.DateOfBirth,
                    Gender: request.CorrectedInfo.Gender,
                    IdType: request.CorrectedInfo.IdType,
                    IdNumber: request.CorrectedInfo.IdNumber,
                    IdIssuedDate: request.CorrectedInfo.IdIssuedDate,
                    IdExpiredDate: request.CorrectedInfo.IdExpiredDate,
                    IdIssuedPlace: request.CorrectedInfo.IdIssuedPlace,
                    FullAddress: request.CorrectedInfo.FullAddress,
                    Province: request.CorrectedInfo.Province,
                    District: request.CorrectedInfo.District,
                    Ward: request.CorrectedInfo.Ward,
                    Nationality: request.CorrectedInfo.Nationality,
                    Message: request.Message,
                    MediaUploadIds: request.MediaUploadIds);

                var result = await sender.Send(command, ct);
                return result.ToCreatedHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Me.ManageVerification)
            .WithName(ApiEndpoint.Names.Me.CreateVerificationDispute)
            .WithTags(ApiEndpoint.Tags.Verifications)
            .Produces<DisputeThreadMetaDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
