using OIO.Application.Context.AssistantContext.Services.Models;

namespace OIO.Application.Context.AssistantContext.Services;

public interface IAssistantKnowledgeRetriever
{
    IReadOnlyList<KnowledgeEntry> Retrieve(string query, int topN = 3);
}
