using MediatR;
using Microsoft.AspNetCore.Mvc;
using OIO.Api.Common;
using OIO.Application.Context.PaymentContext.Commands.PaymentMethods;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Api.Endpoints.PaymentContext.PaymentMethods;

public sealed class HardDeletePaymentMethodEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete(ApiEndpoint.Url.Payments.HardDeleteMethod, async (
            Guid id,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new HardDeletePaymentMethodCommand(id), cancellationToken);

            return result.ToNoContentHttpResult();
        })
        .WithTags(ApiEndpoint.Tags.Payments)
        .WithName(ApiEndpoint.Names.Payments.HardDeletePaymentMethod)
        .RequireAuthorization()
        .Produces(StatusCodes.Status204NoContent)
        .Produces<Error>(StatusCodes.Status404NotFound)
        .Produces<Error>(StatusCodes.Status409Conflict)
        .Produces<Error>(StatusCodes.Status503ServiceUnavailable);
    }
}
