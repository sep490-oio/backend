using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AdminContext.DTOs;
using OIO.Domain.Context.Shared.Entities;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AdminContext.Queries.GetSettingByKey;

public sealed record GetSettingByKeyQuery(string Key) : IQuery<SystemSettingDto>;

internal sealed class GetSettingByKeyQueryHandler
    : IQueryHandler<GetSettingByKeyQuery, SystemSettingDto>
{
    private readonly IDbContext _dbContext;

    public GetSettingByKeyQueryHandler(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<SystemSettingDto, Error>> Handle(
        GetSettingByKeyQuery request,
        CancellationToken cancellationToken)
    {
        var setting = await _dbContext.Set<SystemSetting>()
            .AsNoTracking()
            .Where(s => s.Id == request.Key)
            .Select(s => new SystemSettingDto(
                s.Id.Value,
                s.Value,
                s.ValueType,
                s.Description,
                s.CreatedAt,
                s.ModifiedAt,
                s.ModifiedBy))
            .FirstOrDefaultAsync(cancellationToken);

        //TODO: bring this Error to AdminErrors
        if (setting is null)
            return Error.NotFound("Setting.NotFound",
                $"Setting '{request.Key}' not found.");

        return setting;
    }
}