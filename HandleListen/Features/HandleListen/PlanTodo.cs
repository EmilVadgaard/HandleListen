public class PlanTodo
{
    public int Id { get; set; }
    public int PlanId { get; set; }
    public required string Title { get; set; }
    public bool Done { get; set; } = false;

    // Weekly only - 0-based day offset ("Dag 1" = 0).
    public int? DayIndex { get; set; }

    // Monthly only - 1-based day of month.
    public int? DayOfMonth { get; set; }

    public TimeOnly? Time { get; set; }

    // Drag-and-drop order within a Standard plan's single column; otherwise just a tiebreaker.
    public int SortOrder { get; set; }
}
