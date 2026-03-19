using System.Security.Cryptography;
using System.Text;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Helpers
{
    public static class PasswordHasher
    {
        private const int SaltSize = 16;
        private const int KeySize = 32;
        private const int Iterations = 10000;
        
        public static string HashPassword(string password)
        {
            using var algorithm = new Rfc2898DeriveBytes(
                password,
                SaltSize,
                Iterations,
                HashAlgorithmName.SHA256);
            
            var key = Convert.ToBase64String(algorithm.GetBytes(KeySize));
            var salt = Convert.ToBase64String(algorithm.Salt);
            
            return $"{Iterations}.{salt}.{key}";
        }
        
        public static bool VerifyPassword(string hash, string password)
        {
            var parts = hash.Split('.', 3);
            
            if (parts.Length != 3)
                return false;
            
            var iterations = Convert.ToInt32(parts[0]);
            var salt = Convert.FromBase64String(parts[1]);
            var key = Convert.FromBase64String(parts[2]);
            
            using var algorithm = new Rfc2898DeriveBytes(
                password,
                salt,
                iterations,
                HashAlgorithmName.SHA256);
            
            var keyToCheck = algorithm.GetBytes(KeySize);
            var verified = keyToCheck.Length == key.Length;
            
            for (int i = 0; i < keyToCheck.Length && i < key.Length; i++)
            {
                verified &= keyToCheck[i] == key[i];
            }
            
            return verified;
        }
    }
}
