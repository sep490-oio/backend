using CSharpFunctionalExtensions;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.MediaContext.DTOs;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.MediaContext.Queries.GetUploadContexts;

public sealed record GetUploadContextsQuery : IQuery<IReadOnlyList<UploadContextDto>>;

internal sealed class GetUploadContextsQueryHandler
    : IQueryHandler<GetUploadContextsQuery, IReadOnlyList<UploadContextDto>>
{
    private readonly IRuntimeSettings _runtimeSettings;

    public GetUploadContextsQueryHandler(IRuntimeSettings runtimeSettings)
    {
        _runtimeSettings = runtimeSettings;
    }

    public async Task<Result<IReadOnlyList<UploadContextDto>, Error>> Handle(
        GetUploadContextsQuery request,
        CancellationToken cancellationToken)
    {
        var contexts = _runtimeSettings.Media.UploadContexts;

        return contexts.Select(c => new UploadContextDto(
                Name: c.Name,
                ResourceType: c.ResourceType,
                MaxFileSizeBytes: c.MaxFileSizeBytes,
                AllowedFormats: c.AllowedFormats,
                MaxUploadsPerEntity: c.MaxUploadsPerEntity))
            .ToList();;
    }
}
