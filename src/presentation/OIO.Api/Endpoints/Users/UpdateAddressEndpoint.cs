using MediatR;
using OIO.Api.Common;
using OIO.Api.Extensions;
using OIO.Application.UserContext.Commands.UpdateAddress;

namespace OIO.Api.Endpoints.Users;

public class UpdateAddressEndpoint : IEndpoint
{
    public sealed record Request(
        string? Type,
        string? RecipientName,
        string? Street,
        string? Ward,
        string? District,
        string? City,
        string? PhoneNumber,
        string? CountryCode,
        string? PostalCode);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("api/users/me/addresses/{addressId:guid}",
                async (Guid addressId, Request request, ISender sender, CancellationToken ct) =>
                {
                    var command = new UpdateAddressCommand(
                        addressId,
                        request.Type,
                        request.RecipientName,
                        request.Street,
                        request.Ward,
                        request.District,
                        request.City,
                        request.PhoneNumber,
                        request.CountryCode,
                        request.PostalCode);

                    var result = await sender.Send(command, ct);

                    return result.IsFailure ? result.Error.ProcessError() : Results.Ok(result.Value);
                })
            .RequireAuthorization()
            .WithTags(Tags.Users);
    }
}