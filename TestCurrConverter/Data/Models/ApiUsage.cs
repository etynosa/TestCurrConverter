using System.ComponentModel.DataAnnotations;

namespace TestCurrConverter.Data.Models
{
    public class ApiUsage: BaseModel<int>
    {
        [Required]
        public string ApiKey { get; set; } = string.Empty;

        public int RequestCount { get; set; }
        public DateTime LastRequest { get; set; }
        public DateTime WindowStart { get; set; }
    }
}
