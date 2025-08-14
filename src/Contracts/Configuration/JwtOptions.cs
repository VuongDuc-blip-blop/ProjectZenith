using System.ComponentModel.DataAnnotations;

namespace ProjectZenith.Contracts.Configuration
{
    /// <summary>
    /// Configuration options for JWT (JSON Web Token).
    /// </summary>
    public class JwtOptions
    {
        /// <summary>
        /// The issuer of the JWT token, typically the application or service that generates the token.
        /// </summary>
        [Required]
        public string Issuer { get; set; } = null!;

        /// <summary>
        /// The audience for which the JWT token is intended, usually the application or service that will consume the token.
        /// </summary>
        [Required]
        public string Audience { get; set; } = null!;

        /// <summary>
        /// The secret key used to sign the JWT token, ensuring its integrity and authenticity.
        /// </summary>
        [Required]
        public string Key { get; set; } = null!;

        /// <summary>
        /// The number of minutes after which the JWT token will expire.
        /// </summary>
        [Required]
        public int ExpiryMinutes { get; set; } = 60;
    }
}
