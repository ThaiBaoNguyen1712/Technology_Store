using BCrypt.Net;
namespace Tech_Store.Helper
{
    public class PasswordHelper
    {
        public static string HashPassword(string password)
        {
            // Mã băm mật khẩu với salt tự động sinh ra
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        // Kiểm tra mật khẩu với mật khẩu đã băm
        public static bool VerifyPassword(string password, string hashedPassword)
        {
            try
            {
                if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hashedPassword))
                {
                    throw new ArgumentException("Password hoặc hashedPassword không được null hoặc rỗng.");
                }

                // Kiểm tra mật khẩu với giá trị đã băm
                return BCrypt.Net.BCrypt.Verify(password.Trim(), hashedPassword.Trim());
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi khi kiểm tra mật khẩu: " + ex.Message);
                return false;
            }
        }

    }
}
