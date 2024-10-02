namespace Mojo.Framework.Core.Data.Entities;

public class Metadata
{
    public DateTimeOffset CreatedAt { get; set; }
    public string CreatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string UpdatedBy { get; set; }
}
