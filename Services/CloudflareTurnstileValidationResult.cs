namespace Tech_Store.Services
{
    public class CloudflareTurnstileValidationResult
    {
        public bool IsSuccess { get; init; }
        public string ErrorMessage { get; init; } = string.Empty;

        public static CloudflareTurnstileValidationResult Success()
        {
            return new CloudflareTurnstileValidationResult
            {
                IsSuccess = true
            };
        }

        public static CloudflareTurnstileValidationResult Fail(string errorMessage)
        {
            return new CloudflareTurnstileValidationResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };
        }
    }
}
