using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.Context.CatalogContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Commands.HideItemQuestion;

public sealed record HideItemQuestionCommand(
    Guid ItemId,
    Guid QuestionId,
    string Reason) : ICommand;

internal sealed class HideItemQuestionCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock)
    : ICommandHandler<HideItemQuestionCommand>
{
    public async Task<UnitResult<Error>> Handle(
        HideItemQuestionCommand request,
        CancellationToken cancellationToken)
    {
        var itemId = ItemId.From(request.ItemId);

        var item = await dbContext.Set<Item>()
            .Include(i => i.Questions)
            .FirstOrDefaultAsync(i => i.Id == itemId, cancellationToken);

        if (item is null)
            return AuctionErrors.Item.NotFound(itemId);

        var questionId = ItemQuestionId.From(request.QuestionId);
        var question = item.Questions.FirstOrDefault(q => q.Id == questionId);

        if (question is null)
            return AuctionErrors.Item.QuestionNotFound(questionId);

        question.Hide(currentUser.UserId, request.Reason, clock.UtcNow);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return UnitResult.Success<Error>();
    }
}
