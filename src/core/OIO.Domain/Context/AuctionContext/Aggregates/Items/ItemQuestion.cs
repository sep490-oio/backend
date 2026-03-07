using CSharpFunctionalExtensions;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Domain.Context.AuctionContext.Aggregates.Items;

public sealed class ItemQuestion : BaseEntity<ItemQuestionId>, ICreatedAtEntity
{
    public ItemId ItemId { get; private set; }
    public UserId AskerId { get; private set; }
    public string Question { get; private set; }
    public string? Answer { get; private set; }
    public DateTime? AnsweredAt { get; private set; }
    public bool IsPublic { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private ItemQuestion() { }

    public static ItemQuestion Create(
        ItemId itemId,
        UserId askerId,
        string question,
        DateTime nowUtc,
        bool isPublic = true)
    {
        return new ItemQuestion
        {
            Id = ItemQuestionId.From(Guid.CreateVersion7()),
            ItemId = itemId,
            AskerId = askerId,
            Question = question,
            IsPublic = isPublic,
            CreatedAt = nowUtc
        };
    }

    public void AnswerQuestion(string answer, DateTime now)
    {
        Answer = answer;
        AnsweredAt = now;
    }
    
    public bool IsAnswered => Answer is not null;

    public void Hide() => IsPublic = false;
    public void Show() => IsPublic = true;
}