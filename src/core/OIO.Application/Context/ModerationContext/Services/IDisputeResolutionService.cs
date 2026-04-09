using CSharpFunctionalExtensions;
using OIO.Domain.Context.ModerationContext.Aggregates.Disputes;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.ModerationContext.Services;

public interface IDisputeResolutionService
{
    Task<UnitResult<Error>> ApplyResolutionAsync(Dispute dispute, CancellationToken ct);
}
