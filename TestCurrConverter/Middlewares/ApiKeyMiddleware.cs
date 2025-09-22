using System.Text.Json;
using TestCurrConverter.Data.Dtos;

namespace TestCurrConverter.Middlewares
{
    public class ApiKeyMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ApiKeyMiddleware> _logger;

        public ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<ApiKeyMiddleware> logger)
        {
            _next = next;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path.StartsWithSegments("/health") ||
                context.Request.Path.StartsWithSegments("/swagger"))
            {
                await _next(context);
                return;
            }

            if (!context.Request.Headers.TryGetValue("X-API-Key", out var apiKey) ||
                string.IsNullOrEmpty(apiKey))
            {
                await WriteErrorResponse(context, "API key is required", 401);
                return;
            }

            var validApiKeys = _configuration.GetSection("ApiKeys").Get<string[]>() ??
                             new[] { "default-api-key", "test-key-1", "test-key-2" };

            if (!validApiKeys.Contains<string>(apiKey))
            {
                _logger.LogWarning("Invalid API key attempted: {ApiKey}", apiKey);
                await WriteErrorResponse(context, "Invalid API key", 401);
                return;
            }


            context.Items["ApiKey"] = apiKey.ToString();

            await _next(context);
        }

        private static async Task WriteErrorResponse(HttpContext context, string message, int statusCode)
        {
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            var errorResponse = new ApiErrorResponse
            {
                Error = "Unauthorized",
                Message = message,
                StatusCode = statusCode
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
        }
    }
}
