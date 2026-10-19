using System;
using StitchDB.Postgrest.Attributes;
using StitchDB.Postgrest.Models;

namespace Postgrest.Tests.Models;

[Table("product")]
public class Product : BaseModel
{

    [PrimaryKey("id")]
    public Guid Id { get; set; }

    [Column("name")]
    public string? Name { get; set; }

    [Reference(typeof(Category))]
    public Category? Category { get; set; }
}
