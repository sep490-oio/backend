using System.Text.Json;
using CSharpFunctionalExtensions;
using OIO.Domain.Context.Shared.ValueObjects;

namespace OIO.Domain.Context.WarehouseContext.ValueObjects;

public sealed class InspectionEvidence : ValueObject
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private InspectionEvidence() { }

    private InspectionEvidence(string rawJson)
    {
        RawJson = rawJson;
    }

    public string RawJson { get; private set; } = "[]";

    public static InspectionEvidence Empty => new("[]");

    public static InspectionEvidence From(string rawJson)
        => new(string.IsNullOrWhiteSpace(rawJson) ? "[]" : rawJson);

    public IReadOnlyList<InspectionEvidenceSnapshot> ToSnapshots()
        => JsonSerializer.Deserialize<List<InspectionEvidenceSnapshot>>(RawJson, JsonOptions) ?? [];

    public bool HasAny() => ToSnapshots().Count > 0;

    public InspectionEvidence RefreshSnapshot(
        string oldPublicId,
        StorageRef storageRef,
        MediaInfo info)
    {
        var snapshots = ToSnapshots().ToList();
        var index = snapshots.FindIndex(snapshot => snapshot.PublicId == oldPublicId);
        if (index < 0)
            return this;

        snapshots[index] = InspectionEvidenceSnapshot.Create(storageRef, info);
        return new InspectionEvidence(JsonSerializer.Serialize(snapshots, JsonOptions));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return RawJson;
    }
}

public sealed record InspectionEvidenceSnapshot(
    string PublicId,
    string Folder,
    string? SecureUrl,
    string? FileName,
    long? Bytes,
    string? Format,
    int? Width,
    int? Height,
    double? DurationSeconds)
{
    public static InspectionEvidenceSnapshot Create(StorageRef storageRef, MediaInfo info) =>
        new(
            storageRef.PublicId,
            storageRef.Folder,
            info.SecureUrl,
            info.FileName,
            info.Bytes,
            info.Format,
            info.Width,
            info.Height,
            info.DurationSeconds);
}
