using System.Text.Json.Serialization;

namespace StitchDB.Gotrue.Mfa;

public class MfaUnenrollResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
}
