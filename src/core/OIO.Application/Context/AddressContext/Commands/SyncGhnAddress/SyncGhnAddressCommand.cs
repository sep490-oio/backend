using CSharpFunctionalExtensions;
using MediatR;
using OIO.Application.Abstractions.Address;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AddressContext.Commands.SyncGhnAddress;

public sealed record SyncGhnAddressCommand : IRequest<Result<GhnSyncResult, Error>>;
