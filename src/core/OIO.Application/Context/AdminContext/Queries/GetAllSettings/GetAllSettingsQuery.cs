using System.Text.Json;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AdminContext.DTOs;
using OIO.Domain.Context.Shared.Entities;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AdminContext.Queries.GetAllSettings;

public sealed record GetAllSettingsQuery : IQuery<IReadOnlyList<SystemSettingDto>>;


internal sealed class GetAllSettingsQueryHandler
    : IQueryHandler<GetAllSettingsQuery, IReadOnlyList<SystemSettingDto>>
{
    private readonly IDbContext _dbContext;

    public GetAllSettingsQueryHandler(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<IReadOnlyList<SystemSettingDto>, Error>> Handle(
        GetAllSettingsQuery request,
        CancellationToken cancellationToken)
    {
        var settings = await _dbContext.Set<SystemSetting>()
            .AsNoTracking()
            .OrderBy(s => s.Id)
            .Select(s => new SystemSettingDto(
                s.Id.Value,
                JsonSerializer.Deserialize<object>(s.Value),
                s.ValueType,
                s.Description,
                s.CreatedAt,
                s.ModifiedAt,
                s.ModifiedBy))
            .ToListAsync(cancellationToken);

        return settings;
    }
}