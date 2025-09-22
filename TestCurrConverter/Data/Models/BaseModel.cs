namespace TestCurrConverter.Data.Models
{
    public abstract class BaseModel<T> 
    {       
        public T Id { get; set; }
        public DateTimeOffset CreatedDate { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedDate { get; set; }
    }
}
