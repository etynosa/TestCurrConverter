using System.ComponentModel.DataAnnotations;

namespace TestCurrConverter.Data.Dtos
{
    public class ConversionRequest
    {
        [Required]
        [StringLength(3, MinimumLength = 3)]
        public string? FromCurrency { get; set; }

        [Required]
        [StringLength(3, MinimumLength = 3)]
        public string? ToCurrency { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }
    }

    public class HistoricalConversionRequest : ConversionRequest
    {
        [Required]
        public DateTime Date { get; set; }
    }
}
