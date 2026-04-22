using MediatR;
using Microsoft.AspNetCore.Mvc;
using OIO.Api.Common;
using OIO.Application.Context.PaymentContext.Commands.PaymentMethods;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Api.Endpoints.PaymentContext.PaymentMethods;

public sealed class ReactivatePaymentMethodEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Payments.ReactivateMethod, async (
            Guid id,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ReactivatePaymentMethodCommand(id), cancellationToken);

            return result.ToNoContentHttpResult();
        })
        .WithTags(ApiEndpoint.Tags.Payments)
        .WithName(ApiEndpoint.Names.Payments.ReactivatePaymentMethod)
        .RequireAuthorization()
        .Produces(StatusCodes.Status204NoContent)
        .Produces<Error>(StatusCodes.Status404NotFound)
        .Produces<Error>(StatusCodes.Status409Conflict);
    }
}
