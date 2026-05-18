namespace Tech_Store.Models.Enums
{
    public enum AuthOtpActionType
    {
        Register = 1,
        ForgotPassword = 2
    }

    public static class AuthOtpActionTypeExtensions
    {
        public static string ToRouteValue(this AuthOtpActionType actionType)
        {
            return actionType switch
            {
                AuthOtpActionType.Register => "Register",
                AuthOtpActionType.ForgotPassword => "ForgotPassword",
                _ => string.Empty
            };
        }

        public static bool TryParseRouteValue(string? value, out AuthOtpActionType actionType)
        {
            if (string.Equals(value, "Register", StringComparison.OrdinalIgnoreCase))
            {
                actionType = AuthOtpActionType.Register;
                return true;
            }

            if (string.Equals(value, "ForgotPassword", StringComparison.OrdinalIgnoreCase))
            {
                actionType = AuthOtpActionType.ForgotPassword;
                return true;
            }

            actionType = default;
            return false;
        }
    }
}
