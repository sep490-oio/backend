using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.PaymentContext.Commands.Withdrawals;
using OIO.Domain.AppDefinitions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Api.Endpoints.PaymentContext.Admins;

public sealed class BulkApproveWithdrawalsEndpoint : IEndpoint
{
    public sealed record Request(List<Guid> WithdrawalIds);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.AdminPayments.BulkApproveWithdrawals, async (
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new BulkApproveWithdrawalsCommand(request.WithdrawalIds),
                    ct);

                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ManagePayments)
            .WithName(ApiEndpoint.Names.AdminPayments.BulkApproveWithdrawals)
            .WithTags(ApiEndpoint.Tags.Payments)
            .Produces(StatusCodes.Status204NoContent)
            .Produces<Error>(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
