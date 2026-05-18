using Tech_Store.Models.Enums;

namespace Tech_Store.Services.Auth
{
    public class OtpChallengeResult : AuthFlowResult
    {
        public string Email { get; init; } = string.Empty;
        public AuthOtpActionType ActionType { get; init; }
        public bool IsDeliveryDelayed { get; init; }
        public int OtpExpiresInSeconds { get; init; }
        public int ResendAvailableInSeconds { get; init; }
    }
}
