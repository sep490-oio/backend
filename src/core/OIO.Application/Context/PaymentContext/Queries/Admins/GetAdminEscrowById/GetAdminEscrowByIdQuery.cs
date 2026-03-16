using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.PaymentContext.DTOs;
using OIO.Application.Context.PaymentContext.Queries;
using OIO.Domain.Context.PaymentContext.Aggregates.Escrows;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.PaymentContext.Queries.Admins.GetAdminEscrowById;

public sealed record GetAdminEscrowByIdQuery(Guid EscrowId) : IQuery<EscrowDetailDto>;

internal sealed class GetAdminEscrowByIdQueryHandler
    : IQueryHandler<GetAdminEscrowByIdQuery, EscrowDetailDto>
{
    private readonly IDbContext _dbContext;

    public GetAdminEscrowByIdQueryHandler(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<EscrowDetailDto, Error>> Handle(
        GetAdminEscrowByIdQuery request,
        CancellationToken cancellationToken)
    {
        var escrow = await _dbContext.Set<Escrow>()
            .AsNoTracking()
            .Include(x => x.Order)
            .Include(x => x.ReleaseEvents)
            .FirstOrDefaultAsync(x => x.Id.Value == request.EscrowId, cancellationToken);

        if (escrow is null)
            return Error.NotFound("Escrow.NotFound", "Escrow not found.");

        return PaymentReadModelMapper.ToDetailDto(escrow);
    }
}
