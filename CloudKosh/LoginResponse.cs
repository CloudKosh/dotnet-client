using System.Text.Json.Serialization;

namespace CloudKosh;

internal class LoginResponse
{
    [JsonPropertyName("authorization")]
    public string Authorization { get; set; }
}