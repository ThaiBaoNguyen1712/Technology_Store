using Tech_Store.Models.Enums;

namespace Tech_Store.Services.Auth
{
    public class OtpPageState : AuthFlowResult
    {
        public string Email { get; init; } = string.Empty;
        public AuthOtpActionType ActionType { get; init; }
        public int OtpExpiresInSeconds { get; init; }
        public int ResendAvailableInSeconds { get; init; }
    }
}
