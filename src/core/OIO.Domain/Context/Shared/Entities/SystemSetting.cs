using OIO.Domain.Context.Shared.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.Shared.Entities;

public sealed class SystemSetting : BaseEntity<SystemSettingId>, IAuditableEntity
{
    public string Value { get; private set; } = null!;
    public string? Description { get; private set; }
    public string ValueType { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? ModifiedAt { get; private set; }
    public string? ModifiedBy { get; private set; }

    private SystemSetting() { }

    public static SystemSetting Create(
        DateTime nowUtc,
        string key, 
        string value,
        string valueType,
        string? description = null)
    {
        return new SystemSetting
        {
            Id = SystemSettingId.From(key),
            Value = value,
            ValueType = valueType,
            Description = description,
            CreatedAt = nowUtc
        };
    }

    public void Update(
        DateTime nowUtc,
        string value,
        string? modifiedBy = null)
    {
        Value = value;
        ModifiedAt = nowUtc;
        ModifiedBy = modifiedBy;
    }
}