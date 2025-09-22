using System.ComponentModel.DataAnnotations;

namespace TestCurrConverter.Data.Models
{
    public class CurrencyPair: BaseModel<int>
    {
        [Required]
        [StringLength(3, MinimumLength = 3)]
        public string? BaseCurrency { get; set; }

        [Required]
        [StringLength(3, MinimumLength = 3)]
        public string? TargetCurrency { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime LastUpdated { get; set; }
    }
}
