namespace TestCurrConverter.Data.Dtos
{
    public class ExternalRealTimeRateResponse
    {
        public string? Base { get; set; }
        public string? Date { get; set; }
        public Dictionary<string, decimal> Rates { get; set; } = new();
    }
}
