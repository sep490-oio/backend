using CSharpFunctionalExtensions;
using MediatR;
using OIO.Application.Abstractions.Address;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AddressContext.Commands.SyncGhnAddress;

internal sealed class SyncGhnAddressCommandHandler
    : IRequestHandler<SyncGhnAddressCommand, Result<GhnSyncResult, Error>>
{
    private readonly IGhnAddressService _ghnAddressService;

    public SyncGhnAddressCommandHandler(IGhnAddressService ghnAddressService)
    {
        _ghnAddressService = ghnAddressService;
    }

    public async Task<Result<GhnSyncResult, Error>> Handle(
        SyncGhnAddressCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _ghnAddressService.SyncAllAsync(cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            return Error.Unexpected(
                code: "Address.Sync.Failed",
                description: $"GHN address sync failed: {ex.Message}");
        }
    }
}
