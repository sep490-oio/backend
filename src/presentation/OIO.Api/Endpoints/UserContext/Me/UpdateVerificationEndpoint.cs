using System.ComponentModel.DataAnnotations;
using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.UserContext.Commands.UpdateVerification;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.UserContext.Me;

public class UpdateVerificationEndpoint : IEndpoint
{
    public sealed record Request(
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

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiEndpoint.Url.Me.UpdateVerification, async (
                Guid verificationId,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new UpdateVerificationCommand(
                    VerificationId: verificationId,
                    FullName: request.FullName,
                    DateOfBirth: request.DateOfBirth,
                    Gender: request.Gender,
                    IdType: request.IdType,
                    IdNumber: request.IdNumber,
                    IdIssuedDate: request.IdIssuedDate,
                    IdExpiredDate: request.IdExpiredDate,
                    IdIssuedPlace: request.IdIssuedPlace,
                    FullAddress: request.FullAddress,
                    Province: request.Province,
                    District: request.District,
                    Ward: request.Ward,
                    Nationality: request.Nationality);

                var result = await sender.Send(command, ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Me.ManageVerification)
            .WithName(ApiEndpoint.Names.Me.UpdateVerification)
            .WithTags(ApiEndpoint.Tags.Verifications);
    }
}
