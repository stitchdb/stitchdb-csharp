using System.Text.Json.Serialization;

namespace StitchDB.Storage.Responses;

internal class CreatedUploadSignedUrlResponse
{
    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

