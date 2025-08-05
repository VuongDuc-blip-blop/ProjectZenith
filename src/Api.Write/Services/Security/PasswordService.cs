namespace ProjectZenith.Api.Write.Services.Security
{
    public class PasswordService : IPasswordService
    {

        public string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password cannot be null");
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public bool VerifyPassword(string password, string hash)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password cannot be null");
            if (string.IsNullOrEmpty(hash))
                throw new ArgumentException("Hash cannot be null");

            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
    }
}
