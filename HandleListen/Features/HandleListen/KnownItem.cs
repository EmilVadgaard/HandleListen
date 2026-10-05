public class KnownItem
{
    public int Id { get; set; }
    public required string CanonicalName { get; set; }
    public required string Category { get; set; }
    public UnitOfMeasure? DefaultUnit { get; set; }
}
