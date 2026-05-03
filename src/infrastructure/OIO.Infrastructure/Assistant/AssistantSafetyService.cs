using System.Text.RegularExpressions;
using OIO.Application.Context.AssistantContext.Services;

namespace OIO.Infrastructure.Assistant;

internal sealed partial class AssistantSafetyService : IAssistantSafetyService
{
    [GeneratedRegex(@"\b(ignore|disregard|forget)\s+(all\s+)?(previous|prior|earlier|above)\s+(instructions?|prompts?|rules?)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex InjectionInstructionsRegex();

    [GeneratedRegex(@"\b(reveal|show|print|leak|expose)\s+(your|the)?\s*(system\s+prompt|hidden\s+rules|internal\s+(prompt|instruction))\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex InjectionRevealRegex();

    [GeneratedRegex(@"\b(act|pretend|roleplay)\s+as\s+(a\s+)?(developer|admin|root|system)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex InjectionRolePlayRegex();

    [GeneratedRegex(@"[A-Za-z0-9+/=]{200,}",
        RegexOptions.CultureInvariant)]
    private static partial Regex SuspiciousBlobRegex();

    [GeneratedRegex(@"[\w\.-]+@[\w\.-]+\.[A-Za-z]{2,}",
        RegexOptions.CultureInvariant)]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"(0|\+84)\d{9,10}",
        RegexOptions.CultureInvariant)]
    private static partial Regex VnPhoneRegex();

    [GeneratedRegex(@"\b(eyJ[\w-]{6,}\.[\w-]{6,}\.[\w-]{6,})\b",
        RegexOptions.CultureInvariant)]
    private static partial Regex JwtRegex();

    public bool IsPromptInjection(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        return InjectionInstructionsRegex().IsMatch(text)
            || InjectionRevealRegex().IsMatch(text)
            || InjectionRolePlayRegex().IsMatch(text)
            || SuspiciousBlobRegex().IsMatch(text);
    }

    public string ScrubForLogging(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var scrubbed = JwtRegex().Replace(text, "<jwt>");
        scrubbed = EmailRegex().Replace(scrubbed, "<email>");
        scrubbed = VnPhoneRegex().Replace(scrubbed, "<phone>");
        return scrubbed.Length <= 500 ? scrubbed : scrubbed[..500] + "…";
    }
}
