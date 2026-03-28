using CSharpFunctionalExtensions;
using MediatR;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.WarehouseContext.Queries.CalculateExpectedDeliveryTime;

public sealed record CalculateExpectedDeliveryTimeQuery : IRequest<Result<DateTime?, Error>>
{
    public required string ProviderCode { get; init; } // "ghn"

    // --- Sender: supply either UserId (BE resolves) or explicit fields ---
    public Guid?   SenderUserId                 { get; init; }
    public string? SenderDistrict               { get; init; }
    public string? SenderProvince               { get; init; }
    public string? SenderCarrierAddressDataJson { get; init; }

    // --- Recipient: warehouse flag, UserId, or explicit fields ---
    public bool    RecipientIsWarehouse              { get; init; }
    public Guid?   RecipientUserId                  { get; init; }
    public string? RecipientDistrict                { get; init; }
    public string? RecipientProvince                { get; init; }
    public string? RecipientCarrierAddressDataJson  { get; init; }
}
