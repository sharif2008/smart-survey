namespace SurveyApi.Options;

/// <summary>
/// Configuration for rate limiting public survey submissions (POST /api/responses) per client IP.
/// </summary>
public class SubmissionRateLimitOptions
{
    public const string SectionName = "SubmissionRateLimit";

    /// <summary>Maximum number of submissions allowed per client in the time window. Default 10.</summary>
    public int PermitLimit { get; set; } = 10;

    /// <summary>Time window in minutes (sliding). Default 1.</summary>
    public int WindowMinutes { get; set; } = 1;

    /// <summary>When true, rate limiting is disabled (e.g. for testing).</summary>
    public bool Disabled { get; set; }
}
