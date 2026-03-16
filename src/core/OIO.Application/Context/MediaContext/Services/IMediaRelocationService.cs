using OIO.Domain.Context.Shared.Entities;

namespace OIO.Application.Context.MediaContext.Services;

public interface IMediaRelocationService
{
    Task RelocateLinkedUploadAsync(MediaUpload upload, CancellationToken cancellationToken = default);
}
