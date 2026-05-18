using Microsoft.AspNetCore.Http;

namespace Tech_Store.Services
{
    public interface ICloudflareTurnstileService
    {
        Task<CloudflareTurnstileValidationResult> ValidateAsync(HttpRequest request, CancellationToken cancellationToken = default);
    }
}
