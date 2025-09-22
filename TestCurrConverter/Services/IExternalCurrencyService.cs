using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using System.Text.Json;
using TestCurrConverter.Data.Dtos;

namespace TestCurrConverter.Services
{
    public interface IExternalCurrencyService
    {
        Task<ExternalRealTimeRateResponse?> GetRealTimeRatesAsync(string baseCurrency, string apiKey, CancellationToken ct = default);
        Task<ExternalHistoricalRateResponse?> GetHistoricalRatesAsync(HistoricalRateRequest request, string apiKey, CancellationToken ct = default);
    }

    public class ExternalCurrencyService : IExternalCurrencyService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ExternalCurrencyService> _logger;
        private readonly IRateLimitService _rateLimitService;
        private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;

        public ExternalCurrencyService(HttpClient httpClient,
            ILogger<ExternalCurrencyService> logger, IRateLimitService rateLimitService)
        {
            _httpClient = httpClient;
            _logger = logger;
            _rateLimitService = rateLimitService;
            _retryPolicy = Policy
                            .Handle<HttpRequestException>()
                            .OrResult<HttpResponseMessage>(r =>
                                (int)r.StatusCode >= 500 || r.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                            .WaitAndRetryAsync(3, retryAttempt =>
                                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                                onRetry: (outcome, timespan, retryAttempt, context) =>
                                {
                                    _logger.LogWarning("Retry {RetryAttempt} after {TotalSeconds}s due to: {Exception}",
                                        retryAttempt, timespan.TotalSeconds, outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());
                                });
        }

        public async Task<ExternalRealTimeRateResponse?> GetRealTimeRatesAsync(string baseCurrency, string apiKey, CancellationToken ct = default)
        {
            if (!await _rateLimitService.IsRequestAllowedAsync(apiKey))
            {
                _logger.LogWarning("Rate limit exceeded for API key: {ApiKey}", apiKey);
                throw new InvalidOperationException("Rate limit exceeded");
            }

            var maxRetries = 3;
            var baseDelay = TimeSpan.FromSeconds(1);

            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                var response = await _retryPolicy.ExecuteAsync(async ct =>
                   {
                       await SimulateNetworkDelay();
                       var request = new HttpRequestMessage(HttpMethod.Get,
                        $"real-time?base={baseCurrency}");

                       request.Headers.Add("X-API-Key", apiKey);

                       return await _httpClient.SendAsync(request);

                   }, ct);


                if (Random.Shared.Next(1, 10) == 1 && attempt < 2) // 10% chance of failure on first 2 attempts
                {
                    throw new HttpRequestException("Simulated network error");
                }

                await HandleResponseErrors(response, "real-time rates");

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ExternalRealTimeRateResponse>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? throw new InvalidOperationException("Failed to deserialize real-time rates response");


                var mockResponse = new ExternalRealTimeRateResponse
                {
                    Base = baseCurrency,
                    Date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                    Rates = GetSimulatedRates(baseCurrency)
                };

                await _rateLimitService.RecordRequestAsync(apiKey);
                return mockResponse;
            }

            _logger.LogError("Failed to fetch real-time rates after {MaxRetries} attempts", maxRetries);
            return null;

        }

