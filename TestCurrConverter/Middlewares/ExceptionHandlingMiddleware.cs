using System.Net;
using System.Text.Json;
using TestCurrConverter.Common;
using TestCurrConverter.Data.Dtos;

namespace TestCurrConverter.Middlewares
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred");
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var response = exception switch
            {
                InvalidOperationException ex when ex.Message.Contains("Rate limit") => new ApiErrorResponse
                {
                    Error = "RateLimitExceeded",
                    Message = "API rate limit exceeded. Please try again later.",
                    StatusCode = (int)HttpStatusCode.TooManyRequests
                },
                ArgumentException => new ApiErrorResponse
                {
                    Error = "BadRequest",
                    Message = "Invalid request parameters",
                    StatusCode = (int)HttpStatusCode.BadRequest
                },
                TimeoutException => new ApiErrorResponse
                {
                    Error = "ServiceTimeout",
                    Message = CustomMessages.ServiceTimeOut,
                    StatusCode = (int)HttpStatusCode.RequestTimeout
                },
                RateLimitExceededException => new ApiErrorResponse
                {
                    Error = "RateLimitExceeded",
                    Message = "API rate limit exceeded. Please try again later.",
                    StatusCode = (int)HttpStatusCode.TooManyRequests
                },
                _ => new ApiErrorResponse
                {
                    Error = "InternalServerError",
                    Message = CustomMessages.InternalErrorMessage,
                    StatusCode = (int)HttpStatusCode.InternalServerError
                }
            };

            context.Response.StatusCode = response.StatusCode;
            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}
