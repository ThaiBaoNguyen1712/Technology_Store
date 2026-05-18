using Microsoft.AspNetCore.Http;
using Tech_Store.Models.DTO.Authentication;
using Tech_Store.Services.Auth;

namespace Tech_Store.Services.Interfaces.Auth
{
    public interface IAuthenticationFlowService
    {
        Task<LoginAuthResult> AuthenticateUserAsync(string email, string password);
        Task<OtpChallengeResult> RegisterAsync(RegisterDTo registerDto, ISession session);
        OtpPageState GetOtpPageState(ISession session);
        Task<OtpChallengeResult> VerifyOtpAsync(VerifyOtpDTo verifyOtp, ISession session);
        Task<OtpChallengeResult> ResendOtpAsync(ISession session);
        Task<OtpChallengeResult> ForgotPasswordAsync(string email, ISession session);
        bool CanAccessChangePassword(ISession session);
        Task<AuthFlowResult> ChangeForgottenPasswordAsync(ForgotPasswordDTo forgotPassword, ISession session);
    }
}
