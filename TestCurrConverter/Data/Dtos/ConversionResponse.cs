namespace TestCurrConverter.Data.Dtos
{
    public class ConversionResponse
    {
        public string? FromCurrency { get; set; }
        public string? ToCurrency { get; set; }
        public decimal OriginalAmount { get; set; }
        public decimal ConvertedAmount { get; set; }
        public decimal ExchangeRate { get; set; }
        public DateTime Date { get; set; }
        public bool IsRealTime { get; set; }
    }
}
