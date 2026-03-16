using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.PaymentContext.Commands.Withdrawals;
using OIO.Domain.AppDefinitions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Api.Endpoints.PaymentContext.Admins;

public sealed class ApproveWithdrawalEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.AdminPayments.ApproveWithdrawal, async (
                Guid withdrawalId,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new ApproveWithdrawalCommand(withdrawalId),
                    ct);

                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ManagePayments)
            .WithName(ApiEndpoint.Names.AdminPayments.ApproveWithdrawal)
            .WithTags(ApiEndpoint.Tags.Payments)
            .Produces(StatusCodes.Status204NoContent)
            .Produces<Error>(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
