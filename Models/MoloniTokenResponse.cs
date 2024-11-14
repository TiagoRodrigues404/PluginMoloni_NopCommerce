using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Nop.Plugin.Misc.Moloni.Models
{
    public class MoloniTokenResponse
    {
        [Key]
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; }

        [JsonPropertyName("scope")]
        public string? Scope { get; set; }

        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
