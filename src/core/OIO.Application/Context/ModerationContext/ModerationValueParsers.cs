using CSharpFunctionalExtensions;
using OIO.Domain.Context.ModerationContext.Enums;
using OIO.Domain.Context.UserContext.Enums;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.ModerationContext;

internal static class ModerationValueParsers
{
    public static Result<AlertSeverity, Error> ParseAlertSeverity(string severity)
    {
        return severity.Trim().ToLowerInvariant() switch
        {
            "low" => AlertSeverity.Low,
            "medium" => AlertSeverity.Medium,
            "high" => AlertSeverity.High,
            "critical" => AlertSeverity.Critical,
            _ => Error.Validation("severity", "MonitoringAlert.InvalidSeverity", $"Unsupported alert severity '{severity}'.")
        };
    }

    public static Result<RiskFlagSeverity, Error> ParseRiskSeverity(string severity)
    {
        return severity.Trim().ToLowerInvariant() switch
        {
            "low" => RiskFlagSeverity.Low,
            "medium" => RiskFlagSeverity.Medium,
            "high" => RiskFlagSeverity.High,
            "critical" => RiskFlagSeverity.Critical,
            _ => Error.Validation("severity", "UserRiskFlag.InvalidSeverity", $"Unsupported risk flag severity '{severity}'.")
        };
    }
}
