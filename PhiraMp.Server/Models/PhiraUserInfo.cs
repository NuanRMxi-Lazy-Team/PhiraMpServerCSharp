using System.Text.Json.Serialization;

namespace PhiraMp.Server.Models;

public class PhiraUserInfo
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("language")]
    public string Language { get; set; } = "en-US";
}