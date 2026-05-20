using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Tech_Store.Events;
using Tech_Store.Helper;
using Tech_Store.Helpers;
using Tech_Store.Models;
using Tech_Store.Models.DTO.Authentication;
using Tech_Store.Models.Enums;
using Tech_Store.Services.Admin.NotificationServices;
using Tech_Store.Services.Interfaces.Auth;

namespace Tech_Store.Services.Auth
{
    public class AuthenticationFlowService : IAuthenticationFlowService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly NotificationService _notificationService;
        private readonly RedisService _redisService;
        private const int OtpExpirationMinutes = 3;
        private const int OtpResendCooldownSeconds = 45;
        private const int OtpMaxVerifyAttempts = 5;
        private const int OtpMaxSendsPerHour = 5;
        private const int OtpEmailTimeoutSeconds = 8;
        public const string PendingOtpEmailSessionKey = "PendingOtpEmail";
        public const string PendingOtpActionSessionKey = "PendingOtpAction";
        public const string PasswordResetVerifiedSessionKey = "PasswordResetVerified";
        public const string UserEmailSessionKey = "UserEmail";

        public AuthenticationFlowService(
            ApplicationDbContext context,
            IEmailService emailService,
            NotificationService notificationService,
            RedisService redisService)
        {
            _context = context;
            _emailService = emailService;
            _notificationService = notificationService;
            _redisService = redisService;
        }

        public async Task<LoginAuthResult> AuthenticateUserAsync(string email, string password)
        {
            var user = await _context.Users
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(x => x.Email == email);

            if (user == null || !PasswordHelper.VerifyPassword(password, user.PasswordHash))
            {
                return new LoginAuthResult
                {
                    IsSuccess = false,
                    Message = "Sai tài khoản hoặc mật khẩu."
                };
            }

            if (user.IsActive != true)
            {
                return new LoginAuthResult
                {
                    IsSuccess = false,
                    Message = "Bạn đã bị chặn bởi cửa hàng."
                };
            }

            return new LoginAuthResult
            {
                IsSuccess = true,
                User = user
            };
        }