        public async Task<ExternalHistoricalRateResponse?> GetHistoricalRatesAsync(HistoricalRateRequest request, string apiKey, CancellationToken ct = default)
        {

            if (!await _rateLimitService.IsRequestAllowedAsync(apiKey))
            {
                _logger.LogWarning("Rate limit exceeded for API key: {ApiKey}", apiKey);
                throw new InvalidOperationException("Rate limit exceeded");
            }

            var maxRetries = 3;
            var baseDelay = TimeSpan.FromSeconds(1);

            for (int attempt = 0; attempt < maxRetries; attempt++)
            {

                var response = await _retryPolicy.ExecuteAsync(async ct =>
                   {
                       await SimulateNetworkDelay();
                       var res = new HttpRequestMessage(HttpMethod.Get,
                        $"historical?base={request.BaseCurrency}&target={request.TargetCurrency}&start_date={request.StartDate:yyyy-MM-dd}&end_date={request.EndDate:yyyy-MM-dd}");

                       res.Headers.Add("X-API-Key", apiKey);

                       return await _httpClient.SendAsync(res);
                   }, ct);

                if (Random.Shared.Next(1, 10) == 1 && attempt < 2)
                {
                    throw new HttpRequestException("Simulated network error");
                }

                await HandleResponseErrors(response, "historical rates");

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ExternalHistoricalRateResponse>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? throw new InvalidOperationException("Failed to deserialize historical rates response");

                var mockResponse = new ExternalHistoricalRateResponse
                {
                    Base = request.BaseCurrency,
                    Target = request.TargetCurrency,
                    Rates = GenerateHistoricalRates(request.BaseCurrency, request.TargetCurrency, request.StartDate, request.EndDate)
                };

                await _rateLimitService.RecordRequestAsync(apiKey);
                return mockResponse;
            }

            _logger.LogError("Failed to fetch historical rates after {MaxRetries} attempts", maxRetries);
            return null;
        }

        private static async Task SimulateNetworkDelay()
        {
            await Task.Delay(Random.Shared.Next(100, 500));
        }

        private static Dictionary<string, decimal> GetSimulatedRates(string baseCurrency)
        {
            var rates = new Dictionary<string, decimal>();
            var random = new Random();

            if (baseCurrency == "USD")
            {
                rates["GBP"] = 0.80m + (decimal)(random.NextDouble() * 0.05 - 0.025);
                rates["EUR"] = 0.92m + (decimal)(random.NextDouble() * 0.05 - 0.025);
                rates["JPY"] = 155.00m + (decimal)(random.NextDouble() * 10 - 5);
            }
            else if (baseCurrency == "GBP")
            {
                rates["USD"] = 1.25m + (decimal)(random.NextDouble() * 0.05 - 0.025);
                rates["EUR"] = 1.15m + (decimal)(random.NextDouble() * 0.05 - 0.025);
                rates["JPY"] = 193.75m + (decimal)(random.NextDouble() * 10 - 5);
            }
            else if (baseCurrency == "EUR")
            {
                rates["USD"] = 1.09m + (decimal)(random.NextDouble() * 0.05 - 0.025);
                rates["GBP"] = 0.87m + (decimal)(random.NextDouble() * 0.05 - 0.025);
                rates["JPY"] = 168.48m + (decimal)(random.NextDouble() * 10 - 5);
            }

            return rates;
        }

        private static Dictionary<string, decimal> GenerateHistoricalRates(string baseCurrency, string targetCurrency, DateTime startDate, DateTime endDate)
        {
            var rates = new Dictionary<string, decimal>();
            var random = new Random();
            var baseRate = GetBaseRate(baseCurrency, targetCurrency);

            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                var variance = (decimal)(random.NextDouble() * 0.1 - 0.05); // ±5% variance
                rates[date.ToString("yyyy-MM-dd")] = baseRate + (baseRate * variance);
            }

            return rates;
        }

        private static decimal GetBaseRate(string baseCurrency, string targetCurrency)
        {
            return (baseCurrency, targetCurrency) switch
            {
                ("USD", "GBP") => 0.80m,
                ("USD", "EUR") => 0.92m,
                ("USD", "JPY") => 155.00m,
                ("GBP", "USD") => 1.25m,
                ("GBP", "EUR") => 1.15m,
                ("EUR", "USD") => 1.09m,
                ("EUR", "GBP") => 0.87m,
                _ => 1.00m
            };
        }

        private async Task HandleResponseErrors(HttpResponseMessage response, string operation)
        {
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("External API error for {Operation}: {StatusCode} - {Content}",
                    operation, response.StatusCode, errorContent);

                throw response.StatusCode switch
                {
                    System.Net.HttpStatusCode.TooManyRequests => new InvalidOperationException("Rate limit exceeded"),
                    System.Net.HttpStatusCode.Unauthorized => new UnauthorizedAccessException("Invalid API key"),
                    System.Net.HttpStatusCode.NotFound => new KeyNotFoundException("Currency not found"),
                    _ => new HttpRequestException($"External API error: {response.StatusCode} - {errorContent}")
                };
            }
        }
    }
}
