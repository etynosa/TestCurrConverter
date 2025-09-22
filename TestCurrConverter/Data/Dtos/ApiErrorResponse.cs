namespace TestCurrConverter.Data.Dtos
{
    public class ApiErrorResponse
    {
        public string? Error { get; set; }
        public string? Message { get; set; }
        public int StatusCode { get; set; }
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    }
}
