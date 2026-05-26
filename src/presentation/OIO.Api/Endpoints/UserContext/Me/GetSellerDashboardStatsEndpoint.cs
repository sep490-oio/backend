using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using OIO.Api.Common;
using OIO.Application.Context.UserContext.Queries.GetSellerDashboardStats;
using CSharpFunctionalExtensions.HttpResults;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.UserContext.Me;

internal sealed class GetSellerDashboardStatsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Me.GetDashboardStats,
            async (ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetSellerDashboardStatsQuery(), ct)).ToOkHttpResult())
            .RequireAuthorization(App.Permissions.Catalogs.Me.ReadSellerProfile)
            .WithName(ApiEndpoint.Names.Me.GetDashboardStats)
            .WithTags(ApiEndpoint.Tags.SellerProfiles)
            .WithSummary("Get combined dashboard stats for a seller.")
            .Produces<SellerDashboardStatsDto>(StatusCodes.Status200OK);
    }
}
