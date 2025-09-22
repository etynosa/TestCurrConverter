namespace TestCurrConverter.Data.Dtos
{
    public class HistoricalRateResponse
    {
        public string? BaseCurrency { get; set; }
        public string? TargetCurrency { get; set; }
        public Dictionary<DateTime, decimal> Rates { get; set; } = new();
    }
}
