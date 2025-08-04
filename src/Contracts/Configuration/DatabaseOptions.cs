using System.ComponentModel.DataAnnotations;

namespace ProjectZenith.Contracts.Configuration
{
    /// <summary>
    /// Configuration settings for the MSSQL connection.
    /// </summary>
    public class DatabaseOptions
    {
        /// <summary>
        /// Connnection string for the Write or Read database.  
        /// </summary>
        [Required]
        public string ConnectionString { get; set; } = string.Empty;

    }
}
