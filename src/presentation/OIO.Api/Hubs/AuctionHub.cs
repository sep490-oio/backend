using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using OIO.Api.Common;
using OIO.Api.Extensions;
using OIO.Application.Context.AuctionContext.Commands.BuyNow;
using OIO.Application.Context.AuctionContext.Commands.ConfigureAutoBid;
using OIO.Application.Context.AuctionContext.Commands.PlaceBid;
using OIO.Application.Context.AuctionContext.Commands.WatchAuction;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.AuctionContext.Hubs;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.AppDefinitions;
using OIO.Domain.SeedWork.Checks;
using OIO.Domain.SeedWork.Errors;
using OIO.Infrastructure.Authorizations;
using SignalRSwaggerGen.Attributes;

namespace OIO.Api.Hubs;

[SignalRHub("/hubs/auction", tag: ApiEndpoint.Tags.Hub)]
public sealed class AuctionHub : Hub<IAuctionHubClient>
{
    private readonly ISender _sender;
    private readonly ICurrentUser _currentUser;

    public AuctionHub(
        ISender sender,
        ICurrentUser currentUser)
    {
        _sender = sender;
        _currentUser = currentUser;
    }

    // ==================== Connection Management ====================

    [AllowAnonymous]
    public async Task JoinAuction(Guid auctionId)
    {
        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            AuctionGroupName(auctionId));
    }

    [AllowAnonymous]
    public async Task LeaveAuction(Guid auctionId)
    {
        await Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            AuctionGroupName(auctionId));
    }

    // ==================== Item Q&A ====================

    [AllowAnonymous]
    public async Task JoinItem(Guid itemId)
    {
        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            ItemGroupName(itemId));
    }

    [AllowAnonymous]
    public async Task LeaveItem(Guid itemId)
    {
        await Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            ItemGroupName(itemId));
    }

    // ==================== Server Time ====================

    [AllowAnonymous]
    public DateTimeOffset GetServerTime()
    {
        return DateTimeOffset.UtcNow;
    }

    // ==================== Bidding ====================

    [HasPermission(App.Permissions.Catalogs.Auctions.Bid)]
    public async Task<HubCommandResult<PlaceBidResultDto>> PlaceBid(
        Guid auctionId,
        decimal amount,
        string currency,
        string? idempotencyKey = null)
    {
        var httpContext = Context.GetHttpContext();
        var command = new PlaceBidCommand(auctionId, amount, currency, httpContext?.GetIpAddress());
        var result = await _sender.Send(command, Context.ConnectionAborted);

        return HubCommandResult<PlaceBidResultDto>.FromResult(result);
    }

    [HasPermission(App.Permissions.Catalogs.Auctions.BuyNow)]
    public async Task<HubCommandResult<BuyNowCheckoutDto>> BuyNow(Guid auctionId)
    {
        var httpContext = Context.GetHttpContext();

        var command = new BuyNowCommand(auctionId, httpContext?.GetIpAddress());
        var result = await _sender.Send(command);

        return HubCommandResult<BuyNowCheckoutDto>.FromResult(result);
    }

    [HasPermission(App.Permissions.Catalogs.Auctions.AutoBid)]
    public async Task ConfigureAutoBid(
        Guid auctionId,
        decimal maxAmount,
        string currency,
        decimal? incrementAmount)
    {
        var command = new ConfigureAutoBidCommand(auctionId, maxAmount, currency, incrementAmount);
        var result = await _sender.Send(command);

        if (result.IsFailure)
        {
            var error = ToErrorNotification(result.Error);

            await Clients.Caller.Error(error);
        }
    }

    [HasPermission(App.Permissions.Catalogs.Auctions.Watch)]
    public async Task WatchAuction(
        Guid auctionId,
        bool notifyOnBid = true,
        bool notifyOnEnd = true)
    {
        var command = new WatchAuctionCommand(auctionId, notifyOnBid, notifyOnEnd);
        var result = await _sender.Send(command);

        if (result.IsFailure)
        {
            var error = ToErrorNotification(result.Error);

            await Clients.Caller.Error(error);
        }
    }

    // ==================== Lifecycle ====================

    public override async Task OnConnectedAsync()
    {
        if (_currentUser.IsAuthenticated)
        {
            var userId = _currentUser.UserId;
            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                UserGroupName(userId.Value));
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (_currentUser.IsAuthenticated)
        {
            var userId = _currentUser.UserId;
            await Groups.RemoveFromGroupAsync(
                Context.ConnectionId,
                UserGroupName(userId.Value));
        }

        await base.OnDisconnectedAsync(exception);
    }

    public static string AuctionGroupName(Guid auctionId) => $"auction:{auctionId}";
    public static string ItemGroupName(Guid itemId) => $"item:{itemId}";
    public static string UserGroupName(Guid userId) => $"user:{userId}";

    private static ErrorNotification ToErrorNotification(Error error)
    {
        if (error is not ViolationsError violationsError)
            return new ErrorNotification(error.Code, error.Message, null);

        var errorsDict = violationsError.Violations
            .GroupBy(e => ((ICheckError)e).PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.Message).ToArray());

        return new ErrorNotification(violationsError.Code, violationsError.Message, errorsDict);
    }
}