        public async Task<OtpChallengeResult> RegisterAsync(RegisterDTo registerDto, ISession session)
        {
            if (await CheckExistEmailAsync(registerDto.Email))
            {
                return CreateFailedOtpResult("Email đã được sử dụng.");
            }

            if (registerDto.Password != registerDto.ConfirmPassword)
            {
                return CreateFailedOtpResult("Mật khẩu không khớp.");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var userMutation = await CreateOrUpdatePendingUserAsync(registerDto);
                if (userMutation.IsNewUser)
                {
                    await SetupUserRoleAsync(userMutation.User.UserId);
                    await CreateCartAsync(userMutation.User);
                }

                await transaction.CommitAsync();

                var otpResult = await IssueOtpAsync(userMutation.User, AuthOtpActionType.Register, true);
                if (!otpResult.IsSuccess)
                {
                    return otpResult;
                }

                SetPendingOtpSession(session, userMutation.User.Email, AuthOtpActionType.Register);
                await _notificationService.NotifyAsync(
                    NotificationTarget.Admins,
                    "Người dùng mới",
                    $"Người dùng{userMutation.User.Email} vừa tạo tài khoản !",
                    "new register",
                    $"/admin/users/{userMutation.User.UserId}");

                return new OtpChallengeResult
                {
                    IsSuccess = true,
                    Email = userMutation.User.Email,
                    Message = otpResult.IsDeliveryDelayed
                        ? "Tài khoản đã được tạo. Hệ thống đang xử lý gửi mã, nếu chưa nhận được bạn có thể gửi lại sau ít giây."
                        : "Tài khoản đã được tạo. Vui lòng kiểm tra email để nhập mã xác thực.",
                    ActionType = AuthOtpActionType.Register,
                    IsDeliveryDelayed = otpResult.IsDeliveryDelayed,
                    OtpExpiresInSeconds = otpResult.OtpExpiresInSeconds,
                    ResendAvailableInSeconds = otpResult.ResendAvailableInSeconds
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public OtpPageState GetOtpPageState(ISession session)
        {
            var pendingEmail = session.GetString(PendingOtpEmailSessionKey);
            var pendingAction = session.GetString(PendingOtpActionSessionKey);

            if (string.IsNullOrWhiteSpace(pendingEmail) ||
                !AuthOtpActionTypeExtensions.TryParseRouteValue(pendingAction, out var actionType))
            {
                return new OtpPageState
                {
                    IsSuccess = false,
                    Message = "Phiên làm việc đã hết hạn."
                };
            }

            var user = _context.Users.AsNoTracking().FirstOrDefault(x => x.Email == pendingEmail);
            if (user == null)
            {
                return new OtpPageState
                {
                    IsSuccess = false,
                    Message = "Không tìm thấy người dùng."
                };
            }

            return new OtpPageState
            {
                IsSuccess = true,
                Email = pendingEmail,
                ActionType = actionType,
                OtpExpiresInSeconds = GetOtpExpiresInSeconds(user),
                ResendAvailableInSeconds = GetResendAvailableInSeconds(user)
            };
        }

        public async Task<OtpChallengeResult> VerifyOtpAsync(VerifyOtpDTo verifyOtp, ISession session)
        {
            var pageState = GetOtpPageState(session);
            if (!pageState.IsSuccess)
            {
                return CreateFailedOtpResult(pageState.Message);
            }

            var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == pageState.Email);
            if (user == null)
            {
                return CreateFailedOtpResult("Không tìm thấy người dùng.");
            }

            var validationResult = await ValidateOtpAsync(user, pageState.ActionType, verifyOtp.OtpToken);
            if (!validationResult.IsSuccess)
            {
                return new OtpChallengeResult
                {
                    IsSuccess = false,
                    Message = validationResult.Message,
                    ActionType = pageState.ActionType,
                    OtpExpiresInSeconds = GetOtpExpiresInSeconds(user),
                    ResendAvailableInSeconds = GetResendAvailableInSeconds(user)
                };
            }

            if (pageState.ActionType == AuthOtpActionType.Register)
            {
                user.IsVerified = true;
                await _context.SaveChangesAsync();
            }
            else
            {
                session.SetString(UserEmailSessionKey, user.Email);
                session.SetString(PasswordResetVerifiedSessionKey, bool.TrueString);
            }

            await ClearOtpStateAsync(session, user.Email, pageState.ActionType);

            return new OtpChallengeResult
            {
                IsSuccess = true,
                Email = user.Email,
                Message = "Mã xác thực OTP hợp lệ!",
                ActionType = pageState.ActionType
            };
        }

        public async Task<OtpChallengeResult> ResendOtpAsync(ISession session)
        {
            var pageState = GetOtpPageState(session);
            if (!pageState.IsSuccess)
            {
                return CreateFailedOtpResult(pageState.Message);
            }

            var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == pageState.Email);
            if (user == null)
            {
                return CreateFailedOtpResult("Không tìm thấy người dùng.");
            }

            var otpResult = await IssueOtpAsync(user, pageState.ActionType, pageState.ActionType == AuthOtpActionType.Register);
            if (!otpResult.IsSuccess)
            {
                return otpResult;
            }

            return new OtpChallengeResult
            {
                IsSuccess = true,
                Email = user.Email,
                Message = otpResult.IsDeliveryDelayed
                    ? "Yêu cầu đã được ghi nhận. Nếu chưa nhận được email, vui lòng chờ thêm ít giây rồi kiểm tra lại."
                    : "Đã gửi mã xác thực mới.",
                ActionType = pageState.ActionType,
                IsDeliveryDelayed = otpResult.IsDeliveryDelayed,
                OtpExpiresInSeconds = otpResult.OtpExpiresInSeconds,
                ResendAvailableInSeconds = otpResult.ResendAvailableInSeconds
            };
        }

        public async Task<OtpChallengeResult> ForgotPasswordAsync(string email, ISession session)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == email && x.IsVerified == true);
            if (user == null)
            {
                return CreateFailedOtpResult("Email chưa được đăng ký!");
            }

