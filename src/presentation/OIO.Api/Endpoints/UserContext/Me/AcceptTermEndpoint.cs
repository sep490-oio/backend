using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.UserContext.Commands.AcceptTerms;
using OIO.Application.Context.UserContext.DTOs;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.UserContext.Me;

public sealed class AcceptTermEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Me.AcceptTerm, async (
                Guid termDocumentId,
                HttpContext httpContext,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new AcceptTermsCommand(
                    termDocumentId,
                    httpContext.Connection.RemoteIpAddress?.ToString(),
                    httpContext.Request.Headers["User-Agent"].ToString());

                var result = await sender.Send(command, ct);
                return result.ToCreatedHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Me.AcceptTerms)
            .WithName(ApiEndpoint.Names.Me.AcceptTerm)
            .WithTags(ApiEndpoint.Tags.Me)
            .Produces<TermsAcceptanceDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
