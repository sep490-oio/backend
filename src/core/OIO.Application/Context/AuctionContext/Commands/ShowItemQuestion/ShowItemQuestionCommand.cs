using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.Context.CatalogContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Commands.ShowItemQuestion;

public sealed record ShowItemQuestionCommand(
    Guid ItemId,
    Guid QuestionId) : ICommand;

internal sealed class ShowItemQuestionCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork)
    : ICommandHandler<ShowItemQuestionCommand>
{
    public async Task<UnitResult<Error>> Handle(
        ShowItemQuestionCommand request,
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

        question.Show();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return UnitResult.Success<Error>();
    }
}
