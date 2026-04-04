using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.EventHandlers;
using OIO.Application.Context.AuctionContext.Hubs;
using OIO.Application.Context.AuctionContext.Services;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;
using ItemId = OIO.Domain.Context.CatalogContext.ValueObjects.Ids.ItemId;
using ItemQuestionId = OIO.Domain.Context.CatalogContext.ValueObjects.Ids.ItemQuestionId;

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
    private readonly IAuctionNotificationService _notificationService;
    private readonly ILogger<AnswerQuestionCommandHandler> _logger;

    public AnswerQuestionCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IClock clock,
        IAuctionNotificationService notificationService,
        ILogger<AnswerQuestionCommandHandler> logger)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
        _notificationService = notificationService;
        _logger = logger;
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

        // Dual-publish: push realtime notification immediately (outbox will also fire later, FE deduplicates by ID)
        try
        {
            var question = item.Questions.First(q => q.Id == questionId);
            var answerer = await _dbContext.Set<User>()
                .AsNoTracking()
                .Include(u => u.Profile)
                .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId, cancellationToken);

            await _notificationService.NotifyQuestionAnsweredAsync(
                item.Id.Value,
                new ItemQuestionNotification(
                    ItemId: item.Id.Value,
                    QuestionId: question.Id.Value,
                    AskerId: question.AskerId.Value,
                    AskerDisplayName: AuctionNotificationDisplayNames.Resolve(answerer),
                    Question: question.Question,
                    Answer: question.Answer,
                    IsPublic: question.IsPublic,
                    CreatedAt: question.CreatedAt),
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to dual-publish QuestionAnswered for item {ItemId}. Outbox will deliver later.", item.Id.Value);
        }

        return UnitResult.Success<Error>();
    }
}
