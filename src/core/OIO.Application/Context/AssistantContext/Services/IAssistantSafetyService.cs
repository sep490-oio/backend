namespace OIO.Application.Context.AssistantContext.Services;

public interface IAssistantSafetyService
{
    bool IsPromptInjection(string text);
    string ScrubForLogging(string text);
}
