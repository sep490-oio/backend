using MediatR;
using Microsoft.AspNetCore.Mvc;
using OIO.Api.Common;
using OIO.Application.Context.PaymentContext.Commands.PaymentMethods;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Api.Endpoints.PaymentContext.PaymentMethods;

public sealed class DeletePaymentMethodEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete(ApiEndpoint.Url.Payments.DeleteMethod, async (
            Guid id,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new DeletePaymentMethodCommand(id), cancellationToken);

            return result.ToNoContentHttpResult();
        })
        .WithTags(ApiEndpoint.Tags.Payments)
        .WithName(ApiEndpoint.Names.Payments.DeletePaymentMethod)
        .RequireAuthorization()
        .Produces(StatusCodes.Status204NoContent)
        .Produces<Error>(StatusCodes.Status400BadRequest);
    }
}
