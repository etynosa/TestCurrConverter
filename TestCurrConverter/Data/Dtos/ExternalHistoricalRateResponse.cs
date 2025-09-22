namespace TestCurrConverter.Data.Dtos
{
    public class ExternalHistoricalRateResponse
    {
        public string? Base { get; set; }
        public string? Target { get; set; }
        public Dictionary<string, decimal> Rates { get; set; } = new();
    }
}
