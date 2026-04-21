using CSharpFunctionalExtensions;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Services;

/// <summary>
/// BE authoritative gate for forced terms re-acceptance (plan §3.6.4 / B6).
/// Gated command handlers call <see cref="EnsureAsync"/> as their first check; on a pending
/// acceptance it returns <c>Error.Conflict("Terms.PendingAcceptance", ...)</c> and the handler
/// short-circuits. The FE 409 interceptor maps this into the <c>TermsAcceptanceModal</c>.
/// </summary>
public interface IEnsureTermsAcceptedService
{
    Task<UnitResult<Error>> EnsureAsync(
        UserId userId,
        IEnumerable<string> requiredTermTypes,
        CancellationToken cancellationToken = default);
}
