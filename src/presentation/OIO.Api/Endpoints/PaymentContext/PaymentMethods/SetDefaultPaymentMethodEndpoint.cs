using MediatR;
using Microsoft.AspNetCore.Mvc;
using OIO.Api.Common;
using OIO.Application.Context.PaymentContext.Commands.PaymentMethods;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Api.Endpoints.PaymentContext.PaymentMethods;

public sealed class SetDefaultPaymentMethodEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Payments.SetDefaultMethod, async (
            Guid id,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new SetDefaultPaymentMethodCommand(id), cancellationToken);

            return result.ToNoContentHttpResult();
        })
        .WithTags(ApiEndpoint.Tags.Payments)
        .WithName(ApiEndpoint.Names.Payments.SetDefaultPaymentMethod)
        .RequireAuthorization()
        .Produces(StatusCodes.Status204NoContent)
        .Produces<Error>(StatusCodes.Status400BadRequest);
    }
}
