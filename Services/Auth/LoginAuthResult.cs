using Tech_Store.Models;

namespace Tech_Store.Services.Auth
{
    public class LoginAuthResult : AuthFlowResult
    {
        public User? User { get; init; }
    }
}
