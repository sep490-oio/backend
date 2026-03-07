using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.AuctionContext.Aggregates.Items;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Commands.AnswerQuestion;

public sealed record AnswerQuestionCommand(
    Guid ItemId,
    Guid QuestionId,
    string Answer) : ICommand, IHasValidate
{
    public ViolationsError Validate()
    {
        return AnswerQuestionCommand.Check()
            .WithOwnerName("AnswerQuestion")
            .Field(ItemId)
            .NotEmptyGuid()
            .Field(QuestionId)
            .NotEmptyGuid()
            .Field(Answer)
            .NotWhiteSpace()
            .MaxLength(2000);
    }
}

internal sealed class AnswerQuestionCommandHandler
    : ICommandHandler<AnswerQuestionCommand>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public AnswerQuestionCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IClock clock)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<UnitResult<Error>> Handle(
        AnswerQuestionCommand request,
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


        // Only item owner (seller) can answer questions
        if (item.SellerId != _currentUser.UserId.Value)
            return AuctionErrors.Item.NotOwnedByUser(itemId, _currentUser.UserId);

        var questionId = ItemQuestionId.From(request.QuestionId);
        
        var result = item.AnswerQuestion(
                questionId, 
                request.Answer,
                nowUtc);
        
        if (result.IsFailure)
        {
            return result.Error;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return UnitResult.Success<Error>();
    }
}