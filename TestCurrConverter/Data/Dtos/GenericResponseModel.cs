namespace TestCurrConverter.Data.Dtos
{
    public class GenericResponseModel<T>
    {
        public T? Data { get; set; }        
        public string? Message { get; set; }
        public bool IsSuccess { get; set; }

        public GenericResponseModel(T? data, string? message, bool isSuccess)
        {
            Data = data;
            Message = message;
            IsSuccess = isSuccess;
        }
    }
}
