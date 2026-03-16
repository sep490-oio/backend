using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.ModerationContext.Commands.AssignReport;
using OIO.Application.Context.ModerationContext.DTOs;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.ModerationContext.Admins;

public sealed class AssignReportEndpoint : IEndpoint
{
    public sealed record Request(Guid AssignedToUserId);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Admins.AssignReport, async (
                Guid reportId,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new AssignReportCommand(reportId, request.AssignedToUserId),
                    ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ManageItems)
            .WithName(ApiEndpoint.Names.Admins.AssignReport)
            .WithTags(ApiEndpoint.Tags.Admins)
            .Produces<ReportDto>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
