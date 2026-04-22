namespace OIO.Application.Context.UserContext.Hubs;

/// <summary>
/// Scoped broadcast of the <c>TermsActivated</c> SignalR message to the
/// <c>terms:authenticated</c> group (plan §3.6.2 step 2 / C4). Implementation lives in
/// <c>OIO.Api</c> because <see cref="Microsoft.AspNetCore.SignalR.IHubContext{THub}"/> requires
/// the hub type and the Api project owns the hub class.
/// </summary>
public interface ITermsHubBroadcaster
{
    Task BroadcastTermsActivatedAsync(
        TermsActivatedBroadcast payload,
        CancellationToken cancellationToken = default);
}

public sealed record TermsActivatedBroadcast(
    string TermType,
    string NewVersionId,
    int NewVersion,
    DateTime ActivatedAt);
