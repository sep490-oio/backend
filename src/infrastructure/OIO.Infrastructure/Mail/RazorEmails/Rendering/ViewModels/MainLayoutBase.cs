using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using OIO.Application.Abstractions.Clock;
using OIO.Infrastructure.Settings;
using OIO.Infrastructure.Settings.Apps;

namespace OIO.Infrastructure.Mail.RazorEmails.Rendering.ViewModels;

public class MainLayoutBase : LayoutComponentBase
{
    [Inject] protected IClock Clock { get; set; } = default!;
    [Inject] protected IOptions<AppInfoOptions> AppInfoOptions { get; set; } = default!;

    protected string AppName => AppInfoOptions.Value.AppName;
    protected int CurrentYear => Clock.UtcNow.Year;
}