            var otpResult = await IssueOtpAsync(user, AuthOtpActionType.ForgotPassword, false);
            if (!otpResult.IsSuccess)
            {
                return otpResult;
            }

            SetPendingOtpSession(session, email, AuthOtpActionType.ForgotPassword);
            session.SetString(PasswordResetVerifiedSessionKey, bool.FalseString);

            return new OtpChallengeResult
            {
                IsSuccess = true,
                Email = user.Email,
                Message = otpResult.IsDeliveryDelayed
                    ? "Hệ thống đang xử lý gửi mã. Nếu chưa nhận được email, bạn có thể gửi lại sau ít giây."
                    : "Vui lòng kiểm tra email để lấy mã xác thực.",
                ActionType = AuthOtpActionType.ForgotPassword,
                IsDeliveryDelayed = otpResult.IsDeliveryDelayed,
                OtpExpiresInSeconds = otpResult.OtpExpiresInSeconds,
                ResendAvailableInSeconds = otpResult.ResendAvailableInSeconds
            };
        }

        public bool CanAccessChangePassword(ISession session)
        {
            return string.Equals(session.GetString(PasswordResetVerifiedSessionKey), bool.TrueString, StringComparison.Ordinal);
        }

        public async Task<AuthFlowResult> ChangeForgottenPasswordAsync(ForgotPasswordDTo forgotPassword, ISession session)
        {
            var email = session.GetString(UserEmailSessionKey);
            if (string.IsNullOrEmpty(email))
            {
                return new AuthFlowResult
                {
                    IsSuccess = false,
                    Message = "Phiên làm việc đã hết hạn."
                };
            }

            if (!CanAccessChangePassword(session))
            {
                return new AuthFlowResult
                {
                    IsSuccess = false,
                    Message = "Bạn cần xác thực mã OTP trước khi đổi mật khẩu."
                };
            }

            if (forgotPassword.Password != forgotPassword.ConfirmPassword)
            {
                return new AuthFlowResult
                {
                    IsSuccess = false,
                    Message = "Mật khẩu không khớp."
                };
            }

            var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == email);
            if (user == null)
            {
                return new AuthFlowResult
                {
                    IsSuccess = false,
                    Message = "Không tìm thấy người dùng."
                };
            }

            user.PasswordHash = PasswordHelper.HashPassword(forgotPassword.Password);
            await _context.SaveChangesAsync();

            session.Remove(UserEmailSessionKey);
            session.Remove(PasswordResetVerifiedSessionKey);
            session.Remove(PendingOtpEmailSessionKey);
            session.Remove(PendingOtpActionSessionKey);

            await _notificationService.NotifyAsync(
                NotificationTarget.SpecificUsers,
                "Thay đổi mật khẩu",
                $"Bạn đã thay đổi mật khẩu lúc {DateTime.Now}",
                "info",
                "#",
                new List<int> { user.UserId });

            return new AuthFlowResult
            {
                IsSuccess = true,
                Message = "Thay đổi mật khẩu thành công"
            };
        }

        private async Task<bool> CheckExistEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            return await _context.Users.AnyAsync(x =>
                x.Email == email.Trim() &&
                x.PasswordHash != null &&
                x.IsVerified == true);
        }

        private async Task<(User User, bool IsNewUser)> CreateOrUpdatePendingUserAsync(RegisterDTo registerDto)
        {
            var normalizedEmail = registerDto.Email.Trim();
            var user = await _context.Users
                .Include(x => x.Carts)
                .Include(x => x.Roles)
                .FirstOrDefaultAsync(x => x.Email == normalizedEmail);

            if (user != null)
            {
                user.PasswordHash = PasswordHelper.HashPassword(registerDto.Password);
                user.IsVerified = false;
                user.LastLogin = DateTime.Now;
                user.IsActive ??= true;
                user.Img ??= "none.png";
                await _context.SaveChangesAsync();

                return (user, false);
            }

            user = new User
            {
                Email = normalizedEmail,
                PasswordHash = PasswordHelper.HashPassword(registerDto.Password),
                IsVerified = false,
                IsActive = true,
                Img = "none.png",
                LastLogin = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return (user, true);
        }

        private async Task SetupUserRoleAsync(int userId)
        {
            var userRole = new Dictionary<string, object>
            {
                { "UserId", userId },
                { "RoleId", 2 }
            };

            _context.Set<Dictionary<string, object>>("UserRole").Add(userRole);
            await _context.SaveChangesAsync();
        }

        private async Task<bool> TrySendVerificationEmailAsync(string email, string subject, string code)
        {
            var emailTask = _emailService.SendEmailAsync(new MailRequest
            {
                ToEmail = email,
                Subject = subject,
                Body = $"Đây là mã xác thực của bạn: {code}"
            });

            var completedTask = await Task.WhenAny(emailTask, Task.Delay(TimeSpan.FromSeconds(OtpEmailTimeoutSeconds)));
            if (completedTask != emailTask)
            {
                return false;
            }

            await emailTask;
            return true;
        }

        private async Task<(bool IsSuccess, string Message)> ValidateOtpAsync(User user, AuthOtpActionType actionType, string otpToken)
        {
            if (string.IsNullOrWhiteSpace(user.VerificationCode) || string.IsNullOrWhiteSpace(otpToken))
            {
                return (false, "Mã xác thực không hợp lệ.");
            }

            var expiresInSeconds = GetOtpExpiresInSeconds(user);
            if (expiresInSeconds <= 0)
            {
                return (false, "Mã xác thực đã hết hạn. Vui lòng gửi lại mã mới.");
            }

            var invalidAttemptKey = GetOtpInvalidAttemptKey(user.Email, actionType);
            var currentAttemptCount = await GetRedisIntAsync(invalidAttemptKey);
            if (currentAttemptCount >= OtpMaxVerifyAttempts)
            {
                return (false, "Bạn đã nhập sai quá nhiều lần. Vui lòng gửi lại mã mới.");
            }

            if (!string.Equals(user.VerificationCode.Trim(), otpToken.Trim(), StringComparison.Ordinal))
            {
                var updatedAttemptCount = await _redisService.IncrementAsync(invalidAttemptKey, TimeSpan.FromMinutes(OtpExpirationMinutes));
                if (updatedAttemptCount >= OtpMaxVerifyAttempts)
                {
                    return (false, "Bạn đã nhập sai quá nhiều lần. Vui lòng gửi lại mã mới.");
                }

                return (false, $"Mã xác thực chưa đúng. Bạn còn {OtpMaxVerifyAttempts - updatedAttemptCount} lần thử.");
            }

            await _redisService.DeleteAsync(invalidAttemptKey);
            return (true, string.Empty);
        }

        private async Task<OtpChallengeResult> IssueOtpAsync(User user, AuthOtpActionType actionType, bool resetVerifiedStatus)
        {
            var resendAvailableInSeconds = GetResendAvailableInSeconds(user);
            if (resendAvailableInSeconds > 0)
            {
                return CreateFailedOtpResult(
                    $"Vui lòng chờ {resendAvailableInSeconds} giây trước khi gửi lại mã.",
                    actionType,
                    GetOtpExpiresInSeconds(user),
                    resendAvailableInSeconds);
            }

            var sendCountKey = GetOtpSendCountKey(user.Email, actionType);
            var sendCount = await _redisService.IncrementAsync(sendCountKey, TimeSpan.FromHours(1));
            if (sendCount > OtpMaxSendsPerHour)
            {
                var remainingBlockSeconds = (int)Math.Ceiling((await _redisService.GetTimeToLiveAsync(sendCountKey))?.TotalSeconds ?? 0);
                return CreateFailedOtpResult(
                    "Bạn thao tác quá nhanh. Vui lòng thử lại sau ít phút.",
                    actionType,
                    GetOtpExpiresInSeconds(user),
                    Math.Max(remainingBlockSeconds, OtpResendCooldownSeconds));
            }

            user.VerificationCode = GenerateOtp();
            user.CreatedVerify = DateTime.Now;
            if (resetVerifiedStatus)
            {
                user.IsVerified = false;
            }

            await _context.SaveChangesAsync();
            await _redisService.DeleteAsync(GetOtpInvalidAttemptKey(user.Email, actionType));

            var subject = actionType == AuthOtpActionType.Register ? "Xác thực tài khoản" : "Mã xác thực đặt lại mật khẩu";
            var isDeliveryDelayed = false;

            try
            {
                var emailSent = await TrySendVerificationEmailAsync(user.Email, subject, user.VerificationCode!);
                isDeliveryDelayed = !emailSent;
            }
            catch
            {
                isDeliveryDelayed = true;
            }

            return new OtpChallengeResult
            {
                IsSuccess = true,
                Email = user.Email,
                ActionType = actionType,
                IsDeliveryDelayed = isDeliveryDelayed,
                OtpExpiresInSeconds = GetOtpExpiresInSeconds(user),
                ResendAvailableInSeconds = OtpResendCooldownSeconds
            };
        }

        private static string GenerateOtp()
        {
            return new Random().Next(100000, 999999).ToString();
        }

        private int GetOtpExpiresInSeconds(User user)
        {
            if (!user.CreatedVerify.HasValue)
            {
                return 0;
            }

            var expiresAt = user.CreatedVerify.Value.AddMinutes(OtpExpirationMinutes);
            return Math.Max(0, (int)Math.Ceiling((expiresAt - DateTime.Now).TotalSeconds));
        }

        private int GetResendAvailableInSeconds(User user)
        {
            if (!user.CreatedVerify.HasValue)
            {
                return 0;
            }

            var resendAt = user.CreatedVerify.Value.AddSeconds(OtpResendCooldownSeconds);
            return Math.Max(0, (int)Math.Ceiling((resendAt - DateTime.Now).TotalSeconds));
        }

        private static void SetPendingOtpSession(ISession session, string email, AuthOtpActionType actionType)
        {
            session.SetString(PendingOtpEmailSessionKey, email);
            session.SetString(PendingOtpActionSessionKey, actionType.ToRouteValue());
        }

        private async Task ClearOtpStateAsync(ISession session, string email, AuthOtpActionType actionType)
        {
            session.Remove(PendingOtpEmailSessionKey);
            session.Remove(PendingOtpActionSessionKey);
            await _redisService.DeleteAsync(GetOtpInvalidAttemptKey(email, actionType));
        }

        private static string GetOtpSendCountKey(string email, AuthOtpActionType actionType)
        {
            return $"auth:otp:send-count:{actionType.ToRouteValue().ToLowerInvariant()}:{email.Trim().ToLowerInvariant()}";
        }

        private static string GetOtpInvalidAttemptKey(string email, AuthOtpActionType actionType)
        {
            return $"auth:otp:invalid-attempt:{actionType.ToRouteValue().ToLowerInvariant()}:{email.Trim().ToLowerInvariant()}";
        }

        private async Task<int> GetRedisIntAsync(string key)
        {
            var value = await _redisService.GetAsync(key);
            return int.TryParse(value, out var result) ? result : 0;
        }

        private async Task CreateCartAsync(User user)
        {
            var cart = new Models.Cart
            {
                UserId = user.UserId,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.Carts.Add(cart);
            await _context.SaveChangesAsync();
        }

        private static OtpChallengeResult CreateFailedOtpResult(
            string message,
            AuthOtpActionType actionType = default,
            int otpExpiresInSeconds = 0,
            int resendAvailableInSeconds = 0)
        {
            return new OtpChallengeResult
            {
                IsSuccess = false,
                Message = message,
                ActionType = actionType,
                OtpExpiresInSeconds = otpExpiresInSeconds,
                ResendAvailableInSeconds = resendAvailableInSeconds
            };
        }
    }
}
