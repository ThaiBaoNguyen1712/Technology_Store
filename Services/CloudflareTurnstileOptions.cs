namespace Tech_Store.Services
{
    public class CloudflareTurnstileOptions
    {
        public const string SectionName = "CloudflareTurnstile";

        public string SiteKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string VerifyEndpoint { get; set; } = "https://challenges.cloudflare.com/turnstile/v0/siteverify";
    }
}
