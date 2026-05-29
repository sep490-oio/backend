using CSharpFunctionalExtensions;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Messaging;
using OIO.Domain.SeedWork.Checks;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.OrderContext.Commands.ProvisionWinnerOrder;

public sealed record ProvisionWinnerOrderCommand(Guid AuctionId, Guid? CurrentUserId = null, bool IsAdmin = false)
    : ICommand<Guid>;
