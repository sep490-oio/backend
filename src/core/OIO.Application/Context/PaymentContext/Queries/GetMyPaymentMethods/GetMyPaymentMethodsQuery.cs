using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.PaymentContext.DTOs;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.PaymentContext.Aggregates.PaymentMethods;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.PaymentContext.Queries.GetMyPaymentMethods;

public sealed record GetMyPaymentMethodsQuery() : IQuery<IReadOnlyList<PaymentMethodDto>>;

internal sealed class GetMyPaymentMethodsQueryHandler
    : IQueryHandler<GetMyPaymentMethodsQuery, IReadOnlyList<PaymentMethodDto>>
{
    private readonly IDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public GetMyPaymentMethodsQueryHandler(IDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<PaymentMethodDto>, Error>> Handle(
        GetMyPaymentMethodsQuery request,
        CancellationToken cancellationToken)
    {
        var items = await _dbContext.Set<PaymentMethod>()
            .AsNoTracking()
            .Where(x => x.UserId == _currentUser.UserId)
            .OrderByDescending(x => x.IsDefault)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return items
            .Select(PaymentReadModelMapper.ToDto)
            .ToList();
    }
}
