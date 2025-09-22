using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure.Internal;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using TestCurrConverter.Common;
using TestCurrConverter.Data;
using TestCurrConverter.Data.Models;

namespace TestCurrConverter.Services
{
    public interface IRateLimitService
    {
        Task<bool> IsRequestAllowedAsync(string apiKey);
        Task RecordRequestAsync(string apiKey);
    }

    public class RateLimitService : IRateLimitService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RateLimitService> _logger;
        private readonly AsyncRetryPolicy _retryPolicy;

        public RateLimitService(ApplicationDbContext context,
            IConfiguration configuration, ILogger<RateLimitService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _retryPolicy = Policy
                        .Handle<HttpRequestException>()
                        //.OrResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                            onRetry: (outcome, timespan, retryCount, context) =>
                            {
                                _logger.LogWarning("External API retry {RetryCount} after {Delay}s due to {Outcome}",
                                    retryCount, timespan.TotalSeconds, outcome.Message ?? "non-success status");
                            });
        }

        public async Task<bool> IsRequestAllowedAsync(string apiKey)
        {
            var rateLimitPerHour = _configuration.GetValue<int>("RateLimit:RequestsPerHour", 1000);
            var now = DateTime.UtcNow;
            var windowStart = now.AddHours(-1);

            var usage = await _context.ApiUsages
                .FirstOrDefaultAsync(u => u.ApiKey == apiKey && u.WindowStart >= windowStart);

            if (usage == null)
            {
                return true;
            }

            return usage.RequestCount < rateLimitPerHour;
        }

        public async Task RecordRequestAsync(string apiKey)
        {
            var now = DateTime.UtcNow;
            var windowStart = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc);

            var usage = await _context.ApiUsages
                .FirstOrDefaultAsync(u => u.ApiKey == apiKey && u.WindowStart == windowStart);

            if (usage == null)
            {
                usage = new ApiUsage
                {
                    ApiKey = apiKey,
                    RequestCount = 1,
                    LastRequest = now,
                    WindowStart = windowStart
                };
                _context.ApiUsages.Add(usage);
            }
            else
            {
                usage.RequestCount++;
                usage.LastRequest = now;
            }

            await _context.SaveChangesAsync();
        }
    }
}
