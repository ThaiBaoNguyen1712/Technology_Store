using System.Security.Claims;
using Tech_Store.Models;

namespace Tech_Store.Services.Auth
{
    public interface IExternalAuthenticationService
    {
        Task<ExternalAuthenticationResult> GetOrCreateUserAsync(ClaimsPrincipal principal, string providerName);
    }

    public sealed class ExternalAuthenticationResult
    {
        public bool IsSuccess { get; init; }
        public string? ErrorMessage { get; init; }
        public User? User { get; init; }
    }
}
