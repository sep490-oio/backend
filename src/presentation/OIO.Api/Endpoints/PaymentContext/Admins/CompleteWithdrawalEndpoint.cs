using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.PaymentContext.Commands.Withdrawals;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.PaymentContext.Admins;

public sealed record CompleteWithdrawalRequest(
    string TransferProofUrl,
    string? TransferNote);

public sealed class CompleteWithdrawalEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.AdminPayments.CompleteWithdrawal, async (
                Guid withdrawalId,
                CompleteWithdrawalRequest request,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new CompleteWithdrawalCommand(
                        withdrawalId,
                        request.TransferProofUrl,
                        request.TransferNote), ct);
                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ManagePayments)
            .WithName(ApiEndpoint.Names.AdminPayments.CompleteWithdrawal)
            .WithTags(ApiEndpoint.Tags.Payments)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
