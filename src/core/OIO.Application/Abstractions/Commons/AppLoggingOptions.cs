namespace OIO.Application.Abstractions.Commons;

public sealed class AppLoggingOptions
{
    public const string SectionName = "AppLogging";

    public AppRequestLoggingOptions Request { get; set; } = new();
    public AppJobLoggingOptions Jobs { get; set; } = new();
    public AppMediatRLoggingOptions MediatR { get; set; } = new();
}

public sealed class AppRequestLoggingOptions
{
    public int SlowRequestThresholdMs { get; set; } = 1_000;
    public bool LogReadSuccessAsDebug { get; set; } = true;
    public List<string> ExcludedPathPrefixes { get; set; } =
    [
        "/health",
        "/docs",
        "/openapi",
        "/swagger",
        "/hubs"
    ];
}

public sealed class AppJobLoggingOptions
{
    public int SlowJobThresholdMs { get; set; } = 2_000;
    public bool LogNoopRuns { get; set; }
}

public sealed class AppMediatRLoggingOptions
{
    public int SlowRequestThresholdMs { get; set; } = 500;
    public bool LogSuccessfulQueriesAtDebug { get; set; } = true;
}
