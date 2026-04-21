using Microsoft.AspNetCore.SignalR;
using OIO.Api.Hubs;
using OIO.Application.Context.UserContext.Hubs;

namespace OIO.Api.Services;

/// <summary>
/// Api-layer implementation of <see cref="ITermsHubBroadcaster"/>. Broadcasts
/// <c>TermsActivated</c> to the <c>terms:authenticated</c> group on the <see cref="UserHub"/>.
/// </summary>
internal sealed class TermsHubBroadcaster : ITermsHubBroadcaster
{
    internal const string AuthenticatedGroup = "terms:authenticated";
    private const string ClientMethodName = "TermsActivated";

    private readonly IHubContext<UserHub> _hub;

    public TermsHubBroadcaster(IHubContext<UserHub> hub)
    {
        _hub = hub;
    }

    public Task BroadcastTermsActivatedAsync(
        TermsActivatedBroadcast payload,
        CancellationToken cancellationToken = default)
    {
        return _hub.Clients
            .Group(AuthenticatedGroup)
            .SendAsync(ClientMethodName, payload, cancellationToken);
    }
}
