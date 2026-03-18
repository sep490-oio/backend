using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Media;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;
using ItemId = OIO.Domain.Context.CatalogContext.ValueObjects.Ids.ItemId;
using ItemMediaId = OIO.Domain.Context.CatalogContext.ValueObjects.Ids.ItemMediaId;

namespace OIO.Application.Context.AuctionContext.Commands.RemoveItemMedia;

public sealed record RemoveItemMediaCommand(
    Guid EntityId,
    Guid MediaId) : ICommand, IHasValidate
{
    public ViolationsError Validate()
    {
        return RemoveItemMediaCommand.Check()
            .WithOwnerName("RemoveItemMedia")
            .Field(EntityId)
            .NotEmptyGuid()
            .Field(MediaId)
            .NotEmptyGuid();
    }
}

internal sealed class RemoveItemMediaCommandHandler
    : ICommandHandler<RemoveItemMediaCommand>
{
    private readonly IDbContext _dbContext;
    private readonly IMediaSignatureService _signatureService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public RemoveItemMediaCommandHandler(
        IDbContext dbContext,
        IMediaSignatureService signatureService,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IClock clock)
    {
        _dbContext = dbContext;
        _signatureService = signatureService;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<UnitResult<Error>> Handle(
        RemoveItemMediaCommand request,
        CancellationToken cancellationToken)
    {
        //TODO: bring Error to MediaErrors
        var nowUtc = _clock.UtcNow;
        
        var itemId = ItemId.From(request.EntityId);
        var item = await _dbContext.GetByIdAsync<Item, ItemId>(
            id: itemId,
            queryBuilder: query => query
                .Include(i => i.Media),
            cancellationToken: cancellationToken);

        if (item is null)
            return AuctionErrors.Item.NotFound(itemId);

        if (item.SellerId != _currentUser.UserId)
            return Error.Forbidden("Media.NotOwner", "Only item owner can delete images.");

        var itemMediaId = ItemMediaId.From(request.MediaId);
        
        var media = item.Media.FirstOrDefault(i => i.Id == itemMediaId);

        if (media is null)
            return AuctionErrors.Item.MediaNotFound(itemMediaId);

        var result = item.RemoveMedia(itemMediaId, nowUtc);
        
        if (result.IsFailure)
            return result.Error;
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success<Error>();
    }
    
}
