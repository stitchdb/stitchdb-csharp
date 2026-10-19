using System.Text.Json.Nodes;
using StitchDB.Postgrest.Attributes;
using StitchDB.Postgrest.Models;

namespace Postgrest.Tests.Models;

[Table("users")]
public class UserWithJsonData : BaseModel
{
    [PrimaryKey("username", true)]
    public string? Username { get; set; }

    [Column("data")]
    public JsonObject? Data { get; set; }
}
