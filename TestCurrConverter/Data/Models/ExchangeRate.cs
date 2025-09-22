using System.ComponentModel.DataAnnotations;

namespace TestCurrConverter.Data.Models
{
    public class ExchangeRate : BaseModel<int>
    {

        [Required]
        [StringLength(3, MinimumLength = 3)]
        public string? BaseCurrency { get; set; }

        [Required]
        [StringLength(3, MinimumLength = 3)]
        public string? TargetCurrency { get; set; }
        [Required]
        [Range(0.000001, double.MaxValue)]
        public decimal Rate { get; set; }

        [Required]
        public DateTime Date { get; set; }
        public bool IsRealTime { get; set; }
    }
}
