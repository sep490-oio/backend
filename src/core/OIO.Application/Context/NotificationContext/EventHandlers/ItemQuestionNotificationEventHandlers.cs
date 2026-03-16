using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Data;
using OIO.Application.Context.NotificationContext.Commands.CreateNotification;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.Context.CatalogContext.Aggregates.Items.Events;
using OIO.Domain.Context.NotificationContext.Enums;
using OIO.Domain.Context.CatalogContext.ValueObjects.Ids;

namespace OIO.Application.Context.NotificationContext.EventHandlers;

internal sealed class ItemQuestionAskedNotificationHandler
    : INotificationHandler<ItemQuestionAskedEvent>
{
    private readonly IDbContext _dbContext;
    private readonly ISender _sender;
    private readonly ILogger<ItemQuestionAskedNotificationHandler> _logger;

    public ItemQuestionAskedNotificationHandler(
        IDbContext dbContext,
        ISender sender,
        ILogger<ItemQuestionAskedNotificationHandler> logger)
    {
        _dbContext = dbContext;
        _sender = sender;
        _logger = logger;
    }

    public async Task Handle(ItemQuestionAskedEvent notification, CancellationToken cancellationToken)
    {
        var itemId = ItemId.From(Guid.Parse(notification.ItemId));
        var questionId = ItemQuestionId.From(Guid.Parse(notification.QuestionId));

        var item = await _dbContext.Set<Item>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == itemId, cancellationToken);

        if (item is null)
        {
            _logger.LogWarning(
                "Skipped item question asked notification because item {ItemId} was not found.",
                notification.ItemId);
            return;
        }

        var question = await _dbContext.Set<ItemQuestion>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == questionId, cancellationToken);

        if (question is null)
        {
            _logger.LogWarning(
                "Skipped item question asked notification because question {QuestionId} was not found.",
                notification.QuestionId);
            return;
        }

        await NotificationDispatch.DispatchAsync(
            _sender,
            _logger,
            new CreateNotificationCommand(
                UserId: item.SellerId.Value,
                NotificationType: "item_question",
                EventType: "item_question_asked",
                Title: $"Co cau hoi moi cho san pham {item.Title.Value}",
                Message: "Mot nguoi dung vua gui cau hoi cho san pham cua ban.",
                Priority: NotificationPriority.Normal,
                EntityType: "Item",
                EntityId: item.Id.Value,
                Metadata: NotificationDispatch.SerializeMetadata(new
                {
                    itemId = item.Id.Value,
                    questionId = question.Id.Value,
                    askerId = question.AskerId.Value,
                    questionPreview = TrimPreview(question.Question)
                })),
            cancellationToken);
    }

    private static string TrimPreview(string value)
    {
        const int maxLength = 120;
        return value.Length <= maxLength ? value : $"{value[..maxLength]}...";
    }
}

internal sealed class ItemQuestionAnsweredNotificationHandler
    : INotificationHandler<ItemQuestionAnsweredEvent>
{
    private readonly IDbContext _dbContext;
    private readonly ISender _sender;
    private readonly ILogger<ItemQuestionAnsweredNotificationHandler> _logger;

    public ItemQuestionAnsweredNotificationHandler(
        IDbContext dbContext,
        ISender sender,
        ILogger<ItemQuestionAnsweredNotificationHandler> logger)
    {
        _dbContext = dbContext;
        _sender = sender;
        _logger = logger;
    }

    public async Task Handle(ItemQuestionAnsweredEvent notification, CancellationToken cancellationToken)
    {
        var itemId = ItemId.From(Guid.Parse(notification.ItemId));
        var questionId = ItemQuestionId.From(Guid.Parse(notification.QuestionId));

        var item = await _dbContext.Set<Item>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == itemId, cancellationToken);

        if (item is null)
        {
            _logger.LogWarning(
                "Skipped item question answered notification because item {ItemId} was not found.",
                notification.ItemId);
            return;
        }

        var question = await _dbContext.Set<ItemQuestion>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == questionId, cancellationToken);

        if (question is null)
        {
            _logger.LogWarning(
                "Skipped item question answered notification because question {QuestionId} was not found.",
                notification.QuestionId);
            return;
        }

        await NotificationDispatch.DispatchAsync(
            _sender,
            _logger,
            new CreateNotificationCommand(
                UserId: question.AskerId.Value,
                NotificationType: "item_question",
                EventType: "item_question_answered",
                Title: $"Cau hoi cua ban da duoc tra loi cho san pham {item.Title.Value}",
                Message: "Nguoi ban da tra loi cau hoi cua ban.",
                Priority: NotificationPriority.Normal,
                EntityType: "Item",
                EntityId: item.Id.Value,
                Metadata: NotificationDispatch.SerializeMetadata(new
                {
                    itemId = item.Id.Value,
                    questionId = question.Id.Value,
                    askerId = question.AskerId.Value
                })),
            cancellationToken);
    }
}
