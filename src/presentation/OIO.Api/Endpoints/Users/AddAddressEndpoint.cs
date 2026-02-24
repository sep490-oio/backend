using MediatR;
using OIO.Api.Common;
using OIO.Api.Extensions;
using OIO.Application.UserContext.Commands.AddAddress;
using OIO.Domain.Context.UserContext.ValueObjects;

namespace OIO.Api.Endpoints.Users;

public class AddAddressEndpoint : IEndpoint
{
    public sealed record Request(
        string Type,
        string RecipientName,
        string Street,
        string Ward,
        string District,
        string City,
        string? PostalCode,
        string PhoneNumber,
        string CountryCode = PhoneNumber.DefaultRegion,
        bool IsDefault = false);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/users/me/addresses", async (Request request, ISender sender, CancellationToken ct) =>
            {
                var command = new AddAddressCommand(
                    request.Type,
                    request.RecipientName,
                    request.Street,
                    request.Ward,
                    request.District,
                    request.City,
                    request.PostalCode,
                    request.PhoneNumber,
                    request.CountryCode,
                    request.IsDefault);

                var result = await sender.Send(command, ct);

                return result.IsFailure
                    ? result.Error.ProcessError()
                    : Results.Created("api/users/me/addresses", result.Value);
            })
            .RequireAuthorization()
            .WithTags(Tags.Users);
    }
}