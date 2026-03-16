using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SurveyApi.Options;

namespace SurveyApi.Helpers;

/// <summary>
/// Rate limits POST /api/responses (public survey submissions) per client IP using a sliding window.
/// Returns 429 Too Many Requests with Retry-After when the limit is exceeded.
/// </summary>
public class SubmissionRateLimitMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private static readonly ConcurrentDictionary<string, object> Locks = new();

    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;
    private readonly SubmissionRateLimitOptions _options;
    private readonly ILogger<SubmissionRateLimitMiddleware> _logger;

    public SubmissionRateLimitMiddleware(
        RequestDelegate next,
        IMemoryCache cache,
        IOptions<SubmissionRateLimitOptions> options,
        ILogger<SubmissionRateLimitMiddleware> logger)
    {
        _next = next;
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (_options.Disabled)
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        var path = context.Request.Path.Value ?? "";
        var isSubmission = context.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase)
            && (path.Equals("/api/responses", StringComparison.OrdinalIgnoreCase)
                || path.TrimEnd('/').Equals("/api/responses", StringComparison.OrdinalIgnoreCase));

        if (!isSubmission)
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        var clientIp = GetClientIp(context);
        if (string.IsNullOrEmpty(clientIp))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        var cacheKey = "submission_ratelimit:" + clientIp;
        var window = TimeSpan.FromMinutes(_options.WindowMinutes);
        var now = DateTimeOffset.UtcNow;
        var cutoff = now - window;
        int? retryAfterSeconds = null;

        var lockObj = Locks.GetOrAdd(cacheKey, _ => new object());
        lock (lockObj)
        {
            var timestamps = _cache.Get<List<long>>(cacheKey) ?? new List<long>();
            timestamps.RemoveAll(t => t < cutoff.UtcTicks);

            int currentCount = timestamps.Count;

            if (currentCount >= _options.PermitLimit)
            {
                var oldest = new DateTimeOffset(timestamps.Min(), TimeSpan.Zero);
                var retryAfter = (int)Math.Ceiling((oldest + window - now).TotalSeconds);
                if (retryAfter < 1) retryAfter = 1;

                retryAfterSeconds = retryAfter;
            }
            else
            {
                timestamps.Add(now.UtcTicks);
                _cache.Set(cacheKey, timestamps, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = window.Add(TimeSpan.FromMinutes(1))
                });
            }
        }

        if (retryAfterSeconds.HasValue)
        {
            var retryAfter = retryAfterSeconds.Value;
            _logger.LogWarning("Submission rate limit exceeded for IP {ClientIp}. Retry after {RetryAfterSeconds}s.", clientIp, retryAfter);
            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.Headers.RetryAfter = retryAfter.ToString();
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                message = "Too many submissions. Please try again later.",
                retryAfterSeconds = retryAfter
            }, JsonOptions)).ConfigureAwait(false);
            return;
        }

        await _next(context).ConfigureAwait(false);
    }

    private static string? GetClientIp(HttpContext context)
    {
        var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwarded))
        {
            var first = forwarded.Split(',', StringSplitOptions.TrimEntries).FirstOrDefault();
            if (!string.IsNullOrEmpty(first))
                return first;
        }
        return context.Connection.RemoteIpAddress?.ToString();
    }
}
