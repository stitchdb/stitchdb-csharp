using System;
using StitchDB.Postgrest.Attributes;
using StitchDB.Postgrest.Models;

namespace StitchDB.Tests.Models;

[Table("channels")]
public class Channel : BaseModel
{
    [PrimaryKey("id")]
    public int Id { get; set; }

    [Column("inserted_at")]
    public DateTime InsertedAt { get; set; }

    [Column("slug")]
    public string Slug { get; set; }
}
