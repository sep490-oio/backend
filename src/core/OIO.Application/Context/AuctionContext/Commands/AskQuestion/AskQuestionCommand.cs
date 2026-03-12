using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.AuctionContext.Mappings;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;
using ItemId = OIO.Domain.Context.CatalogContext.ValueObjects.Ids.ItemId;

namespace OIO.Application.Context.AuctionContext.Commands.AskQuestion;

public sealed record AskQuestionCommand(
    Guid ItemId,
    string Question) : ICommand<ItemQuestionDto>, IHasValidate
{
    public ViolationsError Validate()
    {
        return AskQuestionCommand.Check()
            .WithOwnerName("AskQuestion")
            .Field(ItemId)
            .NotEmptyGuid()
            .Field(Question)
            .NotWhiteSpace()
            .MaxLength(1000);
    }
}

internal sealed class AskQuestionCommandHandler
    : ICommandHandler<AskQuestionCommand, ItemQuestionDto>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IAppConfigs _appConfigs;
    private readonly IClock _clock;

    public AskQuestionCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IAppConfigs appConfigs,
        IClock clock)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _appConfigs = appConfigs;
        _clock = clock;
    }

    public async Task<Result<ItemQuestionDto, Error>> Handle(
        AskQuestionCommand request,
        CancellationToken cancellationToken)
    {
        var itemId = ItemId.From(request.ItemId);
        var item = await _dbContext.GetByIdAsync<Item, ItemId>(
            id: itemId,
            queryBuilder: query => query
                .Include(i => i.Questions
                    .Where(q => q.IsPublic)
                    .OrderByDescending(q => q.CreatedAt))
                .AsSplitQuery(),
            cancellationToken: cancellationToken);

        if(item is null)
            return AuctionErrors.Item.NotFound(itemId);

        var nowUtc = _clock.UtcNow;
        
        var (_, isFailure, question, error) = item.AskQuestion(
                _currentUser.UserId, 
                request.Question,
                await _appConfigs.Items.GetMaxQuestionsPerItemAsync(cancellationToken),
                nowUtc);

        if(isFailure)
            return error;
        
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return question.ToDto();
        
    }
}
