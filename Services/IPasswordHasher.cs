namespace CRMFinaciertoBackend.Services
{
    public enum PasswordVerificationResult
    {
        /// <summary>La contrasena no coincide.</summary>
        Failed,

        /// <summary>La contrasena coincide y el hash almacenado esta al dia.</summary>
        Success,

        /// <summary>
        /// La contrasena coincide pero el hash almacenado es heredado (texto plano o con
        /// parametros antiguos). El llamador debe volver a guardarlo con <see cref="IPasswordHasher.Hash"/>.
        /// </summary>
        SuccessRehashNeeded
    }

    public interface IPasswordHasher
    {
        string Hash(string password);

        PasswordVerificationResult Verify(string storedHash, string password);
    }
}
