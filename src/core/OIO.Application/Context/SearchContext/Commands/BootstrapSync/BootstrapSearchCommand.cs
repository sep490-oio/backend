using MediatR;
using OIO.Application.Abstractions.Search;

namespace OIO.Application.Context.SearchContext.Commands.BootstrapSync;

public record BootstrapSearchCommand : IRequest<bool>;

public class BootstrapSearchCommandHandler(
    IElasticsearchSyncService syncService,
    IElasticsearchService searchService) 
    : IRequestHandler<BootstrapSearchCommand, bool>
{
    public async Task<bool> Handle(BootstrapSearchCommand request, CancellationToken cancellationToken)
    {
        // 1. Recreate indices with correct mappings (e.g. Completion for suggest)
        await searchService.RecreateIndicesAsync(cancellationToken);

        // 2. Trigger the bulk sync logic
        await syncService.SyncAllAsync(cancellationToken);
        
        return true;
    }
}
