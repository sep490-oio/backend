using Microsoft.Extensions.Options;
using OIO.Application.Abstractions.Commons;

namespace OIO.Infrastructure.Settings.Apps;

public sealed class AppConfig : IAppInfo, IRuntimeSettings
{
    private readonly IOptionsMonitor<AppOptions> _app;
    private readonly IOptionsMonitor<FeaturesOptions> _features;
    private readonly IOptionsMonitor<AuctionOptions> _auction;
    private readonly IOptionsMonitor<ItemOptions> _item;
    private readonly IOptionsMonitor<MediaOptions> _media;
    private readonly IOptionsMonitor<AuthOptions> _auth;
    private readonly IOptionsMonitor<MonitoringOptions> _monitoring;
    private readonly IOptionsMonitor<OpsOptions> _ops;
    private readonly IOptionsMonitor<OrderOptions> _order;
    private readonly IOptionsMonitor<SettlementOptions> _settlement;

    public AppConfig(
        IOptionsMonitor<AppOptions> app,
        IOptionsMonitor<FeaturesOptions> features,
        IOptionsMonitor<AuctionOptions> auction,
        IOptionsMonitor<ItemOptions> item,
        IOptionsMonitor<MediaOptions> media,
        IOptionsMonitor<AuthOptions> auth,
        IOptionsMonitor<MonitoringOptions> monitoring,
        IOptionsMonitor<OpsOptions> ops,
        IOptionsMonitor<OrderOptions> order,
        IOptionsMonitor<SettlementOptions> settlement)
    {
        _app = app;
        _features = features;
        _auction = auction;
        _item = item;
        _media = media;
        _auth = auth;
        _monitoring = monitoring;
        _ops = ops;
        _order = order;
        _settlement = settlement;
    }

    public string AppName => _app.CurrentValue.AppName;
    public string Version => _app.CurrentValue.Version;
    public string FeUrl => _app.CurrentValue.FeUrl;
    public string BeUrl => _app.CurrentValue.BeUrl;
    public string EmailVerifyPath => _app.CurrentValue.EmailVerifyPath;
    public string ResetPasswordPath => _app.CurrentValue.ResetPasswordPath;
    public FeaturesOptions Features => _features.CurrentValue;

    public AuctionOptions Auction => _auction.CurrentValue;
    public ItemOptions Item => _item.CurrentValue;
    public MediaOptions Media => _media.CurrentValue;
    public AuthOptions Auth => _auth.CurrentValue;
    public MonitoringOptions Monitoring => _monitoring.CurrentValue;
    public OpsOptions Ops => _ops.CurrentValue;
    public OrderOptions Order => _order.CurrentValue;
    public SettlementOptions Settlement => _settlement.CurrentValue;
}
