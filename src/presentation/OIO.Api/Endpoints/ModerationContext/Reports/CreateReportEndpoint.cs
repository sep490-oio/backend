using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.ModerationContext.Commands.CreateReport;
using OIO.Application.Context.ModerationContext.DTOs;

namespace OIO.Api.Endpoints.ModerationContext.Reports;

public sealed class CreateReportEndpoint : IEndpoint
{
    public sealed record Request(
        string EntityType,
        Guid EntityId,
        string ReasonCode,
        string? Description,
        string? Attachments);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Reports.Create, async (
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new CreateReportCommand(
                        request.EntityType,
                        request.EntityId,
                        request.ReasonCode,
                        request.Description,
                        request.Attachments),
                    ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Reports.CreateReport)
            .WithTags(ApiEndpoint.Tags.Reports)
            .Produces<ReportDto>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
