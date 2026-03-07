using System.ComponentModel.DataAnnotations;
using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.UserContext.Commands.SetPhoneNumber;
using OIO.Domain.AppDefinitions;
using OIO.Domain.Context.UserContext.ValueObjects;

namespace OIO.Api.Endpoints.UserContext.Me;

public class SetPhoneNumberEndpoint : IEndpoint
{
    public sealed record Request(
        [Required] string PhoneNumber,
        string? CountryCode = PhoneNumber.DefaultRegion);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiEndpoint.Url.Me.SetPhoneNumber, async (
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new SetPhoneNumberCommand(
                    request.PhoneNumber,
                    request.CountryCode);

                var result = await sender.Send(command, ct);

                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Me.ManagePhone)
            .WithName(ApiEndpoint.Names.Me.SetPhoneNumber)
            .WithTags(ApiEndpoint.Tags.Me);
    }
}