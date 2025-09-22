using System.ComponentModel.DataAnnotations;

namespace TestCurrConverter.Data.Dtos
{
    public class HistoricalRateRequest
    {
        [Required]
        [StringLength(3, MinimumLength = 3)]
        public string? BaseCurrency { get; set; }

        [Required]
        [StringLength(3, MinimumLength = 3)]
        public string? TargetCurrency { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }
    }
}
