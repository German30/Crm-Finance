using System.Security.Cryptography;
using System.Text;

namespace CRMFinaciertoBackend.Services
{
    /// <summary>
    /// Hashing PBKDF2-HMAC-SHA256 con la BCL (sin paquetes extra, el csproj no admite
    /// versiones flotantes). Formato almacenado: PBKDF2$iteraciones$saltBase64$hashBase64.
    /// </summary>
    public class PasswordHasher : IPasswordHasher
    {
        private const string Prefix = "PBKDF2";
        private const int SaltSize = 16;
        private const int KeySize = 32;
        private const int Iterations = 100_000;

        private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

        public string Hash(string password)
        {
            ArgumentNullException.ThrowIfNull(password);

            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, KeySize);

            return string.Join('$',
                Prefix,
                Iterations.ToString(),
                Convert.ToBase64String(salt),
                Convert.ToBase64String(key));
        }

        public PasswordVerificationResult Verify(string storedHash, string password)
        {
            if (string.IsNullOrEmpty(storedHash) || password == null)
                return PasswordVerificationResult.Failed;

            var parts = storedHash.Split('$');

            // Contrasenas heredadas guardadas en texto plano: se aceptan una ultima vez para
            // que el login pueda regrabarlas ya hasheadas.
            if (parts.Length != 4 || parts[0] != Prefix)
            {
                return FixedTimeEquals(storedHash, password)
                    ? PasswordVerificationResult.SuccessRehashNeeded
                    : PasswordVerificationResult.Failed;
            }

            if (!int.TryParse(parts[1], out var iterations) || iterations <= 0)
                return PasswordVerificationResult.Failed;

            byte[] salt;
            byte[] key;
            try
            {
                salt = Convert.FromBase64String(parts[2]);
                key = Convert.FromBase64String(parts[3]);
            }
            catch (FormatException)
            {
                return PasswordVerificationResult.Failed;
            }

            if (salt.Length == 0 || key.Length == 0)
                return PasswordVerificationResult.Failed;

            var computed = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, Algorithm, key.Length);

            if (!CryptographicOperations.FixedTimeEquals(computed, key))
                return PasswordVerificationResult.Failed;

            return iterations == Iterations && salt.Length == SaltSize && key.Length == KeySize
                ? PasswordVerificationResult.Success
                : PasswordVerificationResult.SuccessRehashNeeded;
        }

        private static bool FixedTimeEquals(string left, string right)
        {
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(left),
                Encoding.UTF8.GetBytes(right));
        }
    }
}
