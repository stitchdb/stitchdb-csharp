using System;
using StitchDB.Postgrest.Attributes;
using StitchDB.Postgrest.Models;

namespace Postgrest.Tests.Models;

[Table("category")]
public class Category : BaseModel
{
    [PrimaryKey("id")]
    public Guid Id { get; set; }

    [Column("name")]
    public string? Name { get; set; }
}
