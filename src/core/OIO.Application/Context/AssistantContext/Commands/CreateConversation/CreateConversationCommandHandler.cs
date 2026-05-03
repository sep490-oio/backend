using CSharpFunctionalExtensions;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Domain.Context.AssistantContext.Aggregates;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AssistantContext.Commands.CreateConversation;

internal sealed class CreateConversationCommandHandler
    : ICommandHandler<CreateConversationCommand, Guid>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;

    public CreateConversationCommandHandler(IDbContext dbContext, IUnitOfWork unitOfWork)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid, Error>> Handle(
        CreateConversationCommand request,
        CancellationToken cancellationToken)
    {
        var conversation = AssistantConversation.Create(
            request.UserId,
            request.RoleContext,
            request.Title ?? "New conversation");

        _dbContext.Insert(conversation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return conversation.Id.Value;
    }
}
