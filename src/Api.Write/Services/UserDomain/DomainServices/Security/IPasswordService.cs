namespace ProjectZenith.Api.Write.Services.UserDomain.DomainServices.Security
{
    public interface IPasswordService
    {
        /// <summary>
        /// Hashes a password using BCrypt with a secure work factor
        /// </summary>
        /// <param name="password">The plaintext password to hash.</param>
        /// <returns>The BCrypt hash of the password.</returns>
        string HashPassword(string password);

        /// <summary>
        /// Verifies a password against its stored hash
        /// </summary>
        /// <param name="password">The plaintext password to verify.</param>
        /// <param name="hash">The stored BCrypt hash.</param>
        /// <returns>True if the password matches the hash, false otherwise.</returns>
        bool VerifyPassword(string password, string hash);
    }
}
