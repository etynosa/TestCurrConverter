namespace TestCurrConverter.Common
{
    public class RateLimitExceededException : Exception
    {
        public RateLimitExceededException(string message) : base(message){
        }
    }
}
