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
    private readonly IAppConfigs _appConfigs;

    public GetUploadContextsQueryHandler(IAppConfigs appConfigs)
    {
        _appConfigs = appConfigs;
    }

    public async Task<Result<IReadOnlyList<UploadContextDto>, Error>> Handle(
        GetUploadContextsQuery request,
        CancellationToken cancellationToken)
    {
        var contexts = await _appConfigs.Media.GetUploadContextsAsync(cancellationToken);

        return contexts.Select(c => new UploadContextDto(
                Name: c.Name,
                ResourceType: c.ResourceType,
                MaxFileSizeBytes: c.MaxFileSizeBytes,
                AllowedFormats: c.AllowedFormats,
                MaxUploadsPerEntity: c.MaxUploadsPerEntity))
            .ToList();;
    }
}