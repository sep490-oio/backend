using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using OIO.Api.Common;
using OIO.Application.Context.PaymentContext.Commands.PaymentMethods;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Api.Endpoints.PaymentContext.PaymentMethods;

public sealed class AddPaymentMethodEndpoint : IEndpoint
{
    public sealed record Request(
        [Required] string Type,
        string? Provider,
        string? LastFour,
        int? ExpiryMonth,
        int? ExpiryYear,
        string? HolderName,
        string? TokenReference,
        bool IsDefault);
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Payments.AddMethod, async (
            Request request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var command = new AddPaymentMethodCommand(
                request.Type,
                request.Provider,
                request.LastFour,
                request.ExpiryMonth,
                request.ExpiryYear,
                request.HolderName,
                request.TokenReference,
                request.IsDefault);

            var result = await sender.Send(command, cancellationToken);

            return result.ToOkHttpResult();
        })
        .WithTags(ApiEndpoint.Tags.Payments)
        .WithName(ApiEndpoint.Names.Payments.AddPaymentMethod)
        .RequireAuthorization()
        .Produces<Guid>(StatusCodes.Status200OK)
        .Produces<Error>(StatusCodes.Status400BadRequest);
    }
}

