using System.Text.Json.Serialization;

namespace StitchDB.Storage;

public class CreateSignedUrlResponse
{
    [JsonPropertyName("signedURL")]
    public string? SignedUrl { get; set; }
}

public class CreateSignedUrlsResponse : CreateSignedUrlResponse
{
    [JsonPropertyName("path")]
    public string? Path { get; set; }
}
