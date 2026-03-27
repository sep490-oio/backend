using Microsoft.AspNetCore.Components;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Commons;

namespace OIO.Infrastructure.Mail.RazorEmails.Rendering.ViewModels;

public class MainLayoutBase : LayoutComponentBase
{
    [Inject] protected IClock Clock { get; set; } = default!;
    [Inject] protected IAppInfo AppInfo { get; set; } = default!;

    protected string AppName => AppInfo.AppName;
    protected string BaseUrl => AppInfo.FeUrl;
    protected int CurrentYear => Clock.UtcNow.Year;
}
