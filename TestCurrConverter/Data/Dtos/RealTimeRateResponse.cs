namespace TestCurrConverter.Data.Dtos
{
    public class RealTimeRateResponse
    {
        public string? Base { get; set; }
        public string? Date { get; set; }
        public Dictionary<string, decimal> Rates { get; set; } = new();
    }
}
