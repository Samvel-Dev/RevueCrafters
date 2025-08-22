using System.Text.Json.Serialization;

namespace RRevueCrafters.Models
{
    internal class ApiResponseDTO
    {
        [JsonPropertyName("msg")]
        public string? Msg { get; set; }

        [JsonPropertyName("revueId")]
        public string? RevuedId { get; set; }
    }
}
