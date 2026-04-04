using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.AuctionContext.EventHandlers;
using OIO.Application.Context.AuctionContext.Mappings;
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
    private readonly IRuntimeSettings _runtimeSettings;
    private readonly IClock _clock;
    private readonly IAuctionNotificationService _notificationService;
    private readonly ILogger<AskQuestionCommandHandler> _logger;

    public AskQuestionCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IRuntimeSettings runtimeSettings,
        IClock clock,
        IAuctionNotificationService notificationService,
        ILogger<AskQuestionCommandHandler> logger)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _runtimeSettings = runtimeSettings;
        _clock = clock;
        _notificationService = notificationService;
        _logger = logger;
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
                _runtimeSettings.Item.MaxQuestionsPerItem,
                nowUtc);

        if(isFailure)
            return error;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Dual-publish: push realtime notification immediately (outbox will also fire later, FE deduplicates by ID)
        try
        {
            var asker = await _dbContext.Set<User>()
                .AsNoTracking()
                .Include(u => u.Profile)
                .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId, cancellationToken);

            await _notificationService.NotifyQuestionAskedAsync(
                item.Id.Value,
                new ItemQuestionNotification(
                    ItemId: item.Id.Value,
                    QuestionId: question.Id.Value,
                    AskerId: question.AskerId.Value,
                    AskerDisplayName: AuctionNotificationDisplayNames.Resolve(asker),
                    Question: question.Question,
                    Answer: question.Answer,
                    IsPublic: question.IsPublic,
                    CreatedAt: question.CreatedAt),
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to dual-publish QuestionAsked for item {ItemId}. Outbox will deliver later.", item.Id.Value);
        }

        return question.ToDto();
    }
}
