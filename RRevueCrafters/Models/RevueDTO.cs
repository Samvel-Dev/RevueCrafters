using System.Text.Json.Serialization;

namespace RRevueCrafters.Models
{
    internal class RevueDTO
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }
}

