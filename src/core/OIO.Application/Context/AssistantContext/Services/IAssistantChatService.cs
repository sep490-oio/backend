using CSharpFunctionalExtensions;
using OIO.Application.Context.AssistantContext.Services.Models;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AssistantContext.Services;

public interface IAssistantChatService
{
    Task<Result<AssistantChatResponse, Error>> SendAsync(
        AssistantChatRequest request,
        CancellationToken ct);
}
