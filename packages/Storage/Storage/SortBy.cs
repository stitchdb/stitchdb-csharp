using System.Text.Json.Serialization;

namespace StitchDB.Storage;

public class SortBy
{
    [JsonPropertyName("column")]
    public string? Column { get; set; } = "name";

    [JsonPropertyName("order")]
    public string? Order { get; set; } = "asc";
}
