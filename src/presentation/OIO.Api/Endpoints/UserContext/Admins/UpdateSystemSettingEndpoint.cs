using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AdminContext.Commands.UpdateSystemSetting;

namespace OIO.Api.Endpoints.UserContext.Admins;

public sealed class UpdateSystemSettingEndpoint : IEndpoint
{
    public sealed record Request(string Key, string Value);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiEndpoint.Url.Admins.UpdateSetting, async (
                string key,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new UpdateSystemSettingCommand(key, request.Value);
                var result = await sender.Send(command, ct);
                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Admins.UpdateSetting)
            .WithTags(ApiEndpoint.Tags.Admins);
    }
}