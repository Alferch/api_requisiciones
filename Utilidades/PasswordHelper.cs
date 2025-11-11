using RequisicionesApi.Utilidades;
using System.Security.Cryptography;
using System.Text;

namespace RequisicionesApi.Utilidades
{
    public class PasswordHelper
    {

        public static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        public static bool Verify(string plainPassword, string hashedPassword)
        {
            return HashPassword(plainPassword) == hashedPassword;
        }

    }
}


//string mail = "admin@test.com";
//string passwordHash = PasswordHelper.HashPassword("1234");
//string rol = "Admin";