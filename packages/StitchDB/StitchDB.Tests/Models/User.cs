using System.Text.Json.Serialization;
using StitchDB.Postgrest.Attributes;
using StitchDB.Postgrest.Models;

namespace StitchDB.Tests.Models;

[Table("users")]
public class User : BaseModel
{
    [JsonPropertyName("username")]
    public string? Username { get; set; }
}
