namespace OIO.Application.Context.MediaContext.DTOs;

public sealed record UploadContextDto(
    string Name,
    string ResourceType,
    long MaxFileSizeBytes,
    string[] AllowedFormats,
    int MaxUploadsPerEntity);